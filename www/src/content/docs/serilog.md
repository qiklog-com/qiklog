---
title: Serilog
description: One-line Serilog sink that POSTs to QikLog ingest.
order: 6
---

Ship logs from a .NET app with one line after the package reference. The sink
POSTs to the same `POST /v1/logs` contract as `qiklog send` and the try-it-now curl.

## Install

```bash
dotnet add package QikLog.Serilog
```

Until the package is on nuget.org, pack from this repo:

```bash
make pack-serilog
dotnet add package QikLog.Serilog --source artifacts/nuget
```

## One line

```csharp
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.QikLog("https://api.qiklog.com", apiKey, "demo")
    .CreateLogger();

Log.Information("hello from serilog");
```

`apiKey` is sent as `Authorization: Bearer`. `source` is the QikLog stream name
(use `demo` to see lines on the landing live panel and `/tail/demo`).

Events flush in batches of 50 or every 2 seconds. Ingest failures go to Serilog
SelfLog; they do not throw into your app.

```csharp
Serilog.Debugging.SelfLog.Enable(Console.Error);
```

## Next steps

- [Ingest API](/docs/ingest-api/) — JSON fields and status codes
- [API keys](/docs/api-keys/) — mint a Bearer key
- [Live tail](/docs/live-tail/) — watch the source in the browser
- [CLI](/docs/cli/) — send and watch from a terminal
