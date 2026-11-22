<p align="center">
  <img src="docs/assets/qiklog-lockup.svg" alt="QikLog" width="220" />
</p>

<p align="center">
  <a href="https://github.com/qiklog-com/qiklog/actions/workflows/ci.yml"><img src="https://github.com/qiklog-com/qiklog/actions/workflows/ci.yml/badge.svg" alt="CI" /></a>
  <img src="https://img.shields.io/badge/.NET-9-512BD4?logo=dotnet&logoColor=white" alt=".NET 9" />
  <img src="https://img.shields.io/badge/status-beta-B94700" alt="beta" />
  <a href="https://www.qiklog.com"><img src="https://img.shields.io/badge/www-qiklog.com-2E2A26" alt="www.qiklog.com" /></a>
  <a href="https://app.qiklog.com"><img src="https://img.shields.io/badge/app-app.qiklog.com-B94700" alt="app.qiklog.com" /></a>
</p>

# QikLog

**console.log for your server.** Get a link. Send it a line. Watch it land.

```bash
curl -X POST https://qiklog.com/l                      # get a log link, no account
curl https://qiklog.com/l/k7mq2x9d4fab -d "it broke"   # send it a line
open https://qiklog.com/l/k7mq2x9d4fab                 # watch it, live
```

- **No account, no agent, no dashboard.** The request body is the line.
- **A link is unlisted, not private.** It lasts 24 hours and holds 500 lines. Do not log secrets to it.
- **Plain text for terminals and coding agents:** `curl https://qiklog.com/l/k7mq2x9d4fab/raw`
- **An account makes it private and permanent:** your own sources and API keys. Free up to 10,000 lines a month; Pro is $9/mo for 500,000.
- **.NET first:** a Serilog sink (`WriteTo.QikLog(...)`) and a `qiklog` CLI.

Monitoring suites are for when it is serious. QikLog is for right now.

User docs: [qiklog.com/docs](https://www.qiklog.com/docs/). Plan and positioning: [docs/MARKETING_PLAN.md](docs/MARKETING_PLAN.md).

**Status:** beta. The source is public to read; all rights reserved (see License).

## Console apps (WebView shells)

Native wrappers around the hosted dashboard live in [`clients/`](clients/README.md)
(iOS · Android · desktop). Same admin UI in a `WKWebView` / `WebView` / Electron window.

## Quickstart (local dev)

See **[docs/QUICKSTART.md](docs/QUICKSTART.md)** for demo mode (auth off) and full dev mode (Zitadel OIDC).

```bash
make up-d          # or: docker compose up -d  (demo mode: auth disabled in compose)
make verify        # build, test, health + demo POST
make demo          # send a test log
open http://localhost:5081/tail/demo
```

Manual curl:

```bash
curl -X POST http://localhost:5080/v1/logs \
  -H "Content-Type: application/json" \
  -d '{"source":"demo","level":"info","message":"hello from curl"}'
```

You should see the log line appear in the browser in real time. Ingested lines are stored in Postgres (`log_entries` table).

`GET /healthz` reports `postgres: ok` when the database is reachable.

### API keys (optional locally, required in Production)

```bash
# Create a dev key (API must be running in Development)
dotnet run --project src/QikLog.Cli -- key create --name "local"

# Send with key
export QIKLOG_API_KEY=ql_...
dotnet run --project src/QikLog.Cli -- send -s demo -m "hello" --key "$QIKLOG_API_KEY"
```

Docker Compose keeps `QikLog__Ingest__RequireApiKey=false` so the README curl still works without a key.

## Marketing site (www.qiklog.com)

Static landing page and **end-user docs** in [`www/`](www/): Astro + Field Clinical brand CSS. Not the Blazor dashboard. Guides live at `/docs/` (quickstart, ingest API, live tail, API keys, CLI).

```bash
make www-dev       # http://localhost:4321  →  /docs/quickstart/
```

Deploy `www/dist/` to your static host; point `app.qiklog.com` at the Web container when ready.

Doc screenshots and terminal GIFs: see [docs/DOC_CAPTURE.md](docs/DOC_CAPTURE.md) (`make docs-capture`, `make demos-record`).

## Architecture

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for full detail.

- **QikLog.Core**: domain models, validation, shared contracts. Referenced by everything.
- **QikLog.Api**: ASP.NET Core 9 minimal API + SignalR hub. Ingest + real-time.
- **QikLog.Web**: Blazor Server dashboard. Fluent UI Blazor components.
- **QikLog.Cli**: single-file .NET tool. `qiklog send`, `qiklog watch`, `qiklog tail-file`. Install with `make install-cli`.
- **QikLog.Serilog**: Serilog sink. `WriteTo.QikLog(apiUrl, apiKey, source)`. Pack with `make pack-serilog` (not published to nuget.org from CI).

## Project plan

See [docs/PROJECT_PLAN.md](docs/PROJECT_PLAN.md) for the phased roadmap and ticket list.

## License

© 2026 QikLog. All rights reserved.
