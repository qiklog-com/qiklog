# QikLog local quickstart

Get from a clean clone to a working tail view in under ten minutes. Two modes: **demo** (auth off, fastest) and **full dev** (Zitadel OIDC, matches staging).

## Prerequisites

- Docker Desktop (or Docker Engine + Compose v2)
- .NET 9 SDK (only if you run API/Web outside Docker)
- Ports free: **5080** (API), **5081** (Web), **5432** (Postgres), **6379** (Redis), **8080** (Zitadel, full mode only)

## Mode A — Demo (auth disabled)

**Use for:** trying the product locally. **Never use in production.**

### 1. Start the stack

```bash
docker compose up -d --build
```

Compose sets `QikLog__AuthEnforcement__Enabled=false` and `QikLog__Auth__Enabled=false` on the API so ingest, management, history, and SignalR work without tokens.

### 2. Wait for health

```bash
curl -s http://localhost:5080/healthz | jq
# expect: "status":"ok", "postgres":"ok"
```

First boot can take 30–60s while Postgres migrates and containers build.

### 3. Send a log line

```bash
curl -s -X POST http://localhost:5080/v1/logs \
  -H "Content-Type: application/json" \
  -d '{"source":"demo","level":"info","message":"hello from curl"}'
```

Expect HTTP **202 Accepted**.

### 4. Watch live tail

Open **http://localhost:5081/tail/demo** — the line should appear via SignalR.

### 5. Optional API docs

- Scalar UI: http://localhost:5080/scalar/v1  
- OpenAPI JSON: http://localhost:5080/openapi/v1.json  

### Demo-mode config (reference)

| Setting | Value |
|---------|--------|
| `QikLog:Auth:Enabled` | `false` |
| `QikLog:AuthEnforcement:Enabled` | `false` |

### Common gotchas (demo)

| Issue | Fix |
|-------|-----|
| Port 5080/5081 in use | `lsof -i :5080` — stop old `dotnet run` or other stacks |
| `postgres: unreachable` on healthz | Wait for `docker compose ps` to show postgres healthy |
| Tail page empty | Confirm POST returned 202; check API logs: `docker compose logs api -f` |
| Blazor antiforgery warnings | Benign in dev after container restart |

---

## Mode B — Full dev (auth enabled)

**Use for:** developing auth, tenants, API keys, and management UI against real OIDC.

### 1. Start stack + Zitadel

```bash
docker compose -f docker-compose.yml -f docker-compose.auth.yml --profile auth up -d --build
```

Zitadel first boot can take **2–5 minutes**. Watch: `docker compose logs zitadel -f`

### 2. One-time Zitadel setup

See [ZITADEL_LOCAL.md](ZITADEL_LOCAL.md) for app registration. Summary:

1. Open http://localhost:8080  
2. Complete first-login / instance setup  
3. Register OAuth app for `qiklog-web` with redirect `http://localhost:5081/signin-oidc`  
4. Note client id/secret in compose overrides if needed  

### 3. Create an ingest API key (JWT required for management)

With enforcement on, create keys via the dashboard after OIDC login, **or** use the dev endpoint (Development only):

```bash
# Only when API runs with ASPNETCORE_ENVIRONMENT=Development
curl -X POST http://localhost:5080/v1/dev/keys \
  -H "Authorization: Bearer <your-jwt>" \
  -H "Content-Type: application/json" \
  -d '{"name":"local-ingest"}'
```

For **CLI / curl ingest** without the dashboard:

```bash
export QIKLOG_API_KEY=ql_...
curl -X POST http://localhost:5080/v1/logs \
  -H "Content-Type: application/json" \
  -H "X-QikLog-API-Key: $QIKLOG_API_KEY" \
  -d '{"source":"demo","message":"authenticated ingest"}'
```

### 4. Dashboard tail with API key

Set the Web container (or `appsettings.Development.json`) so Blazor can reach the hub:

```json
{
  "QikLog": {
    "ApiBaseUrl": "http://localhost:5080",
    "HubApiKey": "ql_your_key_here"
  }
}
```

Docker override example:

```yaml
# docker-compose.override.yml (local, gitignored)
services:
  web:
    environment:
      QikLog__HubApiKey: "ql_..."
```

Restart web: `docker compose up -d web`

### 5. Verify enforcement

| Check | Command | Expected |
|-------|---------|----------|
| Ingest without key | `curl -X POST .../v1/logs` (no header) | **401** |
| Ingest with bad key | `X-QikLog-API-Key: ql_invalid...` | **403** |
| Management without JWT | `curl http://localhost:5080/v1/keys` | **401** |
| Health (public) | `curl http://localhost:5080/health` | **200** |

### Full-mode config (reference)

| Setting | Value |
|---------|--------|
| `QikLog:Auth:Enabled` | `true` |
| `QikLog:AuthEnforcement:Enabled` | `true` |
| `QikLog:Management:Enabled` | `true` |
| `QikLog:Ingest:RequireApiKey` | `true` (redundant when enforcement on) |

### Getting a JWT for manual API testing

1. Log in via http://localhost:5081 (OIDC challenge).  
2. Use browser devtools / OIDC token from your IdP test client, **or**  
3. Use Zitadel’s token endpoint with your registered API client (see Zitadel docs).  

Pass as `Authorization: Bearer <jwt>` to management routes and optionally `?access_token=<jwt>` for SignalR WebSocket clients.

### Where to find logs

```bash
docker compose logs api -f
docker compose logs web -f
docker compose logs postgres -f
```

Structured logs use `ILogger` categories (`QikLog.Ingest`, `LogHub`, etc.).

---

## Makefile shortcuts

```bash
make up-d      # docker compose up -d
make verify    # build, test, smoke health + demo POST (uses demo-style local test host)
make demo      # send a test log via CLI
make test      # unit + API tests (no E2E)
```

---

## Switching modes

| From | To | Action |
|------|-----|--------|
| Demo | Full | Stop stack; start with `docker-compose.auth.yml` + `--profile auth` |
| Full | Demo | Stop stack; `docker compose up` without auth overlay |

Do not run production with `AuthEnforcement:Enabled=false`.
