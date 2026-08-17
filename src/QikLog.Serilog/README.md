# QikLog.Serilog

Serilog sink for [QikLog](https://www.qiklog.com). One line in `Program.cs` POSTs
to the same ingest endpoint as `qiklog send` and the try-it-now curl.

Package is packed with `make pack-serilog`. Publishing to nuget.org is a separate
manual step.

## Install

```bash
dotnet add package QikLog.Serilog
```

Until the package is on nuget.org, add a local source:

```bash
make pack-serilog
dotnet add package QikLog.Serilog --source artifacts/nuget
```

## Usage

```csharp
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.QikLog("https://api.qiklog.com", apiKey, "demo")
    .CreateLogger();

Log.Information("hello from serilog");
```

`apiKey` is sent as `Authorization: Bearer`, matching the CLI. Events are batched
(50, or 2 seconds) and posted one-at-a-time to `POST /v1/logs`. Ingest failures
are written to Serilog SelfLog; they do not throw into your app.

```csharp
Serilog.Debugging.SelfLog.Enable(Console.Error);
```
