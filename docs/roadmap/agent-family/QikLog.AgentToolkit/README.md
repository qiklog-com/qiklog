# .NET Agent Toolkit

NuGet package that wraps Microsoft.Extensions.AI + automatic logging to QikLog / Agent Trace Store + x402 payment handling.

One package. Agents log and pay themselves.

## Why this project

Reduces friction to zero for .NET developers building agents. The distribution vehicle for the whole family of services.

## Relation to the family

- Calls the Trace Store and Cost Meter endpoints
- Handles 402 → sign → retry transparently
- Can be used standalone or with the local QikLog instance

## High-bar testing

Reqnroll features that describe the agent experience (no manual payment code).

## Suggested stack

- .NET 9 class library + sample console app
- Microsoft.Extensions.AI
- HttpClient with custom handler for x402
- Reqnroll for the library behavior
