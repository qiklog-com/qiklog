# QikLog Marketing Notes

Working notes. Not a roadmap. Nothing here is committed work until it
graduates to the backlog with Jamey's approval.

## Positioning

The product pitch and the onboarding are the same thing: see your logs
in seconds. A signup form before first value would refute the product's
own claim. The landing page demo is the argument.

Internal shorthand: Velma vs Jessica. Velma-mode ships config files and
setup docs. We ship a cursor already blinking. (G-rated metaphor doing
R-rated work on the conversion rate. Christian Dad certified.)

## The front door (landing page live demo)

Flow, runs backwards from normal SaaS:

1. Dev lands on qiklog.com. One button: "Start streaming".
2. Click. No form, no email. API mints an ephemeral workspace and key.
   Page splits: left, a copy-paste one-liner with the key baked in;
   right, a live tail pane already connected, cursor blinking,
   "waiting for your first log line".
3. Dev pipes stdout. Their real logs scroll on the marketing page
   within seconds. That moment is the entire pitch.
4. Quiet banner: workspace evaporates in 24h, sign in to keep it.
   That is the OIDC hook. Claim-by-signin attaches the ephemeral
   workspace to the tenant the provisioner creates on first login.

Needs built: anonymous workspace endpoint (TTL, tight ingest quota,
aggressive rate limits, origin checks), tail pane on the Astro site,
CORS extended to qiklog.com, claim flow.

Guardrail: anonymous ingest attracts abuse. The sexy front door needs
an unglamorous bouncer. Solvable, not skippable.

## Time-to-value goals (the "Qik" audit)

Priority loop: arrive, hooked, kept, spread.

1. Landing page live demo (above). Put a stopwatch on it and publish
   the median. "Median: 19 seconds" is a benchmark competitors have
   to answer.
2. Serilog/NLog/ILogger sink on NuGet. dotnet add package plus one
   line in Program.cs. For the .NET audience this may matter more
   than the demo.
3. Claim-by-signin. No import step, the workspace follows them.
4. Shareable tail links. "Look at this" is one TTL'd URL. Team
   virality.

Seasoning (later): curl-only ingest path, qiklog init project
detection, Docker log driver / compose snippet, GitHub Action for
live CI tails, keyboard-first log screen (slash to filter), one
command full export as the anti-lock-in closer.

No password exists anywhere. OIDC only. "We don't have a password to
leak" is both sexy and true.

## The Qik thesis (holstered until QikLog is in the black)

Observation: developer tools have normalized terrible time-to-value.
Every category is vulnerable to whoever fixes it. Stripe won payments
with seven lines of code. Vercel won deploys with git push. Incumbents
accumulate setup friction and cannot scrape it off without breaking
enterprise customers.

QikLog is the cheapest possible test of the thesis. The landing page
stopwatch is the experiment. If time-to-first-log converts, the
playbook generalizes; if not, the empire idea dies for free.

Naming formula that works: Qik + the concrete noun for the pain.
QikLog works because Log is instantly scoped. Abstract second
syllables fail the podcast test (hear it, type it, find it).

Candidates, rated:
- QikAuth: strongest. The OIDC pain that inspired it is a firsthand
  founding memo.
- QikSignup, QikUI, QikTool, QikStrap, QikBoots, QikIt: unvetted,
  parked.
- QikTek: pronounceable but reads 1998. Crowded trademark space.
- QikTex: LaTeX collision, materials-company trademark neighbors.
- QikTlz, QikTlx: fail the podcast test. Double abbreviation.
- QikApp / QikApps: "app" is a category, not a pain. Collides with
  Huawei Quick App and QuickApps marks.
- QikPay: rejected. Regulatory buzzsaw plus knife fight with Stripe.
- QikSilver: rejected. Quiksilver trademark, hard no.

Umbrella naming, if the family ever earns one: bare qik is gone in
every TLD that matters (.com is Microsoft's via the $150M Skype
acquisition of the Qik video startup; .dev/.io/.sh/.app/.tools all
registered). Single-letter TLDs are prohibited by ICANN and two-letter
TLDs are reserved for country codes, so .q and .qk are impossible at
any price. A custom .qik gTLD runs roughly $250K in application fees
alone (jokes that are accidentally true: it IS cheaper than buying
qik.com from Microsoft). Realistic pattern when needed: getqik,
qikhq, or each product owns its noun as qiklog.com already does.

Cheap and time-sensitive: check domain availability for QikAuth and
any keeper before caring about the name.

Everything else waits for QikLog's numbers. The empire's first brick
is a developer signing in.

## Hero motion: tape reel, not generic animation

Landing hero needs something proving the "works in seconds" claim,
not decoration. Chosen direction: a small VCR/cassette-tape motif --
reels slowly turning as log lines stream past like a tape counter,
a quiet rust "REC" dot. Ties the visual to the actual feature (a
"tail" is literally a tape metaphor) rather than a generic typing
cursor. Keep it small and quiet in the hero, respect
prefers-reduced-motion (static frame when reduced).

## Standing rules that apply here

- Business case first. The empire is worth zero until QikLog has
  users.
- No em dashes in outward-facing copy.
- Brand identity per docs/BRAND.md: paper, ink, rust. The mark is the
  prompt frame with the Q tail.
