You are working in the existing QikLog repository (ASP.NET Core 9, Blazor, Postgres, Serilog sink, SignalR, CLI).

Goal: Add the core upgrades that make QikLog ready for .NET developers and AI agents: structured properties, OpenTelemetry correlation, optional x402 payments, and a simple local single-command mode.

## Requirements

1. Structured properties
   - Extend POST /v1/logs and the LogEntry model to accept an optional properties bag (Dictionary or JsonElement).
   - Persist as JSONB in the Postgres log_entries table (EF Core).
   - Update QikLog.Serilog sink so structured Serilog properties are automatically extracted and sent.
   - Existing source/level/message continue to work. Live SignalR tail still works.

2. OpenTelemetry correlation
   - Capture TraceId and SpanId from W3C traceparent header or System.Diagnostics.Activity.Current.
   - Store them on the log entry.
   - Show TraceId in the Blazor dashboard (clickable placeholder is fine).
   - Serilog sink includes Activity.Current when present. No extra allocations when no Activity.

3. x402 pay-per-use
   - Middleware that returns HTTP 402 + PAYMENT-REQUIRED (x402 v2) when free-tier volume is exceeded.
   - Accept PAYMENT-SIGNATURE, verify via a facilitator interface (stub for local/dev).
   - Configurable price, free volume, and networks (Base / Solana USDC).
   - No API key required when a valid payment is presented.
   - Local demo mode stays free.

4. Local single-command mode
   - `qiklog serve` in the CLI starts API + Blazor dashboard using SQLite (no Docker required).
   - Same /v1/logs endpoint.
   - README snippet with one-line install and one-line Serilog config pointing at localhost.

5. Testing (high bar)
   - Use Reqnroll. Feature files are already in features/.
   - Step definitions exercise the application / domain layer.
   - Keep or extend existing unit/integration tests.
   - Cover the new scenarios.

## Constraints

- Do not break the current local docker-compose + make workflow.
- Prefer minimal changes to hot paths.
- Reuse existing project structure (QikLog.Core, QikLog.Api, QikLog.Serilog, QikLog.Cli, QikLog.Web).

## Deliverables

- All four upgrades working
- Features green
- Updated quickstart in README or docs/

Start with structured properties (model + endpoint + sink + test), then correlation, then x402, then the serve command.
