# Token & Cost Meter

Tiny, high-frequency endpoint that records a model call’s token usage and cost, then returns the running total for an agent or session.

Perfect micropayment surface. Agents or dashboards hit it after every LLM call. Pairs naturally with the Agent Trace Store.

## Why this project

Agents need an authoritative running cost without parsing every log. This is the simplest paid endpoint you can offer.

## Relation to QikLog / Agent Trace Store

- Same x402 middleware
- Can write a summary row into the same Postgres or a dedicated lightweight table
- Live SignalR optional (cost ticks)

## High-bar testing

Reqnroll features drive the domain rules: cost never negative, totals are correct, free tier vs paid.

## Suggested stack

- ASP.NET Core 9 minimal API
- EF Core or even a simple table
- Reqnroll
