---
title: Live tail
description: Watch logs stream in the browser with the Blazor dashboard.
order: 4
---

The dashboard is where you **watch** logs in real time.

## Open a tail

URL pattern:

```text
/tail/{source}
```

**Local example:** [http://localhost:5081/tail/demo](http://localhost:5081/tail/demo)

Marketing landing scene 3 embeds the same panel at `/embed/tail/{source}` (minimal chrome, iframe-friendly).

Replace `demo` with whatever `source` name you use when ingesting.

## What you see

- **Timestamp** — when the event occurred (or when the server received it)
- **Level** — color-coded severity
- **Message** — the log body
- **Status badge** — SignalR connection state (`connected`, `reconnecting`, etc.)
- **Auto-scroll** — follows new lines (toggle off to freeze the viewport)
- **Clear** — empties the on-screen buffer (does not delete stored history)

The on-screen buffer keeps the most recent **500** lines. Older lines drop from the view but remain in Postgres if persistence is enabled.

Turn on **History** (default on) to preload the latest stored rows for that source from `GET /v1/sources/{source}/logs` before live SignalR events arrive.

## How it works

The web app connects to the API SignalR hub and joins group `source:{name}`. Every successful `POST /v1/logs` for that source is pushed to your browser immediately.

## Tail and authentication

Whether the tail page needs credentials depends on how the API is configured:

| API config | What the tail page does |
| --- | --- |
| `QikLog__AuthEnforcement__Enabled=false` (default local demo) | Connects anonymously; no key needed to view |
| `QikLog__AuthEnforcement__Enabled=true` (production) | The hub requires a tenant credential — an OIDC session or `QikLog__HubApiKey` on the web container |

When enforcement is on and no credential is available, the hub handshake is rejected. The page still renders: the status badge shows `disconnected` and a **Live tail unavailable** notice appears above the viewport. Stored history still loads if the web app can reach `GET /v1/sources/{source}/logs`.

To give the dashboard a credential, set `QikLog__HubApiKey` on the web container to an active API key. The same key is used for the hub and for reading stored history, so both the live stream and the preloaded rows work.

## Tips

- Use one source per service or environment so tabs stay focused.
- Keep the tail tab open while reproducing a bug, then ship logs from your app or curl in another terminal.
- If the badge stays `disconnected`, check that the API is running and that `QikLog__ApiBaseUrl` in the web container points at the API (Docker Compose sets this automatically).
- A **Live tail unavailable** notice mentioning a rejected connection means the API enforced auth and the web app had no credential — see the table above.

## Next steps

- [API keys](/docs/api-keys/) — lock down ingest before you ship to production
- [CLI](/docs/cli/) — send and tail-file from your terminal
