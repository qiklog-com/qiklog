<p align="center">
  <img src="src/QikLog.Web/wwwroot/brand/lockup.svg" alt="QikLog" width="360" />
</p>

# QikLog

Lightweight log tailing for developers who want to see what their app is doing right now — without setting up Datadog.

**Status:** pre-alpha. Hello World + Postgres ingest persistence locally; paid product not yet available.

## Console apps (WebView shells)

Native wrappers around the hosted dashboard live in [`clients/`](clients/README.md)
(iOS · Android · desktop). Same admin UI in a `WKWebView` / `WebView` / Electron window.

## Quickstart (local dev)

See **[docs/QUICKSTART.md](docs/QUICKSTART.md)** for demo mode (auth off) and full dev mode (Zitadel OIDC).

```bash
make up-d          # or: docker compose up -d  (demo mode — auth disabled in compose)
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

Static landing page and **end-user docs** in [`www/`](www/) — Astro + frosted-glass CSS. Not the Blazor dashboard. Guides live at `/docs/` (quickstart, ingest API, live tail, API keys, CLI).

```bash
make www-dev       # http://localhost:4321  →  /docs/quickstart/
```

Deploy `www/dist/` to your static host; point `app.qiklog.com` at the Web container when ready.

Doc screenshots and terminal GIFs: see [docs/DOC_CAPTURE.md](docs/DOC_CAPTURE.md) (`make docs-capture`, `make demos-record`).

## Architecture

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for full detail.

- **QikLog.Core** — domain models, validation, shared contracts. Referenced by everything.
- **QikLog.Api** — ASP.NET Core 9 minimal API + SignalR hub. Ingest + real-time.
- **QikLog.Web** — Blazor Server dashboard. Fluent UI Blazor components.
- **QikLog.Cli** — single-file .NET tool. `qiklog tail`, `qiklog send`. Distributed via Homebrew/Scoop/winget.

## Project plan

See [docs/PROJECT_PLAN.md](docs/PROJECT_PLAN.md) for the phased roadmap and ticket list.

## License

© 2026 QikLog. All rights reserved.
