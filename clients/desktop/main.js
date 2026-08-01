const { app, BrowserWindow, shell, session } = require("electron");
const path = require("path");

const START_URL =
  (process.env.QIKLOG_APP_URL || "https://qiklog.up.railway.app").trim();

const ALLOWED_HOST_SUFFIXES = [
  "qiklog.up.railway.app",
  "qiklog.com",
  "signin.qiklog.com",
  "zitadel.cloud",
  "localhost",
  "127.0.0.1",
];

function isAllowedNavigation(urlString) {
  try {
    const url = new URL(urlString);
    if (url.protocol !== "http:" && url.protocol !== "https:") {
      return false;
    }
    const host = url.hostname.toLowerCase();
    return ALLOWED_HOST_SUFFIXES.some(
      (allowed) => host === allowed || host.endsWith(`.${allowed}`)
    );
  } catch {
    return false;
  }
}

function createWindow() {
  const win = new BrowserWindow({
    width: 1280,
    height: 840,
    minWidth: 900,
    minHeight: 600,
    title: "QikLog Console",
    backgroundColor: "#0d1117",
    webPreferences: {
      preload: path.join(__dirname, "preload.js"),
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: true,
    },
  });

  // Identify this shell in logs / future feature detection.
  session.defaultSession.webRequest.onBeforeSendHeaders((details, callback) => {
    const headers = details.requestHeaders;
    const ua = headers["User-Agent"] || "";
    headers["User-Agent"] = `${ua} QikLogConsole-Desktop/1.0`;
    callback({ requestHeaders: headers });
  });

  win.webContents.setWindowOpenHandler(({ url }) => {
    if (isAllowedNavigation(url)) {
      return { action: "allow" };
    }
    shell.openExternal(url);
    return { action: "deny" };
  });

  win.webContents.on("will-navigate", (event, url) => {
    if (!isAllowedNavigation(url)) {
      event.preventDefault();
      shell.openExternal(url);
    }
  });

  win.loadURL(START_URL);
}

app.whenReady().then(() => {
  createWindow();

  app.on("activate", () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      createWindow();
    }
  });
});

app.on("window-all-closed", () => {
  if (process.platform !== "darwin") {
    app.quit();
  }
});
