# QikLog Core Upgrades

Work package for the existing QikLog repo (https://github.com/qiklog-com/qiklog).

Adds the features that make the core product desirable for .NET developers and agents in 2026/2027: structured properties, OpenTelemetry correlation, x402 pay-per-use, and a one-command local mode.

## Why this project

The other services (Trace Store, Cost Meter, etc.) assume a solid core. This pack brings the current pre-alpha up to the level needed to support them.

## Relation to the family

- All other packs reuse patterns from this work (x402 middleware, Postgres JSONB, SignalR).
- Do this first or in parallel with Agent Trace Store.

## High-bar testing

Reqnroll features in `features/`. Keep the existing test style. Domain language first.

## Suggested order inside this pack

1. Structured properties on ingest + Serilog sink
2. TraceId / SpanId capture
3. x402 middleware (free tier then 402)
4. `qiklog serve` local mode (SQLite)
