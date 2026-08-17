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

## Post-demo choice branch (8/16 late night)

After the "try it now" demo, offer a secondary choice menu rather
than one CTA, catching visitors who scrolled past the primary
button without converting. Four paths, brainstormed from a real
usage moment:

1. Sign me up (existing CTA, just relabeled as one branch)
2. Terminal curl install -- "want to see it in your terminal
   instead?" BLOCKED on the CLI watch/receive command (see TODO,
   not built yet -- send only right now). This path can't honestly
   exist until that ships.
3. `qiklog init` / an installable AI skill that wires logging into
   an app automatically -- the deep-integration bet (echoes the
   Serilog/NuGet sink idea). Real engineering, correctly held, not
   a tonight thing.
4. Clone-and-run GitHub repo -- cheapest of the four, no
   dependencies, doubles as the quickstart docs example. Could ship
   soonest.

Framing: keep ONE dominant "Try it now" as primary (already proven,
don't dilute it with paradox-of-choice). These four go in a quieter
secondary section further down the page -- "prefer a different way
in?" -- not competing with the main CTA.

## Scroll sequence mechanic (evolved from Apple's MacBook Pro page)

Watched apple.com/macbook-pro for reference. Initial read was scroll
scrubbing an actual video by frame count (their frame counter, e.g.
"844F -> 1516F", confirms this) -- too heavy an ask for a solo dev
landing page.

CORRECTED, SIMPLER mechanic (Jamey's reframe, this is the one to
build): think animated slides with a "next" control, not continuous
video scrub.
- CSS scroll-snap sections, one slide per viewport.
- A down-chevron affordance (mobile "more below" style) advances to
  the next section via scrollIntoView() -- a discrete click, not
  tracked scroll position.
- Each slide's entrance animation fires ONCE on first arrival, then
  locks static. Scrolling back up/down later just re-reveals settled
  content in order, no replay.
- Reduced-motion: show final static state immediately.
This is a known, buildable pattern (Stripe/Notion/Linear have
shipped versions), NOT the harder video-scrub technique. Real
psychological hook: each "click to advance" is a small commitment
that makes abandoning later feel like walking away from something
half-finished (commitment escalation) -- this alone is doing the
gamification work, see below.

Two content directions for the slides, both good, pick one:
- A: headline zooms ("Easy. Secure. Up in minutes.") -> curl
  terminal slides in from left -> tail/dashboard terminal slides in
  from below-right showing it land. Reskins the existing demo with
  motion.
- B (stronger): "Easily add to your app" -> NuGet install shown ->
  one line added to Program.cs -> save/run -> log appears in
  terminal. Answers "how much of my afternoon does this cost"
  directly instead of just showing the payoff.
Ends with a "Get started" button popping in, same slide-in style as
the rest.

Status: NOT YET SENT to Cursor as its own work order. The existing
"5-scene fade-in" work order (scene 3 = real embedded live tail) is
already in flight -- PR #14 landed the iframe-embed permission piece.
Decide before next session: fold this snap+chevron+one-shot upgrade
into that in-flight work, or treat as a deliberate v2 once the
simpler version ships. Don't send conflicting specs to Cursor.

## Gamifying setup: badges rejected, here's why (and the better idea)

Jamey's instinct: developers respond to gamification, floated
"QikLog badges" (Installed CLI, Viewed 500 log messages, Made a
Suggestion). Correctly self-identified as probably too far, asked
for a real read.

Verdict: badges are solving the right problem with the wrong tool.
Gamification works on REPEATED behavior (Duolingo streaks, GitHub
contribution graphs) -- setup is a one-time event, so a badge for it
is a participation sticker, not a hook. Worse: a badge/leaderboard
system implies social proof (other people have earned this too) that
doesn't exist yet with zero users -- shown to an early visitor it
reads as an empty trophy case, not exciting.

The actual insight: the slide sequence above (progression, small
wins, forward click-to-advance momentum) IS the gamification.
Manufacturing a second badge system to chase the same psychological
need is redundant. Ship the slides, skip the badges.

Where a real game-like mechanic DOES fit, with actual precedent in
this category: shareable tail links (already on the fast-follow
list). Not "look what I unlocked" but "look what I'm debugging,
click to see it live" -- Loom/Figma/Notion's actual growth engine.
Compounds through other people seeing the product, not one person's
profile decoration. This is the "gamify it" itch worth scratching,
not badges.

## Wedge strategy (Planning Center vs ACS precedent, 8/17)

QikLog's wedge is zero-friction log tailing -- not "better
observability," just the one thing the big suites (Splunk, Datadog,
CloudWatch) treat as an afterthought, done painlessly: no agent, no
config, no dashboard build, logs in the browser in under 300ms.
Same playbook Planning Center ran against ACS: pick the narrow thing
the incumbent overbuilds for, win it cleanly with the users the
incumbent never fit (solo devs / small teams here; small churches
there), then add modules AFTER the wedge has earned trust -- never
before. The discipline is the strategy: Planning Center kept adding
modules until they had everything ACS had and more, but only once
the wedge was proven. Business-case-first rule exists for exactly
this. The landing page stopwatch is the experiment that tells us
whether the wedge is sharp enough to build anything on top of. The
signal to watch is not "what module next," it's "does the wedge
convert."

Candidate next module once the wedge proves out: JavaScript hook
with frontend-to-backend trace correlation (see TODO for the schema
constraint to honor NOW so this stays possible later).

## Scroll sequence: chevron timing + scene 2 content debate (8/17)

Chevron refinement: the first down-chevron should NOT be visible on
load -- it appears after a short delay or a page event, so the new
motion catches the eye. Label copy: keep the Field Clinical voice.
"ez to add" style slang breaks the tone every winning line has had
(B headline, JWT expired 401). Use something precise and quiet like
"watch it work" / "see it live" instead.

OPEN DEBATE, not settled: Jamey floated scene 2 as a fake/staged
tape-terminal recording of a NuGet install (env var or PowerShell
for the token, then `nuget install QikLog`), followed by real log
display. Bill's pushback: the NuGet sink does not exist yet, and the
one rule held all weekend is scene 3 must be REAL (PR #14/#15 built
the genuine iframe embed for exactly that). A staged install
directly before the genuinely-live scene is the worst placement for
a fake -- once "was that real?" enters a visitor's head it bleeds
onto the real parts. Bill's counter: keep scene 2 anchored to curl
(real, ships today), give it the tape-recorder terminal TREATMENT
(presentation upgrade, fair game), and film the NuGet scene only
once the sink is actually built. Also: the token-delivery question
(env var vs PowerShell vs other) is an unresolved product decision,
more evidence the engineering has to precede the film. Jamey has
not ruled; decide before scene 2 gets built.

## Standing rules that apply here

- Business case first. The empire is worth zero until QikLog has
  users.
- No em dashes in outward-facing copy.
- Brand identity per docs/BRAND.md: paper, ink, rust. The mark is the
  prompt frame with the Q tail.
