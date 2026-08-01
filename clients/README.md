# QikLog Console — native shells

Thin WebView wrappers around the hosted dashboard
(`https://qiklog.up.railway.app`). Not a second UI — same Blazor admin in a
native window so phone / desktop feel like an app.

| Folder | Platform | Stack |
|--------|----------|--------|
| `ios/` | iPhone / iPad | SwiftUI + `WKWebView` |
| `android/` | Android | Kotlin + `WebView` |
| `desktop/` | macOS / Windows / Linux | Electron + BrowserWindow |

**Product name:** QikLog Console  
**Bundle / application id:** `com.qiklog.console`

## Start URL

Default: `https://qiklog.up.railway.app`

Override:

| Platform | How |
|----------|-----|
| iOS | `QIKLOG_APP_URL` in scheme env, or edit `AppConfig.swift` |
| Android | `QIKLOG_APP_URL` gradle property / `local.properties`, or edit `AppConfig.kt` |
| Desktop | `QIKLOG_APP_URL` env var, or `clients/desktop/.env` |

Local dashboard: `http://localhost:5081`

## Quick start

```bash
# Desktop
cd clients/desktop && npm install && npm start

# Android (SDK + device/emulator)
cd clients/android && ./gradlew :app:installDebug

# iOS (Xcode 15+)
open clients/ios/QikLogConsole.xcodeproj
# Run on a simulator or device
```

## Scope

These clients only host the web app. Auth, Manage, Tail, billing — all stay on
the server. If the web app needs camera / file pickers later, add bridge APIs
here; do not fork the dashboard into native screens.
