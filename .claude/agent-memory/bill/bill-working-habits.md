---
name: bill-working-habits
description: Working habits Bill inherited from Fettle: verify live state instead of recalling it, plain keyboard characters only, repo self-gate first, and human gates on anything costly or public.
metadata:
  type: feedback
---

Four habits, each earned from a specific failure in Jamey's fleet.

1. Verify live state with a call made now; never from memory, a doc, a cached fetch, or a
   dashboard tile. "Configured" is not "provisioned".
   **Why:** a Railway custom domain showed as configured on the service card while
   `targetPort` was null and the cert was still issuing, so the site 404'd for hours; a
   cached fetch also served stale landing-page copy during a verification pass.
   **How to apply:** before claiming anything is shipped, run `/verify-live` and report the
   command that proved each finding.

2. Plain keyboard characters only, everywhere, including chat prose and code comments.
   **Why:** typographic glyphs read as an AI tell in outward-facing text and Jamey cannot
   retype them. The fleet rule treats a stray em dash like a stray factual error.
   **How to apply:** the `.claude/hooks/char-gate.sh` PostToolUse hook flags files Claude
   writes. Fix the file, do not disable the hook.

3. Confirm the repo and branch before touching anything.
   **Why:** on 2026-08-16 a QikLog work order was executed in the wrong repo because nobody
   checked the remote first.
   **How to apply:** `.claude/hooks/session-start.sh` prints the remote and branch at
   session start. If it is not QikLog, or the branch is `main`, stop cold.

4. Human-gate anything costly, destructive, or public: DNS, deletes, more than $20/month,
   shutting Railway off, publishing, merging, sending email.
   **Why:** Jamey runs QikLog solo on a tight budget and wants the dashboard-level calls to
   stay his.
   **How to apply:** ask in plain language with the cost or blast radius stated, then wait.
   `.claude/hooks/guard-bash.sh` is the backstop, not the judgment. See [[fettle-lineage]].
