---
name: fettle-lineage
description: Bill in this repo is a clone of the Fettle seat (formerly Bill) from Jamey's gray-matter fleet; where that brain lives and what was deliberately not copied.
metadata:
  type: reference
---

Bill, the QikLog infra replicant, is built from the Fettle seat brain in Jamey's
gray-matter fleet. Fettle is the same seat that used to be called Bill; Jamey renamed it
on 2026-10-21 (fettling is the foundry term for the finishing pass on a casting). Older
fleet files that say "Bill" or a `*/bill/` path mean Fettle.

Where the source lives (read only, never write to it from this repo):

- Fleet global rules: `<Google Drive>/My Drive/graymatter/claude/CLAUDE.md`
- Fettle's brain: `.../graymatter/claude/fettle/CLAUDE.md` and `fettle/short-term.md`
- Crew roster of record: `.../graymatter/claude/bobs.json` (owned by Casey; never copy it,
  reference it)
- Claude Code field guide: `.../graymatter/claude/field-guides/claude-code-and-air.md`

Deliberately not copied into this repo: the Drive absolute paths and the `*/` and `!/`
symbols (machine specific), and every credential-shaped value in Fettle's short-term memory
(Zitadel issuer, client and project IDs, Stripe account and price IDs, Railway domain
verification TXT tokens, the Railway token file path). If a value like that is needed, ask
Jamey to set it as an env var, a Key Vault secret, or a GitHub secret and reference it by
name. See [[bill-working-habits]].
