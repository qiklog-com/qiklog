<p align="center">
  <img src="src/QikLog.Web/wwwroot/brand/lockup.svg" alt="QikLog" width="360" />
</p>

# QikLog

Lightweight log tailing for developers who want to see what their app is doing right now — without setting up Datadog.

**Status:** pre-alpha. Hello World scaffold. Not yet usable.

## Quickstart (local dev)

```bash
# Bring up Postgres + Redis + API + Web
docker compose up

# In another terminal, send a test log
curl -X POST http://localhost:5080/v1/logs \
  -H "Content-Type: application/json" \
  -d '{"source":"demo","level":"info","message":"hello from curl"}'

# Open the live tail
open http://localhost:5081/tail/demo
```

You should see the log line appear in the browser in real time.

## Architecture

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for full detail.

- **QikLog.Core** — domain models, validation, shared contracts. Referenced by everything.
- **QikLog.Api** — ASP.NET Core 9 minimal API + SignalR hub. Ingest + real-time.
- **QikLog.Web** — Blazor Server dashboard. Fluent UI Blazor components.
- **QikLog.Cli** — single-file .NET tool. `qiklog tail`, `qiklog send`. Distributed via Homebrew/Scoop/winget.

## Project plan

See [docs/PROJECT_PLAN.md](docs/PROJECT_PLAN.md) for the phased roadmap and ticket list.

## License

MIT (planned). LICENSE file to be added before first public release.
