# Zitadel local (optional)

Enable the `auth` compose profile when you are ready to wire OIDC (#12).

```bash
docker compose -f docker-compose.yml -f docker-compose.auth.yml --profile auth up -d
```

Console: http://localhost:8080

After first boot, register a **Web** application in Zitadel with:

- Redirect URI: `http://localhost:5081/signin-oidc`
- Post-logout URI: `http://localhost:5081/`
- Auth method: PKCE (public client) or confidential with secret in `QikLog__Auth__ClientSecret`

Set `QikLog__Auth__ClientId` to match the Zitadel application client ID.

Production authority: `https://signin.qiklog.com` (see `docs/AUTH.md`).
