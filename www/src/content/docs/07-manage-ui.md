---
title: Manage UI
description: List sources and API keys in the Blazor dashboard at /manage.
order: 8
---

The dashboard includes a pre-alpha **Manage** console for sources and API keys.

## Open Manage

**Local:** [http://localhost:5081/manage](http://localhost:5081/manage)

![Manage sources and API keys](/docs/screenshots/manage.png)

From the home page, use **Manage sources & keys**, or the **Manage** link in the header.

## Sources tab

- Lists every **source name** seen in persisted logs (from `log_entries`)
- Shows entry count and last received time
- **Tail** opens `/tail/{source}` for live viewing
- **New source** walks you through picking a name and shows a sample curl — sources exist once you ingest with that name

There is no separate “sources” table yet; names are created on first ingest.

## API keys tab

- Lists keys by name, prefix (`ql_{prefix}_…`), status, and last used time
- **Create API key** — plaintext shown once in a dialog; save it immediately
- **Revoke** — deactivates the key; ingest with that key returns **401**

## Requirements

- API running with **Postgres** configured (keys and source stats need persistence)
- Management API enabled — on by default in **Development** (`QikLog:Management:Enabled=true`)

Production disables management endpoints until login (#12) protects them.

## Management API (optional)

When enabled, the API also exposes:

| Method | Path | Purpose |
|--------|------|---------|
| GET | `/v1/sources` | List source summaries |
| GET | `/v1/keys` | List API keys (no secrets) |
| POST | `/v1/keys` | Create key (plaintext once) |
| POST | `/v1/keys/{id}/revoke` | Revoke key |

## Next steps

- [API keys](/docs/api-keys/) — use keys on ingest
- [Live tail](/docs/live-tail/) — watch a source after ingesting
