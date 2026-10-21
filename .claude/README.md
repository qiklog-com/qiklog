# QikLog replicants (Claude Code)

Works the same in the Claude Code CLI, the Claude Desktop Code tab, and Happy.

| Replicant | Lane | Start a session as it |
|---|---|---|
| garfield | app code: `src/`, `tests/`, `www/` | `claude --agent garfield` |
| bill | infra: `infra/`, `scripts/`, workflows, Dockerfiles | `claude --agent bill` |
| reviewer | read-only PR review | `@reviewer` inside any session |

## Parallel work without collisions

```bash
git worktree add ../qiklog-garfield -b milestone-1-self-serve main
git worktree add ../qiklog-bill -b azure-move main
cd ../qiklog-garfield && claude --agent garfield
cd ../qiklog-bill && claude --agent bill
```

In the Desktop Code tab, open the folder and tick the worktree checkbox instead.

## Pieces

- `CLAUDE.md` project overview and scope; imports the Cursor rules so Cursor and Claude agree.
- `.claude/rules/` path-scoped rules (`dotnet.md`, `infra.md`) and always-on `secrets.md`.
- `.claude/skills/ship` `/ship [tag]`: test, push, PR, reviewer pass. Never merges.
- `.claude/skills/handoff` `/handoff`: end-of-session memory update and five-line status.
- `.claude/hooks/guard-bash.sh` blocks force-push/push to main, secret reads, and repo deletion; asks Jamey before cloud deletes, DNS, billable creates, publishes, merges, and hosted-DB access.
- `.claude/agent-memory/<name>/MEMORY.md` each replicant's persistent notes (committed).
- `CLAUDE.local.md`, `.claude/settings.local.json` personal overrides (gitignored).

The hook uses `jq` if present, otherwise `python3`.
