You are building the PII Scrub Service — a small paid API that redacts personal information from text or JSON.

Goal: POST a payload, receive a redacted version. Support x402. Keep it stateless.

## Requirements

1. API:
   - POST /v1/scrub — body: { "text": "..." } or raw text
   - Response: { "redacted": "...", "findings": [ { "type": "email", "count": 1 } ] }
   - Optional: accept JSON and redact string values only

2. Detection (minimum):
   - Email addresses
   - Phone numbers (US-style and simple international)
   - Credit-card-like numbers
   - Common API key patterns
   - Configurable redaction token (default "[REDACTED]")

3. x402:
   - 402 when free tier exceeded
   - Accept payment then scrub

4. Testing (high bar):
   - Reqnroll features provided
   - Unit tests for each detector
   - Integration test that posts and asserts redaction

5. Privacy:
   - Do not log the original payload
   - Document that originals are not stored

## Deliverables

- Working endpoint
- Features green
- Example of calling it from a .NET HttpClient that handles 402

Start with the detectors and the happy-path endpoint, then add payment and the feature steps.
