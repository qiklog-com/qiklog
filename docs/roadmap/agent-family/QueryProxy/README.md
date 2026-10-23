# Structured Query Proxy

Natural-language or simple structured filter over stored logs and agent runs. “Show failures for Payment service in the last hour.” Charge per query via x402.

## Why this project

Once you have logs and runs, the valuable surface is asking questions. This is the paid query layer.

## Relation to the family

- Reads from QikLog log_entries and/or Agent Trace Store tables
- Same x402 middleware
- Can be a thin service or an endpoint inside the main API

## High-bar testing

Reqnroll features describe the query language and result shape.

## Suggested stack

- ASP.NET Core 9
- EF Core queries against existing tables
- Optional: simple LLM call for NL → structured filter (or start with structured filters only)
- Reqnroll
