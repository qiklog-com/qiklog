#!/usr/bin/env bash
# One-shot Stripe setup for QikLog billing. Safe to re-run.
#
#   scripts/stripe-setup.sh          # test mode (default)
#   scripts/stripe-setup.sh --live   # live mode, after the Stripe account is activated
#
# Needs: `stripe login` done, `railway` CLI linked to the qiklog project, python3.
# Creates (or reuses) the Pro product and $9/month price, the webhook endpoint, and a
# Customer Portal configuration, then sets the api service's QikLog__Stripe__* variables.
# Secrets go straight from Stripe to Railway; nothing secret is printed.
set -euo pipefail

MODE_FLAG=()
MODE=test
if [[ "${1:-}" == "--live" ]]; then
  MODE_FLAG=(--live)
  MODE=live
fi

API_URL="${QIKLOG_API_URL:-https://api.qiklog.com}"
APP_URL="${QIKLOG_APP_URL:-https://app.qiklog.com}"
RAILWAY_SERVICE="${RAILWAY_SERVICE:-api}"
WEBHOOK_URL="$API_URL/v1/billing/webhook"
PRICE_LOOKUP_KEY="qiklog_pro_monthly"
EVENTS=(checkout.session.completed customer.subscription.created customer.subscription.updated customer.subscription.deleted)

say()  { printf '→ %s\n' "$1"; }
ok()   { printf '✓ %s\n' "$1"; }
fail() { printf '✗ %s\n' "$1" >&2; exit 1; }

json() { python3 -c "import json,sys; d=json.load(sys.stdin); print(eval(sys.argv[1]))" "$1"; }
# ${arr[@]+...}: macOS ships bash 3.2, where expanding an empty array under `set -u` is an
# "unbound variable" error that kills the script.
stripe_cli() { stripe "$@" ${MODE_FLAG[@]+"${MODE_FLAG[@]}"}; }

command -v stripe  >/dev/null || fail "stripe CLI not found (brew install stripe/stripe-cli/stripe)"
command -v railway >/dev/null || fail "railway CLI not found"
command -v python3 >/dev/null || fail "python3 not found"
if ! LOGIN_CHECK=$(stripe_cli products list --limit 1 2>&1); then
  printf '%s\n' "$LOGIN_CHECK" | tail -3 >&2
  fail "stripe CLI cannot reach your account in $MODE mode. Run: stripe login"
fi
unset LOGIN_CHECK
railway status >/dev/null 2>&1 || fail "railway is not linked here. Run: railway link"

# Any later failure names the line instead of exiting silently.
trap 'printf "✗ stopped at line %s (exit %s). Nothing after this step ran.\n" "$LINENO" "$?" >&2' ERR

echo "QikLog Stripe setup ($MODE mode) → $WEBHOOK_URL"

# 1. Product
say "Pro product"
PRODUCT_ID=$(stripe_cli products search --query "metadata['qiklog_plan']:'pro'" | json "d['data'][0]['id'] if d['data'] else ''")
if [[ -z "$PRODUCT_ID" ]]; then
  # A product made by hand in the dashboard has no metadata; match it by name.
  PRODUCT_ID=$(stripe_cli products list --limit 100 -d "active=true" | json "next((p['id'] for p in d['data'] if p['name']=='QikLog Pro'), '')")
fi
if [[ -z "$PRODUCT_ID" ]]; then
  PRODUCT_ID=$(stripe_cli products create --name "QikLog Pro" \
    -d "description=500,000 log lines a month, live tail, API keys." \
    -d "metadata[qiklog_plan]=pro" | json "d['id']")
  ok "created $PRODUCT_ID"
else
  ok "reusing $PRODUCT_ID"
fi

# 2. Price: $9/month, found by lookup key so re-runs reuse it
say "Pro price (\$9/month)"
PRICE_ID=$(stripe_cli prices list -d "lookup_keys[]=$PRICE_LOOKUP_KEY" -d "active=true" | json "d['data'][0]['id'] if d['data'] else ''")
if [[ -z "$PRICE_ID" ]]; then
  # Or an existing $9/month price on that product (made by hand, no lookup key).
  PRICE_ID=$(stripe_cli prices list --product "$PRODUCT_ID" -d "active=true" --limit 100 | json "next((p['id'] for p in d['data'] if p['unit_amount']==900 and p['currency']=='usd' and (p.get('recurring') or {}).get('interval')=='month' and (p.get('recurring') or {}).get('interval_count',1)==1), '')")
fi
if [[ -z "$PRICE_ID" ]]; then
  PRICE_ID=$(stripe_cli prices create --product "$PRODUCT_ID" --unit-amount 900 --currency usd \
    -d "recurring[interval]=month" -d "lookup_key=$PRICE_LOOKUP_KEY" | json "d['id']")
  ok "created $PRICE_ID"
else
  ok "reusing $PRICE_ID"
fi

# 3. Webhook endpoint. Stripe shows the signing secret only at creation, so an existing
#    endpoint for this URL is replaced (it belongs to QikLog; nothing else uses it).
say "Webhook endpoint"
EXISTING=$(stripe_cli webhook_endpoints list --limit 100 | python3 -c "
import json,sys
d=json.load(sys.stdin)
print(' '.join(e['id'] for e in d['data'] if e['url']=='$WEBHOOK_URL'))")
for id in $EXISTING; do
  stripe_cli webhook_endpoints delete "$id" --confirm >/dev/null
  ok "replaced old endpoint $id"
done
EVENT_ARGS=()
for e in "${EVENTS[@]}"; do EVENT_ARGS+=(-d "enabled_events[]=$e"); done
WEBHOOK_JSON=$(stripe_cli webhook_endpoints create --url "$WEBHOOK_URL" "${EVENT_ARGS[@]}" \
  -d "description=QikLog plan changes")
WEBHOOK_ID=$(printf '%s' "$WEBHOOK_JSON" | json "d['id']")
WEBHOOK_SECRET=$(printf '%s' "$WEBHOOK_JSON" | json "d['secret']")
unset WEBHOOK_JSON
ok "created $WEBHOOK_ID (${#EVENTS[@]} events)"

# 4. Customer Portal: cancel at period end, card updates, invoices. No retention offers.
say "Customer Portal configuration"
PORTAL_ID=$(stripe_cli billing_portal configurations create \
  -d "business_profile[headline]=QikLog billing" \
  -d "default_return_url=$APP_URL/billing" \
  -d "features[subscription_cancel][enabled]=true" \
  -d "features[subscription_cancel][mode]=at_period_end" \
  -d "features[payment_method_update][enabled]=true" \
  -d "features[invoice_history][enabled]=true" \
  -d "features[customer_update][enabled]=false" | json "d['id']")
ok "created $PORTAL_ID"

# 5. Secret key: typed by you, never echoed
printf 'Paste the %s secret key (sk_%s_...) from Stripe → Developers → API keys: ' "$MODE" "$MODE"
read -rs SECRET_KEY; echo
[[ "$SECRET_KEY" == sk_${MODE}_* || "$SECRET_KEY" == rk_${MODE}_* ]] || fail "that is not a $MODE secret key"

# 6. Railway (one --set call → one redeploy)
say "Railway $RAILWAY_SERVICE variables"
railway variables --service "$RAILWAY_SERVICE" \
  --set "QikLog__Stripe__Enabled=true" \
  --set "QikLog__Stripe__SecretKey=$SECRET_KEY" \
  --set "QikLog__Stripe__WebhookSecret=$WEBHOOK_SECRET" \
  --set "QikLog__Stripe__ProPriceId=$PRICE_ID" \
  --set "QikLog__Stripe__PortalConfigurationId=$PORTAL_ID" \
  --set "QikLog__Stripe__SuccessUrl=$APP_URL/billing?success=1" \
  --set "QikLog__Stripe__CancelUrl=$APP_URL/billing?canceled=1" \
  --set "QikLog__Stripe__PortalReturnUrl=$APP_URL/billing" >/dev/null
unset SECRET_KEY WEBHOOK_SECRET
ok "set 8 variables; Railway is redeploying $RAILWAY_SERVICE"

cat <<DONE

Done ($MODE mode). Still to do by hand in the Stripe dashboard ($MODE mode):
  • Settings → Billing → Subscriptions and emails → Manage failed payments:
    after the last retry, "cancel the subscription" or "mark as unpaid" (never "leave past due").
Then test: sign in at $APP_URL/billing → Upgrade to Pro$( [[ $MODE == test ]] && echo " → card 4242 4242 4242 4242, any future date, any CVC").
DONE
