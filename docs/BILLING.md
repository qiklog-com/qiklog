# Billing (Stripe)

Plans: **Free** 10,000 log lines a month, **Pro** $9/month for 500,000. Limits come from
`QikLog:Usage`. Hitting the cap returns 402 on ingest only; reading logs always works.

## How a plan changes

1. Billing page calls `POST /v1/billing/checkout`. The session carries the tenant id in
   `metadata.tenant_id`, `client_reference_id`, and `subscription_data.metadata.tenant_id`.
2. Stripe calls `POST /v1/billing/webhook`. The API verifies `Stripe-Signature`, records the event
   id (idempotency), and updates the tenant:
   - `checkout.session.completed` (paid) → Pro
   - `customer.subscription.created` / `updated`: `active`, `trialing` → Pro;
     `canceled`, `unpaid`, `incomplete_expired`, `paused` → Free; `past_due`, `incomplete` → no change
   - `customer.subscription.deleted` → Free
   - Events older than the newest one already applied to the tenant are ignored (Stripe retries
     and reorders deliveries). A tenant with a live subscription gets 409 from checkout.
3. Cancel and card changes happen in the Stripe Customer Portal (`POST /v1/billing/portal`).
   Cancellation at period end keeps Pro until Stripe sends `customer.subscription.deleted`.

## Configuration (API service)

| Variable | Value |
|---|---|
| `QikLog__Stripe__Enabled` | `true` |
| `QikLog__Stripe__SecretKey` | `sk_test_…` (test) or `sk_live_…` (live) |
| `QikLog__Stripe__WebhookSecret` | `whsec_…` from the webhook endpoint |
| `QikLog__Stripe__ProPriceId` | `price_…` for Pro, $9/month recurring |
| `QikLog__Stripe__SuccessUrl` | `https://app.qiklog.com/billing?success=1` |
| `QikLog__Stripe__CancelUrl` | `https://app.qiklog.com/billing?canceled=1` |
| `QikLog__Stripe__PortalReturnUrl` | `https://app.qiklog.com/billing` |

## Stripe dashboard

1. Developers → Webhooks → Add endpoint: `https://api.qiklog.com/v1/billing/webhook`.
2. Events: `checkout.session.completed`, `customer.subscription.created`,
   `customer.subscription.updated`, `customer.subscription.deleted`.
3. Copy the signing secret into `QikLog__Stripe__WebhookSecret`.
4. Settings → Billing → Subscriptions and emails → "Manage failed payments": after the final retry,
   **cancel the subscription** or **mark it unpaid**. Do not choose "leave past due": QikLog keeps
   Pro during `past_due`, so that setting would keep Pro forever without payment.
5. Settings → Billing → Customer portal: allow cancellation **at end of billing period**, allow
   payment method updates, invoice history on. No retention offers.

Test mode and live mode have separate keys, prices, webhook endpoints, and signing secrets.

## Local testing

```sh
stripe listen --forward-to localhost:5080/v1/billing/webhook   # prints a whsec_ secret
# set QikLog__Stripe__WebhookSecret to it, run the API, then:
stripe trigger checkout.session.completed
```

`stripe trigger` creates its own customer without our metadata, so the event lands as
`TenantNotFound`. For a real end-to-end test, start checkout from the Billing page while signed in.

## Data

`tenants` gains `StripeCustomerId`, `StripeSubscriptionId`, `PlanChangedAt`. Handled event ids go in
`processed_stripe_events`. Migration: `AddStripeBilling` (additive only).
