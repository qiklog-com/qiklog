You are building the Token & Cost Meter service, a companion micropayment endpoint for QikLog and the Agent Trace Store.

Goal: A simple, fast POST endpoint that records tokens + cost for a model call and returns the running total for an agent or session. Support x402 when free volume is exceeded.

## Requirements

1. Domain:
   - CostEvent: AgentName, SessionId (optional), Model, InputTokens, OutputTokens, CostUsd, Timestamp
   - RunningTotal: computed or stored per agent/session

2. API:
   - POST /v1/cost — body: agent, session?, model, inputTokens, outputTokens, costUsd
   - Response: { accepted: true, runningTotalUsd, eventId }
   - GET /v1/cost/{agent} or ?session= — current total

3. Rules:
   - Cost and tokens must be >= 0
   - Running total is the sum of accepted events
   - Idempotency key optional (header) so retries do not double-count

4. x402:
   - Same pattern as other services: 402 + PAYMENT-REQUIRED when free tier exceeded
   - Accept payment and then record the event

5. Testing:
   - Reqnroll features already provided
   - Step definitions exercise the application service
   - Unit tests for the total calculation and validation

6. Performance note:
   - This endpoint will be called frequently. Keep the hot path light. Prefer minimal allocations.

## Deliverables

- Working service that starts locally
- Features green
- Documented payload and example curl / .NET client snippet

Start with the domain and the POST endpoint, then the running total, then wire the feature tests.
