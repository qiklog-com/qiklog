# QikLog Architecture

## One-line pitch

Real-time log tailing for developers who want to `tail -f` their production app from a browser, without setting up Datadog.

## Stack at a glance

| Layer | Choice | Why |
|-------|--------|-----|
| Frontend | Blazor Server + Fluent UI Blazor | C# end-to-end; SignalR is native; Microsoft brand alignment |
| Backend | ASP.NET Core 9 minimal API | Native to the stack; minimal ceremony for small surface |
| Real-time | SignalR | Built-in reconnect, grouping, automatic transport negotiation |
| CLI | .NET 9 + System.CommandLine, single-file publish | Code reuse with API/Web; cross-platform; AOT-able later |
| Persistence (Phase 2) | PostgreSQL 16 | Solid full-text search, JSON support, EF Core 9 maturity |
| Cache (Phase 2) | Redis 7 | Hot log buffer, rate limiting, SignalR backplane when scaling |
| Hosting | Azure Container Apps | Free tier eligible; resume value; Docker-native |
| Container registry | Azure Container Registry | Pairs with ACA, GitHub Actions integration |

## Why Blazor Server, not WASM

Blazor Server's per-user WebSocket is a perfect fit for a dashboard that already needs a persistent connection for log streaming. WASM would add a download cost with no benefit at our scale (small concurrent user count, mostly authenticated users on a dashboard). When concurrent users push past 10K we revisit. Until then, Server.

## Why SignalR, not raw SSE

The original QikLog design used SSE because the backend was Fastify. With ASP.NET Core, SignalR gives us:
- Automatic reconnect with backoff
- Group-based broadcasts (`source:{name}`)
- Symmetric API for Blazor (the client and server speak the same protocol)
- Transport fallback (WebSocket → SSE → long polling)

Switching costs are low if we ever want raw SSE for a non-.NET client; ASP.NET Core supports both.

## Data flow

```
┌─────────────┐    POST /v1/logs    ┌─────────────┐    Group broadcast    ┌─────────────┐
│  qiklog CLI │ ──────────────────► │ QikLog.Api  │ ───────────────────►  │ QikLog.Web  │
│  / ILogger  │                     │  + LogHub   │                       │  (Blazor)   │
│  / curl     │                     └──────┬──────┘                       └─────────────┘
└─────────────┘                            │
                                           │ Phase 2+
                                           ▼
                                    ┌──────────────┐
                                    │  Postgres /  │
                                    │    Redis     │
                                    └──────────────┘
```

## Phasing

- **Phase 1 (now)** — Hello World: ingest endpoint + SignalR + Blazor tail page. No persistence, no auth.
- **Phase 2** — Postgres persistence, API key auth, user accounts, source management.
- **Phase 3** — Search across stored logs, regex alerts, Slack/email integration.
- **Phase 4** — Billing (Stripe, three tiers per pricing decision), usage metering, public launch.
- **Phase 5** — Agents in other ecosystems (NuGet `ILogger` provider, npm shim, Homebrew CLI).

## Open questions

- Cold-storage strategy for old logs (S3? Azure Blob? Postgres partitioning only?). Decide in Phase 2.
- Multi-region story. Not before $5K MRR.
- Self-hostable build target. Strong argument for it as a wedge; needs its own packaging story.
