You are working in a new .NET 9 project that will become the Agent Trace Store service, a companion to QikLog.

Goal: Build a minimal, production-shaped service that accepts full AI agent run traces and lets callers query them. Support optional x402 payment when volume exceeds a free tier.

## Requirements

1. Domain model (in Core project):
   - AgentRun: Id, AgentName, SessionId, TraceId, StartedAt, EndedAt, Prompt, Outcome (success/failure/partial), TotalTokens, CostUsd, Status
   - ToolCall: nested or separate table — Name, Arguments (JSON), Result (JSON), DurationMs, Tokens

2. API (minimal API or controllers):
   - POST /v1/runs — ingest a full run (body matches AgentRun + tool calls)
   - GET /v1/runs?agent=&session=&traceId= — query
   - GET /v1/runs/{id}
   - SignalR hub for live “new run” notifications (same pattern as QikLog tail)

3. Persistence: EF Core, Postgres. Store tool calls as JSONB or owned entities. Index AgentName, SessionId, TraceId.

4. x402 (optional but preferred):
   - Middleware that returns 402 + PAYMENT-REQUIRED (x402 v2) when free-tier volume is exceeded.
   - Accept PAYMENT-SIGNATURE and verify via a facilitator interface (stub for local).
   - Configurable price and free volume.

5. Testing (high bar):
   - Use Reqnroll. Place feature files under features/.
   - Implement step definitions that exercise the application service layer (not just controllers).
   - Cover the scenarios already written in the feature files.
   - Add unit tests for domain rules (e.g. cost must be non-negative, outcome must be valid).

6. Local run:
   - docker-compose or simple `dotnet run` with SQLite fallback so it starts with one command.
   - README quickstart that mirrors QikLog’s make/demo style.

## Constraints

- Keep the public payload simple and documented.
- Do not require an API key when a valid x402 payment is presented.
- Reuse naming and style from QikLog (src/QikLog.Api, QikLog.Core) so the two services feel related.
- Prefer source generators or zero-allocation where it does not complicate the domain.

## Deliverables

- Solution structure: Core, Api, (optional Web for simple tail view)
- Working POST + GET + SignalR
- Reqnroll features green
- Short architecture note in docs/ or README

Start by creating the solution and the domain types, then the ingest endpoint, then the feature-driven tests.
