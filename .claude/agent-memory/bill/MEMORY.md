# Bill memory

## Memory files
- [Fettle lineage](fettle-lineage.md) - where the Fettle brain lives in Drive, and what was deliberately not copied.
- [Working habits](bill-working-habits.md) - verify live, char gate, repo self-gate, human gates, and why each exists.

## Current state
- Branch `bill-fettle-clone` (rebuilt the agent definition). Azure work branch is `azure-move`, not started, waiting on this PR merging.
- Production today: Railway (qiklog.up.railway.app, qiklog-api.up.railway.app; app/api.qiklog.com via Cloudflare DNS, records in docs/TODO.md). Marketing www on Vercel.
- Existing Azure tooling: scripts/azure-setup.sh, azure-deploy.sh, docs/AZURE_DEPLOY.md (never fully used).

## Last session (2026-10-21)
- Jamey asked for Bill to be rebuilt as a clone of Fettle, his ship-ops seat in the gray-matter fleet. Read the fleet global CLAUDE.md, fettle/CLAUDE.md, fettle/short-term.md, epsilon-eridani/CLAUDE.md, the Fettle roster entry, and the Claude Code field guide.
- Rewrote `.claude/agents/bill.md` in Fettle's shape: identity, current posting as a variable, who you serve, repo self-gate as first action, the Azure mission unchanged, how-you-work habits, guardrails, ship and handoff. Lane, mission, and guardrails carried over word for word in substance.
- Brought over, adapted for QikLog: `/verify-live` skill, `session-start.sh` repo self-gate hook, `char-gate.sh` untypable-character hook. Wired both hooks into `.claude/settings.json`.
- Did not start Azure work. Jamey said not until this merges.

## Decisions
- Hooks over prose for the two fleet gates. Reason: Fettle's doctrine says the character gate and repo self-gate are mechanical, because prompting alone already failed once each.
- `/verify-live` is read-only and never fixes what it finds. Reason: keeps verification trustworthy and keeps fixes an explicit, approved step.
- Kept MEMORY.md in the repo's handoff shape and added separate memory files for durable facts. Reason: the `/handoff` skill and Garfield's memory both use this shape, and splitting durable facts out keeps the handoff short.

## Open questions for Jamey
- Is the Azure subscription new (12-month free Postgres B1ms) or existing pay-as-you-go?
- Preferred region (default East US 2).
- Should the char gate also fail `make test` style checks in CI, or stay a Claude-side hook only?

## Requests for Garfield
- None yet.
