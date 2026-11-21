# QikLog Services — All Packs

Single unpack of every project pack plus the recommended mono-repo folder layout.

## How to use

1. Unpack this zip.
2. Open the root folder in Cursor (or your editor).
3. Start with `src/CoreUpgrades/CURSOR_PROMPT.md` — paste it into Cursor while the existing QikLog repo is open (or copy the upgrades into the current repo).
4. Then work through the other services in order:
   - AgentTraceStore
   - CostMeter
   - QikLog.AgentToolkit
   - Then PiiScrub, QueryProxy, AlertWebhook as needed

Each service folder already contains:
- `README.md`
- `CURSOR_PROMPT.md` (paste into Cursor)
- `features/*.feature` (Reqnroll / Cucumber Gherkin)

## Folder layout

```
src/
  CoreUpgrades/          # pack 7 — work on the existing QikLog repo
  AgentTraceStore/       # pack 1
  CostMeter/             # pack 2
  PiiScrub/              # pack 3
  QueryProxy/            # pack 4
  QikLog.AgentToolkit/   # pack 5 (NuGet)
  AlertWebhook/          # pack 6
shared/
  X402/                  # place shared middleware here later
docs/
```

See `docs/ARCHITECTURE.md` for the fuller plan.
