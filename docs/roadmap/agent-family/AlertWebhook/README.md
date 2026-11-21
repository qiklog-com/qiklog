# Alert Webhook

Structured conditions on logs or agent runs fire a paid webhook (or Slack). Agents and services subscribe.

## Why this project

Turns stored telemetry into action. Natural paid surface once you have the query layer.

## Relation to the family

- Evaluates conditions against QikLog / Trace Store data
- Same x402 for subscription or per-alert
- Can reuse the Structured Query Proxy filters

## High-bar testing

Reqnroll features for condition matching and delivery.

## Suggested stack

- ASP.NET Core 9
- Background service or hosted service that evaluates rules
- HttpClient for outbound webhooks
- Reqnroll
