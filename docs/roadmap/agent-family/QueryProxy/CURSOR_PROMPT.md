You are building the Structured Query Proxy — a paid query service over QikLog logs and Agent Trace Store runs.

Goal: Accept a structured filter or a short natural-language question and return matching events. Support x402.

## Requirements

1. Query model:
   - Structured: source/agent, level/outcome, time range, text contains, TraceId
   - Optional NL endpoint that converts a sentence into the structured filter (can be stubbed or use a local model later)

2. API:
   - POST /v1/query — body: structured filter or { "question": "..." }
   - Response: list of matching log entries or runs (limit + cursor)
   - GET /v1/query/{id} if you store query history (optional)

3. Backend:
   - Query the same Postgres tables used by QikLog / Trace Store
   - Efficient indexes assumed on timestamp, source, agent, traceId

4. x402:
   - Every query can be paid, or free tier then 402
   - Document the price

5. Testing:
   - Reqnroll features provided
   - Seed test data, run queries, assert results
   - Cover time-range and agent filters

## Deliverables

- Working query endpoint
- Features green
- Example queries in the README

Start with structured filters against a local SQLite or Postgres, then add the payment gate, then the feature steps.
