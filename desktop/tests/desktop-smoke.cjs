const { _electron: electron } = require("playwright");
const assert = require("node:assert/strict");
const fs = require("node:fs/promises");
const path = require("node:path");

async function run() {
  const directory = path.resolve(
    process.env.KYD_TEST_OUT || "../artifacts/v2/desktop",
  );
  await fs.mkdir(directory, { recursive: true });
  const env = { ...process.env };
  delete env.ELECTRON_RUN_AS_NODE;
  const executablePath = process.env.KYD_APP_EXE;
  const app = await electron.launch({
    executablePath,
    args: executablePath ? [] : ["."],
    cwd: path.resolve("."),
    env,
    timeout: 60000,
  });
  let page;
  const checks = [];
  const errors = [];
  try {
    page = await app.firstWindow();
    page.on("pageerror", (error) => errors.push(error.message));
    await page.getByTestId("tile-cpu").waitFor({ timeout: 90000 });
    const snapshot = await page.evaluate(() => window.device.snapshot(false));
    assert.ok(
      snapshot.Categories.find((c) => c.Id === "cpu").Devices.length,
      "Hardware CPU nyata harus tersedia dalam lingkungan uji ini",
    );
    assert.ok(snapshot.Sources.some((s) => s.Status === "Berhasil"));
    assert.ok(
      snapshot.Categories.flatMap((c) => c.Devices)
        .flatMap((d) => d.Fields)
        .filter((f) => f.Sensitive)
        .every((f) => f.Value === "Disembunyikan (ID pribadi)"),
    );
    checks.push("Pemindaian hardware nyata dan redaksi pada batas IPC lulus");
    await page.waitForTimeout(1500);
    await page.screenshot({
      path: path.join(directory, "01-overview-light.png"),
    });
    assert.equal(await page.evaluate(() => typeof require), "undefined");
    assert.equal(await page.evaluate(() => typeof process), "undefined");
    const rejected = await page.evaluate(async () => {
      try {
        await window.device.exportReport("../../invalid");
        return false;
      } catch {
        return true;
      }
    });
    assert.ok(rejected);
    checks.push("Renderer terisolasi dan format IPC divalidasi");
    for (const category of snapshot.Categories) {
      await page.getByTestId(`nav-${category.Id}`).click();
      await page
        .getByTestId(`nav-${category.Id}`)
        .getAttribute("aria-current")
        .then((value) => assert.equal(value, "page"));
      assert.equal(
        await page.getByTestId("device-card").count(),
        category.Devices.length,
      );
      if (["cpu", "storage", "io", "ports"].includes(category.Id))
        await page.screenshot({
          path: path.join(directory, `${category.Id}.png`),
        });
      checks.push(
        `${category.Id}: ${category.Devices.length} entri terverifikasi`,
      );
    }
    await page.getByTestId("nav-overview").click();
    await page
      .getByRole("textbox", { name: "Cari perangkat atau spesifikasi" })
      .fill("___tidak_ada_98765___");
    await page.getByText("Tidak ada yang cocok.").waitFor();
    await page
      .getByRole("textbox", { name: "Cari perangkat atau spesifikasi" })
      .fill("CPU");
    await page.getByTestId("device-card").first().waitFor();
    checks.push("Pencarian global, kategori, dan kondisi tanpa hasil lulus");
    await page.getByTestId("nav-sources").click();
    assert.equal(
      await page.locator(".source-table-row").count(),
      snapshot.Sources.length,
    );
    await page
      .getByText("Port dan kemampuan konektor", { exact: true })
      .click();
    await page.screenshot({ path: path.join(directory, "sources.png") });
    checks.push("Seluruh status penyedia dan batas deteksi dapat diakses");
    await page.getByTestId("nav-cpu").click();
    await page
      .getByRole("switch", { name: "Tampilkan dan ekspor ID pribadi" })
      .click();
    await page.waitForFunction(() =>
      document
        .querySelector(".privacy-control")
        .textContent.includes("terlihat"),
    );
    const privateSnapshot = await page.evaluate(() =>
      window.device.snapshot(true),
    );
    const secret = privateSnapshot.Categories.find(
      (c) => c.Id === "cpu",
    ).Devices[0].Fields.find(
      (f) => f.Sensitive && f.Value !== "Tidak dilaporkan",
    )?.Value;
    if (secret) await page.getByText(secret, { exact: true }).first().waitFor();
    await page
      .getByRole("switch", { name: "Tampilkan dan ekspor ID pribadi" })
      .click();
    await page.waitForFunction(() =>
      document
        .querySelector(".privacy-control")
        .textContent.includes("tersembunyi"),
    );
    if (secret)
      assert.equal(await page.getByText(secret, { exact: true }).count(), 0);
    checks.push("Sakelar privasi menampilkan dan menyembunyikan nilai nyata");
    for (const [format, label] of [
      ["html", /Laporan HTML/],
      ["json", /Data JSON/],
      ["csv", /Tabel CSV/],
    ]) {
      const target = path.join(directory, `laporan.${format}`);
      await app.evaluate(({ dialog }, file) => {
        dialog.showSaveDialog = async () => ({
          canceled: false,
          filePath: file,
        });
      }, target);
      await page.getByTestId("export-button").click();
      await page.getByRole("menuitem", { name: label }).click();
      await page.waitForFunction(() =>
        document
          .querySelector(".toast")
          ?.textContent.includes("Laporan disimpan:"),
      );
      const content = await fs.readFile(target, "utf8");
      assert.ok(content.includes("Disembunyikan (ID pribadi)"));
      if (secret && secret !== "0") assert.ok(!content.includes(secret));
      if (format === "json")
        assert.equal(
          JSON.parse(content.replace(/^\uFEFF/, "")).Categories.length,
          snapshot.Categories.length,
        );
      await page.getByRole("button", { name: "Tutup pemberitahuan" }).click();
    }
    checks.push(
      "Ekspor HTML/JSON/CSV memakai save dialog, bridge native, dan redaksi nyata",
    );
    await page.getByTestId("nav-overview").click();
    await page.getByRole("button", { name: "Aktifkan mode gelap" }).click();
    await page.waitForFunction(() =>
      document.documentElement.classList.contains("dark"),
    );
    await page.screenshot({
      path: path.join(directory, "02-overview-dark.png"),
    });
    await app.evaluate(({ BrowserWindow }) =>
      BrowserWindow.getAllWindows()[0].setSize(1024, 720),
    );
    await page.waitForTimeout(200);
    await page.screenshot({
      path: path.join(directory, "03-compact-dark.png"),
    });
    assert.ok(
      await page.evaluate(
        () => document.documentElement.scrollWidth <= innerWidth,
      ),
    );
    checks.push("Mode gelap dan tata letak kompak 1024 × 720 lulus");
    await page.getByRole("button", { name: "Aktifkan mode terang" }).click();
    await page.getByRole("button", { name: "Tentang aplikasi" }).click();
    await page.getByRole("dialog").waitFor();
    await page.keyboard.press("Escape");
    const capturedAt = snapshot.CapturedAt;
    await page.getByTestId("scan-button").click();
    await page.waitForFunction(
      async (old) => (await window.device.snapshot(false))?.CapturedAt !== old,
      capturedAt,
      { timeout: 90000 },
    );
    checks.push("Dialog keyboard dan pemindaian ulang lulus");
    assert.deepEqual(
      errors,
      [],
      "Tidak boleh ada error JavaScript pada renderer",
    );
    const summary = {
      passed: true,
      packaged: Boolean(executablePath),
      checks,
      sources: snapshot.Sources.length,
      entries: snapshot.Categories.reduce((n, c) => n + c.Devices.length, 0),
    };
    await fs.writeFile(
      path.join(directory, "desktop-test.json"),
      JSON.stringify(summary, null, 2),
    );
    console.log(JSON.stringify(summary, null, 2));
  } catch (error) {
    if (page)
      await page
        .screenshot({ path: path.join(directory, "failure.png") })
        .catch(() => {});
    await fs.writeFile(
      path.join(directory, "desktop-test.json"),
      JSON.stringify(
        { passed: false, checks, errors, error: String(error.stack) },
        null,
        2,
      ),
    );
    throw error;
  } finally {
    await app.close();
  }
}
run().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
