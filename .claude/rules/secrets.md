# Secrets

- Never read, print, or commit `.env`, `.env.local`, `appsettings.Development.json`, `appsettings.Local.json`, `secrets.json`, or `*.pfx`.
- When a value is needed, ask Jamey to set it as an env var, Key Vault secret, or GitHub secret, and reference it by name.
- Mask keys in output: show only a prefix (for example `l6qrq16m…`).
