# QikLog marketing site (www.qiklog.com)

Static landing page and **end-user documentation** — separate from the Blazor app (`app.qiklog.com` / local `:5081`).

## Stack

- **[Astro](https://astro.build)** — HTML-first, ships zero JS by default, fast deploy to Vercel/Azure Static Web Apps/any CDN
- **Frosted glass** via `backdrop-filter` + token-aligned CSS (not liquid-glass / WebGL)

## Commands

```bash
cd www
npm install
npm run dev      # http://localhost:4321
npm run build    # dist/
```

From repo root: `make www-dev` / `make www-build`.

## Environment

Copy `.env.example` → `.env` and set `PUBLIC_APP_URL` to your dashboard URL.

## Documentation

User-facing guides live in `src/content/docs/` as Markdown (Astro content collection). Pages render at `/docs/` with sidebar navigation and prev/next links.

| File | Topic |
|------|--------|
| `getting-started.md` | Concepts and learning path |
| `quickstart.md` | Docker local run |
| `ingest-api.md` | `POST /v1/logs` |
| `live-tail.md` | Browser tail viewer |
| `api-keys.md` | Auth and rate limits |
| `cli.md` | `qiklog` commands |

Add a doc: create a new `.md` with `title`, `description`, and `order` in frontmatter.

## Deploy

Build `dist/` and host on `www.qiklog.com`. Point DNS (CNAME) at your static host; keep the product on `app.qiklog.com`.
