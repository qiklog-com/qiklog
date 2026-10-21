---
title: Manage UI
description: Create API keys, list sources, and revoke keys in the dashboard at /manage.
order: 8
---

The dashboard includes a pre-alpha **Manage** console for sources and API keys.

## Open Manage

**Local:** [http://localhost:5081/manage](http://localhost:5081/manage)

![Manage sources and API keys](/docs/screenshots/manage.png)

From the home page, use **Manage sources & keys**, or the **Manage** link in the header.

## Sign in

When sign-in is enabled, **Manage** and **Billing** require it. Opening either page
without a session sends you to the sign-in page first, then back.

Each organization gets its own workspace on first sign-in. Your sources, keys, and logs
are visible only to your organization. A new workspace starts empty: create a key, send
a line, and your first source appears.

If Manage shows *Could not load sources and API keys*, your session has expired. Sign
out, sign in again, then click **Refresh**.

In the local demo stack (sign-in off), Manage opens without a session.

## Sources tab

- Lists every **source name** seen in persisted logs (from `log_entries`)
- Shows entry count and last received time
- **Tail** opens `/tail/{source}` for live viewing
- **New source** walks you through picking a name and shows a ready-to-run curl for
  your API, using `Authorization: Bearer $QIKLOG_API_KEY`. Sources exist once you
  ingest with that name.

There is no separate “sources” table yet; names are created on first ingest.

## API keys tab

- Lists keys by name, prefix (`ql_{prefix}_…`), status, and last used time
- **Create API key**: plaintext shown once in a dialog; save it immediately
- **Revoke**: deactivates the key; ingest with that key returns **401**

## Requirements

- API running with **Postgres** configured (keys and source stats need persistence)
- Management API enabled (`QikLog:Management:Enabled=true`; on by default in **Development**)

## Management API (optional)

When enabled, the API also exposes these routes. They require a signed-in access
token (`Authorization: Bearer <JWT>`); ingest API keys are not accepted.

| Method | Path | Purpose |
|--------|------|---------|
| GET | `/v1/sources` | List source summaries |
| GET | `/v1/keys` | List API keys (no secrets) |
| POST | `/v1/keys` | Create key (plaintext once) |
| POST | `/v1/keys/{id}/revoke` | Revoke key |

## Next steps

- [API keys](/docs/api-keys/): use keys on ingest
- [Live tail](/docs/live-tail/): watch a source after ingesting
