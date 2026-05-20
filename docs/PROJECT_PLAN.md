# QikLog Project Plan

Phased roadmap. Each item is a discrete issue. Phases map to GitHub Project columns or Trello lists.

For bulk import to GitHub Projects, use `gh issue create` in a loop against this file, or paste into Project's "Add item" with the `Phase` field set.

---

## Phase 1 — Hello World (done with this scaffold)

These are complete in the initial commit and verify the end-to-end pipe.

- [x] Solution + project structure (Core, Api, Web, Cli, Tests)
- [x] `POST /v1/logs` ingest endpoint
- [x] SignalR `LogHub` with source-based grouping
- [x] Blazor `Tail.razor` page with auto-scroll, clear, status indicator
- [x] CLI `qiklog send` and `qiklog tail-file` commands
- [x] Docker Compose with Postgres + Redis + API + Web
- [x] GitHub Actions CI (build + test + Docker build)
- [x] README quickstart that proves the demo works

## Phase 2 — Make it real (Weeks 2-4)

- [ ] **#10** PostgreSQL schema + EF Core migrations (Tenants, Users, Sources, ApiKeys, LogEntries)
- [ ] **#11** API key auth on `POST /v1/logs` (Argon2id hashed at rest, per-key rate limit)
- [ ] **#12** User registration + login via ASP.NET Core Identity (email/password, verification, reset)
- [ ] **#13** Source management UI (list, create, revoke; Fluent DataGrid)
- [ ] **#14** Persist log entries to Postgres (partitioned by day)
- [ ] **#15** Postgres full-text search across stored logs with filters
- [ ] **#16** Redis-backed hot buffer for fast live tail under load
- [ ] **#17** OpenAPI doc generation + Swagger UI polished for public consumption

## Phase 3 — Alerts and integrations (Week 5)

- [ ] **#20** Regex match → webhook alert engine (background hosted service)
- [ ] **#21** Slack webhook format adapter
- [ ] **#22** Discord webhook format adapter
- [ ] **#23** SendGrid email alerts
- [ ] **#24** Alert management UI (create, edit, disable, view firing history)

## Phase 4 — Billing and launch (Week 6)

- [ ] **#30** Stripe Checkout integration (Free / Pro $9/mo / Tinkerer $20 prepaid)
- [ ] **#31** Stripe Customer Portal embed
- [ ] **#32** Usage metering (ingest GB per tenant per month)
- [ ] **#33** Limits enforcement (block at cap, email at 80%)
- [ ] **#34** Marketing landing page on qiklog.com (static, separate repo)
- [ ] **#35** Docs site (DocFX or Blazor section)
- [ ] **#36** Show HN / r/dotnet / Indie Hackers launch posts

## Phase 5 — Agents (post-launch, opportunistic)

- [ ] **#40** `QikLog.Logging` NuGet — `ILogger` provider
- [ ] **#41** `@qiklog/node` npm package
- [ ] **#42** Homebrew formula for `qiklog` CLI
- [ ] **#43** Scoop manifest for `qiklog` CLI (Windows)
- [ ] **#44** Self-hostable Docker image bundle + docs

## Cross-cutting

- [ ] **#50** Azure deployment pipeline (push to ACR + update Container Apps)
- [ ] **#51** Azure Postgres Flexible Server provisioning script (Bicep)
- [ ] **#52** Trademark filing once at $500+ MRR (USPTO Class 9 + 42)
- [ ] **#53** ToS + Privacy Policy (use a generator + lawyer review at $1K MRR)
