# Roadmap (parked)

> **Status: PARKED. Do not build from these files.**
> Current scope is in `CLAUDE.md`: Milestone 1 (self-serve), then the Azure move.
> These are kept so the next phase starts from a versioned spec instead of a loose zip.
> Cursor / Claude: if a task points here, stop and confirm with Jamey first.

Source: Grok-generated prompt packs, imported verbatim on 2026-10-23 from `~/dev/qiklog-all`.
Nothing here is wired into the build, the solution, CI, or deploys.

## Two separate things

| Folder | What | Where the code will live |
| --- | --- | --- |
| [agent-family/](agent-family/) | AI-agent features on top of QikLog: run traces, cost meter, query, alerts, PII scrub, .NET toolkit | **This repo**, as modules inside the existing projects |
| x402 | Generic HTTP 402 / x402 payment middleware for ASP.NET Core | **Moved to [qiklog-com/x402-dotnet](https://github.com/qiklog-com/x402-dotnet)** (private). QikLog will consume it as a NuGet package and supply only its free-tier policy, usage counters, and tenant mapping. |

## agent-family: triage

| Pack | Phase | Notes |
| --- | --- | --- |
| CoreUpgrades | Next | `LogEntry.Properties` already exists (string dictionary). TraceId/SpanId is the useful new part. Do OpenTelemetry *ingest* separately; it is out of scope today. `qiklog serve` + SQLite would add a second persistence path, so it needs its own decision. |
| AgentTraceStore | Next | New tables + `/v1/runs` route group in `QikLog.Api`, not a separate service. Reuses tenants, API keys, LogHub. |
| CostMeter | Next | `/v1/cost` in `QikLog.Api`. Smallest paid surface; first x402 consumer. |
| QikLog.AgentToolkit | Next | New `src/QikLog.AgentToolkit` package. Publishing to NuGet needs Jamey's OK. |
| QueryProxy | Later | Same as the planned "search" item. |
| AlertWebhook | Later | Same as the planned "alerts" item (Phase 3). |
| PiiScrub | Later / maybe never | Not core to logging. Could be an ingest-time option. |

## Rules for when this is un-parked

- Modules in existing projects, one deployable API. Split a service out only if it needs to scale on its own. (Infra: every extra Container App costs money and adds a pipeline.)
- Tests follow `docs/TESTING.md`: the `.feature` files are the spec; scenarios get written as xUnit `Given_X_When_Y_Then_Z`. Reqnroll only if Jamey approves it.
- Ship new endpoints behind config flags, off by default.
- The packs still describe x402 middleware inline ("x402 middleware" sections). Treat those as superseded: QikLog builds no 402 logic of its own; it uses `qiklog-com/x402-dotnet`.
- Open design question before any x402 code: the packs say "no API key when a valid payment is presented". That leaves the tenant who owns the data, and who can read it, undefined.
