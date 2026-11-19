---
name: project-m1-self-serve
description: Milestone 1 item 1 (self-serve signup) state as of 2026-10-21 — PR #29 safety fixes, open tenant-model decision, next steps
metadata:
  type: project
---

**State (2026-10-21):** PR #29 (`milestone-1-self-serve`) open, not merged. It makes sign-in *safe*, it does not turn OIDC on. Prod OIDC still off; Jamey flips it after config.

**What #29 fixed and why:** first stranger to sign in would have claimed the bootstrap tenant `11bb1044-…` (hub key + all logs) because the provisioner gave any new org the oldest org-less tenant. Now only `QikLog:Auth:BootstrapOrgId` may claim it. Also: signed-in users never get the hub key (`ApiCredentialPolicy`), `/manage` + `/billing` require sign-in, live API URL in Manage snippet.

**Decisions:**
- Fail closed everywhere tenant identity is uncertain (no org claim → no tenant; no HttpContext → no credential). Showing "session expired" beats showing another tenant's logs.
- Kept tenant = Zitadel org (matches docs/AUTH.md) pending Jamey's call.

**Open questions for Jamey:**
- Zitadel signup: (a) register-org flow, one org per signup [my recommendation] or (b) key tenants on user `sub`. Users self-registering into the default org would share a tenant. Block public signup until answered.
- Free/Pro limits: docs say Free 10k, Pro 500k events/month; pricing page lists none. Confirm before enforcing.

**Next (after #29 merges):** guard that refuses to provision for the Zitadel default org (if (a)); browser-verify `/tail/demo` + www embed history still load anonymously; smoke loop sign-in → key → CLI → tail once Jamey enables OIDC; then Milestone 1 items 2 and 3.

**Requests for Bill:** none yet. Possibly: post-deploy `make smoke` gate (Path B #6).

**How to apply:** start next session by checking #29's review/merge state and whether Jamey answered the Zitadel question.
