Sell a disposable log URL. The buyer is a developer who is stuck right now and wants the same move as `console.log("it broke")`, except the line shows up in a browser tab they can paste into Slack. That is a bit.ly-shaped product. The site is currently selling a small observability product, and that category loses to Datadog before the first sentence ends.

## What a stranger actually gets

I went through qiklog.com and clicked Send. The line `hello from the browser` showed up in the tail in 803ms. That moment is the whole company. Then the page takes it back.

The headline is "See what your app is doing, live." Any APM can say that. The big button, Try it now, leaves the page. The thing worth copying is a six-line curl that demands `Authorization: Bearer $QIKLOG_API_KEY` and a JSON body with `source`, `level`, and `message`. The tail on the homepage is one shared `demo` source, so it is a lobby, not your link. Under that, the page sells a 278ms median. Someone mid-debug does not care about your median. They care whether the line showed up.

`app.qiklog.com` is closer. It mints a private tail (`demo-of39` when I opened it) and says it disappears when you leave. The curl underneath still asks for an API key, and the next step is "create a free account to get your own key." The README in the repo still says pre-alpha and that the paid product is not available. The homepage sells a free tier and Pro. A stranger who clicks Docs or GitHub gets talked out of the thing the homepage just asked them to buy.

## The page

One screen. No scroll chapters.

A button: **Get a log link.** It returns immediately:

`https://qiklog.com/l/k7mq`

with that tail already open, and one line they can paste:

```bash
curl https://qiklog.com/l/k7mq -d "it broke"
```

When the line appears, then offer "Keep this link." Anonymous links expire in 24 hours, are public, and are rate limited. An account gives a name, privacy, and a longer life. $9/mo is for the person who already put the URL in an app and does not want it to die. That is why people pay ngrok and bit.ly. Price stays off the first screen.

The curl has to work with no key. If the first snippet still requires a Bearer token, the page is advertising a product you do not have. Keys belong to the "keep this" step, for when a public URL should not sit in production.

Say this out loud and stop: **"Paste this URL anywhere you would write console.log. Open the URL. That is the log."** Words like ingest, source, SignalR, median, and Bearer are docs words. Lead with curl and `console.log`. One line under that: there is a Serilog sink. Your people will find it. Putting .NET in the headline shrinks the room before the trick lands.

## Where to put it

The buyer is not a team evaluating vendors. They are one person, annoyed, tonight. Do not buy ads and do not write an observability blog.

- Show HN, titled like the product: "A public URL for console.log." A gif of one curl and the line appearing. No architecture.
- One post on X and the programming subreddits with that same twelve-second recording. Caption: "I got tired of SSHing in to see a log line."
- Reply where the sentence already exists: "remote console.log", "webhook.site for logs", "how do I see a log from production." Paste the one-liner.
- Make the GitHub README those same fifteen lines. Move the architecture essay into `docs/`. The repo is currently wearing a contributor's face on a product someone might buy.
- Publish the NuGet sink under that same promise. That is the .NET distribution wedge, and it belongs on NuGet, not in the hero.

Drop the scroll-snap scenes, the latency stat, and the shared demo bucket. The link is the marketing.

**Confidence: High.** I clicked through the live homepage and the app try page, and read the homepage samples plus the repo README. I did not check whether the live GitHub README matches this checkout.
