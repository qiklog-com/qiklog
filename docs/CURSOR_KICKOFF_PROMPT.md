# First Cursor session prompt

Paste this verbatim into Cursor (or Composer) as the opening message of your first work session.

---

You're Garfield, my implementer Bob. We're starting work on QikLog — read `.cursor/rules/qiklog.mdc` first; that's your operating context.

Current state: the repo scaffold is in place. Hello World should already work via `docker compose up`, but I haven't actually compiled it yet. Your first job is to verify and fix.

**Step 1 — verify the scaffold compiles and runs.**
- Run `dotnet restore QikLog.sln`
- Run `dotnet build QikLog.sln`
- Fix any compile errors. Likely culprits: Fluent UI Blazor package name/version mismatches, System.CommandLine API drift (the package is beta and the API has shifted), missing `using` directives. Don't paper over errors; fix the root cause.
- Run `docker compose up --build` and confirm:
  - Postgres comes up healthy
  - Redis comes up healthy
  - API responds on http://localhost:5080/healthz with `{"status":"ok"}`
  - Web responds on http://localhost:5081 with the home page
  - From the host, `curl -X POST http://localhost:5080/v1/logs -H "Content-Type: application/json" -d '{"source":"demo","level":2,"message":"hello"}'` returns 202
  - Browsing http://localhost:5081/tail/demo shows the message appearing live

**Step 2 — report what you found.**
Give me a short summary: what compiled clean, what needed fixing, what you changed and why. If you had to make any non-obvious tradeoffs, flag them.

**Step 3 — propose the next ticket.**
Look at `docs/PROJECT_PLAN.md`. The first Phase 2 ticket is #10 (Postgres schema + EF Core migrations). Before writing any code, propose a schema sketch in markdown — tables, key columns, relationships, indexing strategy. I want to review that before you start generating migrations.

Conventions reminder:
- File-scoped namespaces, `sealed` by default, nullable on, warnings as errors.
- No `.Result` / `.Wait()`. Async + CancellationToken throughout.
- Structured logging with named placeholders.
- Use Shouldly in tests.
- Smaller commits beat large ones.

Don't add NuGet packages without asking. Don't add features outside the current ticket without asking. If something on the project plan seems wrong, push back.

Go.
