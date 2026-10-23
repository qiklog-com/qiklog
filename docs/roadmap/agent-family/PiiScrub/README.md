# PII Scrub Service

Send a log line, prompt, or JSON payload. Get back a redacted version. Pay per call via x402.

Useful for compliance before storing or forwarding agent traffic.

## Why this project

Agents and services need a simple, paid redaction step. High volume, small payload, clear value.

## Relation to the family

- Same x402 middleware
- Can be called from the Agent Trace Store or QikLog ingest as an optional step
- Stateless preferred (no long-term storage of original text)

## High-bar testing

Reqnroll features cover common PII types and the payment flow.

## Suggested stack

- ASP.NET Core 9
- Simple regex or Microsoft.ML / custom detectors for email, phone, SSN, credit card, API keys
- Reqnroll
