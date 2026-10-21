# QikLog replicants (Claude Code)

Works the same in the Claude Code CLI, the Claude Desktop Code tab, and Happy.

| Replicant | Lane | Start a session as it |
|---|---|---|
| garfield | app code: `src/`, `tests/`, `www/` | `claude --agent garfield` |
| bill | infra: `infra/`, `scripts/`, workflows, Dockerfiles | `claude --agent bill` |
| reviewer | read-only PR review | `@reviewer` inside any session |

## Run the crew (recommended: agent view)

Agent view (`claude agents`, Claude Code v2.1.139+, research preview) shows every replicant on one screen: working, waiting on you, or done. Sessions run in the background, keep going when you close the terminal, and get their own worktree under `.claude/worktrees/` before editing.

```bash
cd ~/dev/qiklog
claude --agent bill --bg --name bill "Read your memory and continue your mission."
claude --agent garfield --bg --name garfield "Read your memory and continue Milestone 1."
claude agents --cwd ~/dev/qiklog     # the dashboard
```

- Enter or → attaches to a replicant; ← on an empty prompt detaches (it keeps running).
- Inside agent view, start one with `@bill <task>` or `@garfield <task>`.
- Commit before deleting a session in agent view; its Claude-created worktree is deleted with it.
- Each running replicant uses your Claude plan quota independently.

### Manual worktrees (alternative)

```bash
git worktree add ../qiklog-bill -b azure-move main && cd ../qiklog-bill && claude --agent bill
```

## Pieces

- `CLAUDE.md` project overview and scope; imports the Cursor rules so Cursor and Claude agree.
- `.claude/rules/` path-scoped rules (`dotnet.md`, `infra.md`) and always-on `secrets.md`.
- `.claude/skills/ship` `/ship [tag]`: test, push, PR, reviewer pass. Never merges.
- `.claude/skills/handoff` `/handoff`: end-of-session memory update and five-line status.
- `.claude/hooks/guard-bash.sh` blocks force-push/push to main, secret reads, and repo deletion; asks Jamey before cloud deletes, DNS, billable creates, publishes, merges, and hosted-DB access.
- `.claude/agent-memory/<name>/MEMORY.md` each replicant's persistent notes (committed).
- `CLAUDE.local.md`, `.claude/settings.local.json` personal overrides (gitignored).

The hook uses `jq` if present, otherwise `python3`.
