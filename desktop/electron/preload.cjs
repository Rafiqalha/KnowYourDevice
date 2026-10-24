const { contextBridge, ipcRenderer } = require("electron");
contextBridge.exposeInMainWorld("device", {
  scan: () => ipcRenderer.invoke("device:scan"),
  snapshot: (includePrivate) =>
    ipcRenderer.invoke("device:snapshot", includePrivate),
  exportReport: (format) => ipcRenderer.invoke("device:export", format),
  onMetrics: (callback) => {
    const listener = (_event, metrics) => callback(metrics);
    ipcRenderer.on("device:metrics", listener);
    return () => ipcRenderer.removeListener("device:metrics", listener);
  },
  openRepository: () => ipcRenderer.invoke("device:repository"),
  setTheme: (theme) => ipcRenderer.invoke("device:theme", theme),
});
