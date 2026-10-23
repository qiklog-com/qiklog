# Agent Trace Store

Lightweight store for full AI agent runs (prompt, tool calls, tokens, cost, outcome).

Builds on QikLog. Reuses ingest pattern, SignalR live tail, and Postgres. Agents POST a complete run; humans or other agents query by agent name, session, or TraceId. x402 pay-per-query or pay-per-ingest once free tier is exceeded.

## Why this project

Raw logs are not enough for agents. Need structured runs that include cost and outcome so you can debug, bill, or improve agents.

## Relation to QikLog

- Same ASP.NET Core 9 minimal API style
- Same SignalR hub pattern for live tail of new runs
- Can share the same Postgres or use a separate schema
- x402 middleware can be copied from the core QikLog x402 work

## High-bar testing

Reqnroll (Cucumber for .NET) feature files in `features/`.  
Domain language first. Steps call application services, not UI.  
Aim for the feature files to be the living spec.

## Suggested stack

- ASP.NET Core 9
- EF Core + Postgres (JSONB for tool calls)
- Reqnroll + xUnit
- Optional: Microsoft.Extensions.AI types for the payload shape
