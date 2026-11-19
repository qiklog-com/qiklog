# Billing webhook and Customer Portal

Status: implemented on `feat/stripe-webhook`. Tests: `tests/QikLog.Api.Tests/StripeWebhookTests.cs`,
`StripeBillingEventsTests.cs`, `ProtectedApiRoutesTests.cs`.

## Problem

`POST /v1/billing/checkout` takes a payment, but nothing ever set `Tenant.Plan = "pro"`. A paying
customer kept the Free cap. There was also no way to cancel without emailing Jamey.

## Scope

- `POST /v1/billing/webhook`: Stripe events change the tenant's plan.
- `POST /v1/billing/portal`: Stripe Customer Portal for card changes, invoices, and cancel.
- `GET /v1/billing/status`: plan and this month's usage for the Billing page.
- Checkout tags the tenant on the session and on the subscription.
- Billing page shows plan and usage, Upgrade (Free) or Manage billing (Pro).

Out of scope: live Stripe keys, proration, annual plans, team seats, the Zitadel default-org guard
(belongs on top of PR #29, which rewrites `TenantProvisioner`).

## Conditions of Satisfaction

1. A verified `checkout.session.completed` (subscription, paid) sets the tenant to Pro and stores
   the Stripe customer and subscription ids.
2. The webhook needs no QikLog credentials; the Stripe signature is the only authentication.
3. A missing or wrong signature returns 400 and changes nothing.
4. A redelivered event id changes nothing (idempotent).
5. `customer.subscription.deleted` and the statuses `canceled`, `unpaid`, `incomplete_expired`,
   `paused` set Free. `past_due` and `incomplete` change nothing: Stripe is still retrying.
6. An event for an older subscription than the tenant's current one changes nothing.
7. An event for an unknown tenant returns 200 so Stripe stops retrying.
8. Over the monthly cap, ingest returns 402, history stays readable. Upgrading lifts the cap.
9. The portal returns 404 before the tenant has a billing account, without calling Stripe.
10. No secrets or payloads in logs.

## Scenarios

| Given | When | Then |
|---|---|---|
| A Free tenant | Stripe sends a paid checkout for it | Pro, customer + subscription stored |
| Tenant id only in `client_reference_id` | Checkout completes | Pro |
| Async payment pending (`payment_status: unpaid`) | Checkout completes | Subscription stored, still Free; Pro on `subscription.updated active` |
| An event already processed | Stripe redelivers it | Outcome `Duplicate`, no change |
| A forged request | Signature does not verify | 400, no change |
| No signature header | Request arrives | 400 |
| A Pro tenant | Subscription deleted | Free, subscription id cleared, customer kept |
| A Pro tenant | Status `past_due` | Still Pro |
| A Pro tenant | Status `unpaid` | Free |
| A tenant on `sub_new` | Late `deleted` for `sub_old` | Outcome `StaleSubscription`, still Pro |
| Unknown tenant id | Any plan event | 200, outcome `TenantNotFound` |
| Event type we don't handle | Delivered | 200, outcome `NoChange`, recorded |
| Free tenant at its cap | Sends a log / reads history | 402 / 200 |
| Free tenant at its cap | Upgrades, then sends | 202 |
| Signed-in tenant | GET status | plan, used, limit, hasBillingAccount |
| No credentials | GET status / POST portal | 401 |
| Signed-in tenant, never paid | POST portal | 404 |
| Webhook secret not configured | Signed request | 503 |
