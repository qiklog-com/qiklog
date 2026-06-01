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

Replace `demo` with whatever `source` name you use when ingesting.

## What you see

- **Timestamp** — when the event occurred (or when the server received it)
- **Level** — color-coded severity
- **Message** — the log body
- **Status badge** — SignalR connection state (`connected`, `reconnecting`, etc.)
- **Auto-scroll** — follows new lines (toggle off to freeze the viewport)
- **Clear** — empties the on-screen buffer (does not delete stored history)

The on-screen buffer keeps the most recent **500** lines. Older lines drop from the view but remain in Postgres if persistence is enabled.

## How it works

The web app connects to the API SignalR hub and joins group `source:{name}`. Every successful `POST /v1/logs` for that source is pushed to your browser immediately.

You do **not** need an API key to **view** the tail page in the current pre-alpha build. Keys protect **ingest** only.

## Tips

- Use one source per service or environment so tabs stay focused.
- Keep the tail tab open while reproducing a bug, then ship logs from your app or curl in another terminal.
- If the badge stays `disconnected`, check that the API is running and that `QikLog__ApiBaseUrl` in the web container points at the API (Docker Compose sets this automatically).

## Next steps

- [API keys](/docs/api-keys/) — lock down ingest before you ship to production
- [CLI](/docs/cli/) — send and tail-file from your terminal
