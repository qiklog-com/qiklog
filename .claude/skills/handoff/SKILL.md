---
name: handoff
description: End-of-session handoff. Use when a work session is wrapping up, before context runs out, or when Jamey asks for status.
---
Write a handoff so any replicant (or Jamey) can pick up cold.

1. Update your agent memory (`.claude/agent-memory/<your-name>/MEMORY.md`) with these sections, replacing stale content rather than appending forever:
   - **Current state:** branch, last commit, test count, what is deployed where.
   - **Last session:** what Jamey asked for, what you did, and why.
   - **Decisions:** each with a one-line reason.
   - **Open questions for Jamey.**
   - **Requests for the other replicant** (Bill ↔ Garfield).
2. Commit the memory file on your branch: `docs(<scope>): update <name> handoff`.
3. Reply to Jamey with a five-line summary: done, in progress, blocked, needs from Jamey, next step.
