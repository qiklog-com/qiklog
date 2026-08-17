---
title: Getting started
description: What QikLog is, core concepts, and the five-minute mental model.
order: 1
---

QikLog is real-time log tailing for developers: send a line, watch it appear in the browser. Like `tail -f`, but over HTTP and SignalR.

## Core concepts

| Term | Meaning |
|------|---------|
| **Source** | A named stream of logs (e.g. `api-prod`, `worker-01`, `demo`). You choose the name; it groups live viewers and stored rows. |
| **Level** | Severity: `trace`, `debug`, `info`, `warning`, `error`, `critical` (or integers 0–5). |
| **Ingest** | `POST /v1/logs` accepts JSON and broadcasts to anyone tailing that source. |
| **Live tail** | The dashboard page `/tail/{source}` shows new lines as they arrive. |
| **Persistence** | Ingested lines are stored in Postgres so history survives restarts (search UI coming later). |

## What you need

- **API**: receives logs (default local port `5080`)
- **Web app**: Blazor dashboard for live tail (default local port `5081`)
- **Optional CLI**: `qiklog send` and `qiklog tail-file` from your terminal

## Fastest path

1. [qiklog.com](https://www.qiklog.com/): click **Try it now**. The app home shows a copy-paste curl and a live tail for source `demo`.
2. [Quickstart](/docs/quickstart/): run the stack locally
3. [Ingest API](/docs/ingest-api/): wire your app or curl
4. [Live tail](/docs/live-tail/): open the browser viewer
5. [API keys](/docs/api-keys/): required in production; optional locally
6. [CLI](/docs/cli/): ship logs from scripts and files
7. [Serilog](/docs/serilog/): one line in Program.cs
8. [Manage UI](/docs/manage-ui/): list sources and keys in the dashboard

## Pre-alpha honesty

Accounts, billing, and source management UI are still on the roadmap. Today you can ingest, tail live, persist to Postgres, and authenticate ingest with API keys.

The hosted dashboard is a demo environment and says so on every page: sample data only, and authentication is still in active development. Treat anything you send there as public and throwaway.
