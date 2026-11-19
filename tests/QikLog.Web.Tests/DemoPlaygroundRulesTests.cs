using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using QikLog.Core;
using LogLevel = QikLog.Core.LogLevel;
using QikLog.Web.Demo;
using Shouldly;
using Xunit;

namespace QikLog.Web.Tests;

public sealed class DemoPlaygroundRulesTests
{
    [Fact]
    public void Given_matching_line_arrives_When_noted_Then_latency_and_best_show()
    {
        // Given: a send at t0 and the same line 15 ms later, then a slower one
        var time = new ManualTimeProvider();
        var tracker = new DemoLatencyTracker();
        var first = time.GetTimestamp();
        tracker.MarkSent(LogLevel.Error, "boom", first);
        time.Advance(TimeSpan.FromMilliseconds(15));
        tracker.Observe(LogLevel.Error, "boom", time.GetTimestamp(), time);

        // When: the note is taken and a slower line arrives
        var note = tracker.Take(LogLevel.Error, "boom");
        var slowStart = time.GetTimestamp();
        tracker.MarkSent(LogLevel.Info, "slow", slowStart);
        time.Advance(TimeSpan.FromMilliseconds(40));
        tracker.Observe(LogLevel.Info, "slow", time.GetTimestamp(), time);

        // Then: the first arrival is 15 ms and it remains the best
        note.ShouldBe("arrived in 15 ms");
        tracker.BestMs.ShouldBe(15);
        tracker.Take(LogLevel.Info, "slow").ShouldBe("arrived in 40 ms");
    }

    [Fact]
    public void Given_limits_When_exceeded_Then_friendly_denial()
    {
        // Given: one circuit clock and a shared IP window
        var window = new DemoIpWindow();
        var time = new ManualTimeProvider();
        var gate = Gate(window, time, "203.0.113.8");

        // When: a second manual send is inside one second
        gate.TryManual().Allowed.ShouldBeTrue();
        var tooFast = gate.TryManual();

        // Then: it is refused and does not consume another line
        tooFast.Message.ShouldBe("Give it a second.");
        gate.PeekManual().Kind.ShouldBe(DemoLimitKind.SendTooFast);
        time.Advance(TimeSpan.FromSeconds(1));
        gate.TryManual().Allowed.ShouldBeTrue();

        // When: a second burst is inside 30 seconds
        var burst = new DemoRateGate(new DemoSession("demo-burst", "203.0.113.9"), window, time);
        burst.TryBurst().Allowed.ShouldBeTrue();
        var cooling = burst.TryBurst();

        // Then: the burst is refused and the eight lines were not reserved twice
        cooling.Message.ShouldBe("Simulate is cooling down.");
        burst.PeekBurst().Kind.ShouldBe(DemoLimitKind.BurstCooling);

        // When: the 61st line is requested
        var full = Gate(new DemoIpWindow(), time, "203.0.113.7");
        for (var i = 0; i < 60; i++)
        {
            time.Advance(TimeSpan.FromSeconds(1));
            full.TryManual().Allowed.ShouldBeTrue();
        }

        var pastCircuit = full.TryManual();

        // Then: the visit is over
        pastCircuit.Message.ShouldBe("That's the limit for this visit.");

        // When: one IP has already sent 300 lines across circuits
        var shared = new DemoIpWindow();
        var sharedClock = new ManualTimeProvider();
        for (var circuit = 0; circuit < 5; circuit++)
        {
            var part = Gate(shared, sharedClock, "203.0.113.4");
            for (var n = 0; n < 60; n++)
            {
                sharedClock.Advance(TimeSpan.FromSeconds(1));
                part.TryManual().Allowed.ShouldBeTrue();
            }
        }

        var overflow = Gate(shared, sharedClock, "203.0.113.4");
        var pastIp = overflow.TryManual();

        // Then: the hourly cap refuses the next line
        pastIp.Message.ShouldBe("This network is at its hourly limit.");

        // And: a burst that needs 8 lines with 7 remaining is refused entirely
        var almost = Gate(new DemoIpWindow(), time, "203.0.113.6");
        for (var i = 0; i < 53; i++)
        {
            time.Advance(TimeSpan.FromSeconds(1));
            almost.TryManual().Allowed.ShouldBeTrue();
        }

        var refused = almost.TryBurst();
        refused.Message.ShouldBe("That's the limit for this visit.");
        time.Advance(TimeSpan.FromSeconds(1));
        almost.PeekManual().Allowed.ShouldBeTrue();

        // And: a missing IP does not share a bucket, so six circuits can each send 60
        var open = new DemoIpWindow();
        var openClock = new ManualTimeProvider();
        for (var circuit = 0; circuit < 6; circuit++)
        {
            var part = Gate(open, openClock, ip: null);
            for (var n = 0; n < 60; n++)
            {
                openClock.Advance(TimeSpan.FromSeconds(1));
                part.TryManual().Allowed.ShouldBeTrue();
            }
        }
    }

    [Fact]
    public void Given_code_deck_When_templates_compared_Then_they_match()
    {
        // Given: the marketing code deck
        var path = FindRepoFile("www/src/lib/code-deck.ts");
        var templates = ReadTemplates(path);

        // When: the three template strings are read
        templates.Count.ShouldBe(3);

        // Then: they equal the C# copies
        templates[0].ShouldBe(DemoCodeSamples.CurlTemplate);
        templates[1].ShouldBe(DemoCodeSamples.CSharpTemplate);
        templates[2].ShouldBe(DemoCodeSamples.JavaScriptTemplate);

        var rendered = DemoCodeSamples.Render(DemoCodeTab.Curl, "demo-ab12", "warning", "say \"hi\"");
        rendered.ShouldContain("demo-ab12");
        rendered.ShouldContain("warning");
        rendered.ShouldContain("say \\\"hi\\\"");
        rendered.ShouldContain("$QIKLOG_API_KEY");
        DemoCodeSamples.Render(DemoCodeTab.CSharp, "demo-ab12", "warning", "say \"hi\"")
            .ShouldContain("YOUR_API_KEY");
    }

    [Fact]
    public async Task Given_demo_key_When_posting_Then_key_is_a_header_not_a_url()
    {
        // Given: a configured demo key
        const string demoKey = "ql_demo_secret_value";
        var handler = new CaptureHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://api.test/") };
        var client = new DemoIngestClient(
            http,
            Options.Create(new QikLogOptions { Demo = new DemoOptions { ApiKey = demoKey } }),
            NullLogger<DemoIngestClient>.Instance);

        // When: the server posts a line
        var ok = await client.SendAsync("demo-ab12", LogLevel.Error, "boom", CancellationToken.None);

        // Then: the URL does not carry the key, and the API is told via Authorization
        ok.ShouldBeTrue();
        handler.Request.ShouldNotBeNull();
        handler.Request.RequestUri!.AbsoluteUri.ShouldBe("http://api.test/v1/logs");
        handler.Request.RequestUri.AbsoluteUri.ShouldNotContain(demoKey);
        handler.Request.Headers.Authorization.ShouldBe(new AuthenticationHeaderValue("Bearer", demoKey));
    }

    private static DemoRateGate Gate(DemoIpWindow window, ManualTimeProvider time, string? ip) =>
        new(new DemoSession("demo-gate", ip), window, time);

    private static string FindRepoFile(string relative)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, relative);
            if (File.Exists(candidate))
                return candidate;

            dir = dir.Parent;
        }

        throw new FileNotFoundException(relative);
    }

    private static List<string> ReadTemplates(string path)
    {
        var text = File.ReadAllText(path);
        var templates = new List<string>();
        const string marker = "template: `";
        var start = 0;
        while (true)
        {
            var at = text.IndexOf(marker, start, StringComparison.Ordinal);
            if (at < 0)
                break;

            var body = at + marker.Length;
            var end = text.IndexOf('`', body);
            templates.Add(text[body..end].Replace("\\\\", "\\", StringComparison.Ordinal));
            start = end + 1;
        }

        return templates;
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
        }
    }
}
