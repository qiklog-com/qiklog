---
name: verify-live
description: Verify what is actually deployed and running, from live calls rather than memory, docs, or a dashboard tile. Use before claiming something is shipped, after a deploy, or when a doc and reality might disagree.
argument-hint: "[environment or host, e.g. azure | railway | app.qiklog.com]"
allowed-tools: Bash(curl*) Bash(dig*) Bash(openssl*) Bash(az * list*) Bash(az * show*) Bash(gh run list*) Bash(gh run view*) Bash(make smoke) Read Grep Glob
---
Verify live state for: $ARGUMENTS

Rules of evidence. Memory, docs, a prior session's notes, and a green dashboard tile are
not evidence. A command run now is. "Configured" is not "provisioned". Report each finding
with the command that proved it.

1. Identify the targets: web origin, API origin, and the host or resource group in scope.
   Default to the URLs in the `Makefile` and `docs/TODO.md` if none were given.
2. HTTP reality: `curl -sS -o /dev/null -w '%{http_code} %{time_total}s %{url_effective}\n' -L <url>`
   for the web root, the API health endpoint, and any custom domain. Follow redirects and
   note where they land.
3. TLS reality: for each custom domain, check the certificate is issued and matches the
   host (`openssl s_client -connect <host>:443 -servername <host> </dev/null 2>/dev/null | openssl x509 -noout -subject -dates`).
   A hanging request usually means a cert is still issuing, not that the app is down.
4. DNS reality: `dig +short <host>` and `dig +short CNAME <host>`. Compare against what the
   platform expects. Report only; never change DNS.
5. Platform reality, read-only: for Azure, `az containerapp list -o table`,
   `az containerapp show -n <app> -g <rg> --query "properties.{fqdn:configuration.ingress.fqdn,replicas:template.scale}"`,
   and `az containerapp revision list` to see which revision is actually serving.
   For GitHub, `gh run list --limit 5` to see whether the last deploy really succeeded.
6. Application reality: run `make smoke` against the environment when smoke env vars are
   available. If they are not, say which variable is missing instead of skipping silently.
7. Contradiction pass: compare what you just measured against what the repo claims, in
   `README.md`, `docs/AZURE_DEPLOY.md`, `docs/SHIP_CHECKLIST.md`, `docs/TODO.md`, and any
   landing copy. List every place the claim and the measurement disagree.

Report in this shape:

- Verified working: one line each, with the command.
- Verified broken: one line each, with the command, the symptom, and the most likely cause.
- Unverified: what you could not check and exactly what you need in order to check it.
- Claims to fix: doc or copy that disagrees with reality.

Do not change anything in this skill run. Verification is read-only; fixes are a separate,
explicit step.
