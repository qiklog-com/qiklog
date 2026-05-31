<p align="center">
  <img src="src/QikLog.Web/wwwroot/brand/lockup.svg" alt="QikLog" width="360" />
</p>

# QikLog

Lightweight log tailing for developers who want to see what their app is doing right now — without setting up Datadog.

**Status:** pre-alpha. Hello World works locally; paid product not yet available.

## Quickstart (local dev)

```bash
make up-d          # or: docker compose up -d
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

You should see the log line appear in the browser in real time.

## Marketing site (www.qiklog.com)

Static landing page in [`www/`](www/) — Astro + frosted-glass CSS. Not the Blazor dashboard.

```bash
make www-dev       # http://localhost:4321
```

Deploy `www/dist/` to your static host; point `app.qiklog.com` at the Web container when ready.

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
