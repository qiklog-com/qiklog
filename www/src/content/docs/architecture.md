---
title: Architecture
description: How QikLog will run on Azure, from your app's ILogger to your browser.
order: 10
---

> **Planned setup.** QikLog is moving to Azure Container Apps. This diagram shows the setup we're building toward. Details may change before the move.

<figure class="arch-diagram">
  <a href="/diagrams/qiklog-azure.svg">
  <img
    src="/diagrams/qiklog-azure.svg"
    alt="QikLog on Azure: your .NET app sends logs to qiklog-api, which stores them in PostgreSQL and streams them over SignalR to qiklog-web, which you open at app.qiklog.com. Both apps run in a Container Apps environment. Container Registry holds the images, Key Vault holds secrets, GitHub Actions deploys, and Zitadel handles sign-in."
    width="1600"
    loading="lazy"
  />
  </a>
  <figcaption>Select the diagram to open it full size.</figcaption>
</figure>

## How a log line travels

1. **Send.** Your app sends logs over HTTPS to `POST /v1/logs` with an API key, using the Serilog sink, the CLI, or plain HTTP.
2. **Store.** `qiklog-api` saves each batch to PostgreSQL.
3. **Stream.** The API pushes new lines over SignalR to `qiklog-web`, the dashboard.
4. **Watch.** You open the tail page at `app.qiklog.com` and see lines arrive as they're sent.

## The pieces

| Piece | What it does |
|-------|--------------|
| `qiklog-api` | Ingest API and the SignalR hub. Scales to zero when idle. |
| `qiklog-web` | Blazor dashboard. Stays on so live sessions don't drop. |
| PostgreSQL | Stores logs, sources, keys, and tenants. |
| Container Registry | Holds the app images. |
| Key Vault | Holds secrets. The apps read them through a managed identity. |
| GitHub Actions | Builds and deploys, signing in to Azure with OIDC (no stored passwords). |
| Zitadel | Handles sign-in for the dashboard. |

## Next steps

- [Ingest API](/docs/ingest-api/): send your first log line
- [Live tail](/docs/live-tail/): watch logs in the browser
