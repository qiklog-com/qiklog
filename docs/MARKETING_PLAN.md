# QikLog marketing and sales plan

Written 2026-11-21 by Claude for Jamey, from five model answers to the same question
(docs/discussions/) plus qiklog.com, app.qiklog.com and this repo as they stood that day.
The editable copy lives in the QikLog Claude project; this file is the snapshot the
log link work was built from.

## The short version

Sell a log link, not a logging platform. Five models read the same site and all five
landed there, which is the strongest signal in the folder.

1. **Product:** one URL that you send to and open.
   `curl https://qiklog.com/l/k7mq2x9d4fab -d "it broke"`, then open the same URL and watch.
2. **Line:** console.log for your server.
3. **Ladder:** Link (no account, 24 hours) to Free (keep it, private) to Pro at $9
   (history and volume).
4. **Sales:** your network for the first dollar, the Scotty bundle for recurring contracts,
   freelancers with many client apps for self-serve Pro.
5. **Marketing:** one 12-second recording, posted to Show HN and r/dotnet; packages on
   NuGet and npm; a snippet that gives coding agents a log URL.
6. **Order:** first dollar by Nov 29, the link built by Dec 6, a 30-developer quiet test
   to Dec 20, public launch Jan 5.

One thing changes from the PO brief: the public launch lands after Dec 31, so the
$240 MRR date should move to Jan 31 unless network and Scotty sales carry it.

## Scorecard

Cursor wrote the best answer. It was the only one that clicked Send, and it found the
idea the rest circled: the URL is the endpoint.

| Model | Grade | Best contribution | Missed or got wrong |
| --- | --- | --- | --- |
| Cursor | A | The link is both where you send and where you watch, with no key. "Keep this link" is the paywall. Spotted that the homepage tail was one shared `demo` source. | Its example slug `k7mq` is 4 characters, which is guessable. No sales motion. No sequencing. |
| ChatGPT | B+ | Named the real risk: people say "neat" and go back to SSH. Gave a pass/fail validation test. A kept link must not require a code change. Flagged that a public repo marked "All rights reserved" is not open source. | 6,300 words. Read August notes as current. Its 90-day plan adds Go, Python, teams and roles right after warning against feature creep. |
| Gemini | B+ | Concrete moments to sell: webhooks, cron jobs, serverless and edge functions. People pay for permanence and privacy, not line counts. | Its Free tier is account-less only, which contradicts the decided 10,000-line Free plan. |
| Claude | B | Caught the Docker quickstart on a hosted product, the stale pricing page, and that the first-dollar steps come first. Did the $240 = 27 accounts math. | Kept a token in the snippet, one step short of the real idea. Said nothing about retention or sales. |
| Grok | C+ | The shared link as the viral loop. "Datadog for when it is serious, QikLog for right now." | Mostly restates the site. Product Hunt waitlist is the wrong first move. |

### Where they disagree, and the ruling

| Question | Ruling |
| --- | --- |
| .NET in the headline? | Headline is universal. Distribution is .NET first. |
| Are anonymous links public? | Unguessable link, 24-hour expiry, visible "anyone with this link can read" warning. Private the moment it is kept. |
| What triggers payment? | Keep the decided caps as the cost guard. Sell permanence, privacy and history. |
| Price on the first screen? | Nothing before the first line appears. One quiet price line after. |
| Product Hunt? | Later, and only after 20 people use it twice. |
| Plan for acquisition? | Acquisition is an outcome of revenue and retention. This plan does not change to chase it. |

## The product decision everything depends on

Build the log link before any promotion. Without it the marketing describes a product
that does not exist: the first real snippet needed an API key, and a key needed an account.

- **Send:** POST anything to the URL. The body is the line. No key, no JSON, no headers.
- **Watch:** open the same URL in a browser. That is the tail.
- **Read as text:** `curl <url>/raw` returns the last lines as plain text, for terminals
  and coding agents.
- **Share:** paste the URL in Slack. A teammate sees the same tail.

Rules that make it safe: a long random slug (the link is the secret), 24 hours, 500 lines,
2 KB per line, a per-address limit on new links, a visible "do not log secrets" warning,
and lines that belong to no tenant and never count against a customer's cap.

**The move that makes it a business: "Keep this link."** Signing up makes the link yours:
reading goes private, expiry is removed, and the send URL keeps working with no code change.
Once that URL is in an app's environment variables it stays there, which is the retention
answer to the "neat, then back to SSH" worry.

**Status in this repo:** the link itself is built (docs/specs/log-link.md). The keep step
is not. It touches tenant isolation and gets its own spec and review. Until it ships, the
copy says "create a free account for logs that stay private and do not expire" and does
not promise to carry the link over.

The idea is not new. ntfy.sh uses the same shape for notifications, and a tool called
Logsy launched on Product Hunt pitching one curl command and no signup. The shape is
proven; the win is execution, the keep step, and .NET distribution.

## Positioning

QikLog is console.log for your server: a utility in the family of bit.ly, Pastebin and
webhook.site, and deliberately outside the observability category.

- **Headline:** console.log for your server.
- **Sub:** Get a link. Send it a line. Watch it land.
- **Enemy:** SSH and `tail -f`, the host's log tab, and a monitoring setup you do not need
  to answer "did it break?"
- **Recurring theme:** "It worked on my machine."
- **Honest comparison:** monitoring suites for when it is serious, QikLog for right now.

Who, in order:

1. One developer, stuck, tonight. Solo builders and freelancers on Railway, Fly, Render,
   Azure App Service.
2. .NET developers. This is where to distribute first, not what the headline says.
3. People with no box to SSH into: webhook handlers, cron jobs, serverless and edge functions.
4. Coding agents and the people who run them.

Words: say link, line, tail, keep. Source, ingest, tenant, SignalR, Bearer and median
belong in the docs. The 278 ms figure is a footnote.

## The page

One screen that does the thing.

1. Headline and sub.
2. One button: **Get a log link.** It returns the URL with the tail already open beside it.
3. One line to paste, with tabs for curl, JavaScript and C#. All three work with no key.
4. When the first line lands, accounts are mentioned. Not before.
5. Below the fold: what it is not, the price in one line, and "there is a Serilog sink."

Truth pass, required before any traffic:

- [x] Quickstart no longer tells hosted users to run Docker (self-hosting has its own page).
- [x] Pricing page has Free and Pro and no "checkout is not live" line (done in #42).
- [x] README leads with the product; status badge is beta.
- [ ] App header still says "pre-alpha".
- [ ] GitHub About link points to qiklog-www.vercel.app.
- [ ] Decide the one-sentence answer to "is it open source?"

## The ladder

| Rung | Price | What you get | Why you step up |
| --- | --- | --- | --- |
| Link | $0, no account | An unlisted link for 24 hours, 500 lines | You put it in real code and do not want it to die |
| Free | $0, account | Private, permanent, named. API keys. 10,000 lines a month | You want to scroll back to what happened before you opened the tab |
| Pro | $9 a month | History, 500,000 lines a month | |

Still open: retention days. Suggested 24 hours Free and 30 days Pro, because history is
the reason to pay. No Team tier until a customer asks to pay for a second seat.

## Sales plan

None of the five answers had one. Self-serve at $9 is slow on its own, so three motions
run side by side.

1. **Your network, one person at a time.** The first dollar. Ask people who deploy
   something this month. Ask for 10 minutes on a call while they wire it into a real app,
   then ask them to pay.
2. **The Scotty bundle.** After the first dollar. Every modernization engagement ships
   with QikLog wired in and a 12-month Pro line item on the quote ($108 a year at list).
3. **Freelancers and small agencies.** The self-serve Pro buyer: one person, many client
   apps, no ops team. A link per client app is the pitch.

The math: $240 MRR is 27 Pro accounts; $1,000 MRR is 112. If 3 in 100 free accounts
upgrade (an assumption, not a measurement), 27 Pro accounts needs about 900 sign-ups.
So the early dollars come from motions 1 and 2. One offer worth adding: annual Pro at $90.

## Marketing

The link is the marketing; the channels only put it in front of people.

1. **The tail page itself.** Every shared link is opened by another developer.
2. **One recording, 12 seconds, no voice.** Terminal left, browser right. `tapes/` exists.
3. **Packages.** The Serilog sink on NuGet and a tiny JavaScript client on npm, both
   taking just the link. NuGet is on the ask-first list.
4. **Show HN, once.** "Show HN: QikLog - a URL you can console.log to". Then r/dotnet with
   the Serilog angle and r/webdev with the webhook angle.
5. **Coding agents.** A bet. An agent cannot SSH or read a dashboard, but it can curl.
   Ship the three-line snippet for CLAUDE.md and .cursor/rules.
6. **Pain pages.** One short page per search someone types while stuck. Start in January.

Not now: ads, Product Hunt, an observability blog, more SDKs than curl, C# and JavaScript.

## Sequence

| Phase | Dates | What happens | Done when |
| --- | --- | --- | --- |
| 0. First dollar | Nov 22 to Nov 29 | Brief steps 9 to 12. Revoke the old hub key. Truth pass. | Someone who is not Jamey has paid $9 |
| 1. The link | Nov 30 to Dec 6 | Log link and keep step. New homepage. README. Recording. | Blank browser to your own line from real code in under 60 seconds |
| 2. Quiet test | Dec 7 to Dec 20 | 30 developers, invited one by one. No public posts. | The thresholds pass, or you know which one failed |
| 3. Launch | Tue Jan 5 | Show HN in the morning, Eastern. Reddit later that week. | 20 people who used it in two separate weeks |
| 4. Compound | January on | Pain pages, the agent snippet, Scotty conversations. | $240 MRR, target moved to Jan 31 |

Two hard gates before any stranger traffic: the leaked hub key is revoked, and Stripe is live.

## Numbers that decide

| Event | What it answers |
| --- | --- |
| Link created | Is the message working? |
| First line received from outside a browser | Did they put it in real code? |
| Link kept (sign-up) | Is it worth keeping? |
| Came back in a second week | Is it a habit or a toy? |
| Upgraded to Pro | Will they pay? |

Quiet test thresholds, out of 30 invited developers: 20 send a real line unaided, median
time to first line under 2 minutes, 10 come back in the second week, 3 pay.

Stop rule: if two rounds of 30 produce nobody who returns in week two, stop spending on
self-serve. QikLog stays as the Scotty bundle feature and as a showcase project.

## Risks and honest objections

| Objection or risk | Honest answer |
| --- | --- |
| "Why not `railway logs`, `fly logs` or the Azure log stream?" | If those cover you, use them. QikLog is for when there is no shell, when you want one line you chose instead of the firehose, and when you want to hand someone a link. |
| "Neat", then back to SSH | The keep step puts the link in the app's environment. The week-two number says whether it worked. |
| Someone logs a secret to an unlisted link | Long random slug, 24-hour expiry, a visible warning, docs that say so. |
| Abuse of an endpoint with no key | Per-address limits, line and size caps, lines that belong to no tenant. |
| "Is it open source?" | Decide before launch. Today: the source is public to read, the product is hosted. |
| It already exists | Similar tools exist. None owns the .NET developer, the keep step, or the phrase. |

## Launch kit

Hero:

```text
console.log for your server.
Get a link. Send it a line. Watch it land.

[ Get a log link ]

No account. No agent. No dashboard.
```

Message to someone in your network:

```text
I built a small thing: a URL you can console.log to from a deployed app,
and watch in a browser tab. Do you have something live you are poking at
this week? I would like 10 minutes to watch you wire it in and tell me
where it annoys you. If you keep using it, it is $9 a month.
```

Show HN:

```text
Title: Show HN: QikLog - a URL you can console.log to

I kept SSHing into servers to read one log line. I wanted the same move
as console.log("it broke"), but for code that is not on my machine.

Click the button, get a URL, POST a line to it, watch it in the tab.
No account for the first 24 hours.

If your host's log command already does what you need, use that. This is
for when there is no shell (webhooks, cron, serverless, a client's box),
or when you want to hand a teammate a link.

Built in .NET 9 with SignalR. Free up to 10,000 lines a month, $9 after.
I would like to know where it breaks for you.
```

Agent snippet, for CLAUDE.md or .cursor/rules:

```text
When debugging deployed code, do not guess. Log to QIKLOG_URL and read it back.
Send:  curl "$QIKLOG_URL" -d "checkout: cart was null"
Read:  curl "$QIKLOG_URL/raw?last=50"
```

Recording, 12 seconds: click Get a log link; paste the curl and press Enter; the line
lands; hold two seconds; end.
