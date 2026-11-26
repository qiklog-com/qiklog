You are building the Alert Webhook service — conditions on logs or agent runs that fire outbound webhooks, with x402 payment.

Goal: Users (or agents) register a condition + webhook URL. When matching events arrive, the service POSTs a payload to the webhook. Support payment for the subscription or per delivery.

## Requirements

1. Domain:
   - AlertRule: name, filter (same shape as Structured Query), webhookUrl, enabled, owner
   - Delivery attempt log

2. API:
   - POST /v1/rules — create rule
   - GET /v1/rules
   - DELETE /v1/rules/{id}
   - Internal: when a new log/run arrives (or on a timer), evaluate rules and fire webhooks

3. Delivery:
   - POST JSON payload to the webhook URL
   - Retry with backoff on failure
   - Record success/failure

4. x402:
   - Creating a rule or each delivery can require payment
   - 402 pattern consistent with other services

5. Testing:
   - Reqnroll features provided
   - Use a test webhook receiver (or mock HttpClient)
   - Assert that a matching event causes a delivery

## Deliverables

- Working rule registration + evaluation + delivery
- Features green
- Example rule payload in README

Start with rule storage and a simple evaluator, then webhook delivery, then payment and feature steps.
