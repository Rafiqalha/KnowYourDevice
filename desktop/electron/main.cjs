const {
  app,
  BrowserWindow,
  ipcMain,
  dialog,
  shell,
  session,
} = require("electron");
const { execFile } = require("node:child_process");
const { join } = require("node:path");
const os = require("node:os");
const { redact, validFormat, cpuTotals, cpuPercent } = require("./domain.cjs");

let window, snapshot, pendingScan, metricTimer, previousCpu;
let includePrivate = false;
const children = new Set();
const bridge = () =>
  app.isPackaged
    ? join(process.resourcesPath, "native", "HardwareBridge.exe")
    : join(__dirname, "../../build/native/HardwareBridge.exe");
function invokeBridge(args, input) {
  return new Promise((resolve, reject) => {
    const child = execFile(
      bridge(),
      args,
      {
        windowsHide: true,
        encoding: "utf8",
        timeout: 90000,
        maxBuffer: 16 * 1024 * 1024,
      },
      (error, stdout, stderr) => {
        children.delete(child);
        if (error)
          reject(
            new Error(
              stderr.trim() ||
                "Pembacaan hardware gagal. Pastikan .NET Framework 4.8 dan WMI tersedia.",
            ),
          );
        else resolve(stdout.replace(/^\uFEFF/, ""));
      },
    );
    children.add(child);
    child.stdin.on("error", () => {});
    child.stdin.end(input || "");
  });
}
function assertSender(event) {
  if (
    !window ||
    event.sender.id !== window.webContents.id ||
    event.senderFrame !== window.webContents.mainFrame ||
    event.senderFrame.url !== window.webContents.getURL()
  )
    throw new Error("Sumber permintaan tidak diizinkan.");
}
function register(channel, handler) {
  ipcMain.handle(channel, (event, ...args) => {
    assertSender(event);
    return handler(...args);
  });
}
register("device:scan", async () => {
  if (!pendingScan) {
    pendingScan = invokeBridge(["--scan"])
      .then((output) => {
        const next = JSON.parse(output);
        if (!Array.isArray(next.Categories) || !Array.isArray(next.Sources))
          throw new Error("Format inventaris Windows tidak valid.");
        snapshot = next;
      })
      .finally(() => {
        pendingScan = null;
      });
  }
  await pendingScan;
  return redact(snapshot, includePrivate);
});
register("device:snapshot", (showPrivate) => {
  if (typeof showPrivate !== "boolean")
    throw new Error("Pilihan privasi tidak valid.");
  includePrivate = showPrivate;
  return redact(snapshot, includePrivate);
});
register("device:export", async (format) => {
  validFormat(format);
  if (!snapshot)
    throw new Error("Pindai perangkat sebelum mengekspor laporan.");
  const { canceled, filePath } = await dialog.showSaveDialog(window, {
    title: "Simpan laporan perangkat",
    defaultPath: `KnowYourDevice-${new Date().toISOString().slice(0, 10)}.${format}`,
    filters: [
      { name: `Laporan ${format.toUpperCase()}`, extensions: [format] },
    ],
  });
  if (canceled || !filePath) return { canceled: true };
  await invokeBridge(
    ["--export", filePath, String(includePrivate)],
    JSON.stringify(snapshot),
  );
  return { canceled: false, fileName: require("node:path").basename(filePath) };
});
register("device:repository", () =>
  shell.openExternal("https://github.com/Rafiqalha/KnowYourDevice"),
);
register("device:theme", (theme) => {
  if (!["light", "dark"].includes(theme)) throw new Error("Tema tidak valid.");
  window.setTitleBarOverlay({
    color: theme === "dark" ? "#20211f" : "#f8f7f4",
    symbolColor: theme === "dark" ? "#dcd9d0" : "#55524b",
    height: 38,
  });
});

function sendMetrics() {
  if (!window || window.isDestroyed()) return;
  const next = cpuTotals();
  const total = os.totalmem(),
    available = os.freemem();
  const metrics = {
    cpuPercent: cpuPercent(previousCpu, next),
    memoryPercent: total > 0 ? (100 * (total - available)) / total : null,
    totalMemory: total,
    availableMemory: available,
    sampledAt: Date.now(),
  };
  previousCpu = next;
  window.webContents.send("device:metrics", metrics);
}
app.whenReady().then(() => {
  app.setAppUserModelId("id.rafiqalha.knowyourdevice");
  session.defaultSession.setPermissionRequestHandler(
    (_contents, _permission, callback) => callback(false),
  );
  session.defaultSession.setPermissionCheckHandler(() => false);
  window = new BrowserWindow({
    width: 1400,
    height: 930,
    minWidth: 940,
    minHeight: 680,
    show: false,
    backgroundColor: "#f8f7f4",
    title: "KnowYourDevice",
    autoHideMenuBar: true,
    icon: join(__dirname, "../out/mark.png"),
    titleBarStyle: "hidden",
    titleBarOverlay: { color: "#f8f7f4", symbolColor: "#55524b", height: 38 },
    webPreferences: {
      preload: join(__dirname, "preload.cjs"),
      nodeIntegration: false,
      contextIsolation: true,
      sandbox: true,
      webSecurity: true,
      spellcheck: false,
    },
  });
  window.webContents.setWindowOpenHandler(() => ({ action: "deny" }));
  window.webContents.on("will-navigate", (event) => event.preventDefault());
  window.webContents.on("will-attach-webview", (event) =>
    event.preventDefault(),
  );
  window.once("ready-to-show", () => window.show());
  window.webContents.once("did-finish-load", () => {
    sendMetrics();
    metricTimer = setInterval(sendMetrics, 1000);
    const verificationIndex = process.argv.indexOf("--verify-portable");
    if (verificationIndex >= 0 && process.argv[verificationIndex + 1]) {
      verifyPortable(process.argv[verificationIndex + 1]);
    }
  });
  window.loadFile(join(__dirname, "../out/index.html"));
});
async function verifyPortable(directory) {
  const fs = require("node:fs/promises");
  const destination = require("node:path").resolve(directory);
  let result;
  try {
    await fs.mkdir(destination, { recursive: true });
    const deadline = Date.now() + 90000;
    while (!snapshot && Date.now() < deadline) {
      await new Promise((resolve) => setTimeout(resolve, 250));
    }
    if (
      !snapshot ||
      !snapshot.Sources.some((source) => source.Status === "Berhasil")
    )
      throw new Error("Inventaris hardware tidak berhasil dibaca.");
    await new Promise((resolve) => setTimeout(resolve, 1000));
    const rendered = await window.webContents.executeJavaScript(
      "Boolean(document.querySelector('[data-testid=tile-cpu]'))",
    );
    if (!rendered) throw new Error("Ringkasan perangkat belum dirender.");
    await fs.writeFile(
      join(destination, "portable.png"),
      (await window.webContents.capturePage()).toPNG(),
    );
    result = {
      passed: true,
      packaged: app.isPackaged,
      version: app.getVersion(),
      architecture: process.arch,
      sources: snapshot.Sources.length,
      entries: snapshot.Categories.reduce(
        (sum, category) => sum + category.Devices.length,
        0,
      ),
    };
  } catch (error) {
    result = { passed: false, error: error.message };
  }
  await fs.mkdir(destination, { recursive: true });
  await fs.writeFile(
    join(destination, "portable-test.json"),
    JSON.stringify(result, null, 2),
  );
  app.exit(result.passed ? 0 : 1);
}
app.on("window-all-closed", () => app.quit());
app.on("before-quit", () => {
  clearInterval(metricTimer);
  for (const child of children) child.kill();
  snapshot = null;
});
