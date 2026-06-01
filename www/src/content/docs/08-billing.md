---
title: Billing
description: Stripe Pro checkout and usage limits.
order: 8
---

## Plans

| Plan | Ingest / month | Price |
|------|----------------|-------|
| Free | 10,000 (default) | $0 |
| Pro | 500,000 (default) | $9/mo via Stripe |

Limits return **HTTP 402** when exceeded.

## Upgrade (dashboard)

Sign in (when `QikLog:Auth` is enabled), open **Billing**, and start Stripe Checkout.

Requires API configuration:

- `QikLog:Stripe:Enabled=true`
- `QikLog:Stripe:SecretKey` (test or live)
- `QikLog:Stripe:ProPriceId` from your Stripe product

## Next steps

- [API keys](/docs/api-keys/) — secure ingest
- [Terms](/terms/) — service terms (draft)
