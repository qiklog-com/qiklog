using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using QikLog.Core;
using LogLevel = QikLog.Core.LogLevel;
using QikLog.Web.Components.Pages;
using QikLog.Web.Components.Shared;
using QikLog.Web.Demo;
using Shouldly;
using Xunit;

namespace QikLog.Web.Tests;

public sealed class PlaygroundBehaviorTests : TestContext
{
    [Fact]
    public void Given_message_and_level_When_send_clicked_Then_server_posts_them()
    {
        // Given: a draft with a control character and the Error chip
        var harness = new PlaygroundHarness();
        harness.Install(this);
        var cut = RenderComponent<Home>();

        // When: the visitor sends
        cut.Find("input").Input("hello\nthere");
        cut.Find("[data-level=error]").Click();
        cut.Find("[data-action=send]").Click();

        // Then: the server posts the sanitized line for this source
        var sent = harness.Ingest.Sent.ShouldHaveSingleItem();
        sent.Source.ShouldBe("demo-ab12");
        sent.Level.ShouldBe(LogLevel.Error);
        sent.Message.ShouldBe("hellothere");
        cut.Markup.ShouldNotContain("ql_demo_secret_value");
    }

    [Fact]
    public void Given_new_session_When_source_created_Then_private_tail_copy()
    {
        // Given: a new home page
        var harness = new PlaygroundHarness();
        harness.Install(this);

        // When: it renders
        var cut = RenderComponent<Home>();

        // Then: the sample uses the private source and the public warning is gone
        cut.Markup.ShouldContain("demo-ab12");
        DemoSourceNames.Create(4, _ => 0).ShouldBe("demo-aaaa");
        for (var i = 0; i < 40; i++)
            DemoSourceNames.Create().ShouldMatch(@"^demo-[a-z0-9]{4,6}$");
        cut.Markup.ShouldContain("This tail is just yours. It disappears when you leave.");
        cut.Markup.ShouldNotContain("Treat anything you send as public");
    }

    [Fact]
    public void Given_two_lines_sent_When_third_succeeds_Then_nudge_appears()
    {
        // Given: two successful sends
        var harness = new PlaygroundHarness();
        harness.Install(this);
        var cut = RenderComponent<Home>();
        Send(cut, harness, "one");
        Send(cut, harness, "two");
        cut.Markup.ShouldNotContain("That's QikLog.");

        // When: the third succeeds
        Send(cut, harness, "three");

        // Then: the quiet card links to sign-in
        cut.Markup.ShouldContain("That's QikLog.");
        cut.Markup.ShouldContain("free up to 10,000 lines a month.");
        cut.Find("a[href='/login']").ShouldNotBeNull();
        cut.Markup.ShouldContain("Get your own key");
    }

    [Fact]
    public void Given_simulate_When_burst_runs_Then_eight_scripted_lines_are_spaced()
    {
        // Given: the static script
        var harness = new PlaygroundHarness();
        harness.Install(this);
        var cut = RenderComponent<Home>();

        // When: the burst runs without waiting on the clock
        cut.Find("[data-action=simulate]").Click();

        // Then: eight posts, the three examples, and 375 ms gaps
        harness.Ingest.Sent.Count.ShouldBe(8);
        harness.Ingest.Sent.Select(item => item.Message).ShouldBe(DemoScript.Lines.Select(line => line.Message));
        harness.Ingest.Sent.ShouldContain(item => item.Message == "200 GET /api/orders 24ms");
        harness.Ingest.Sent.ShouldContain(item => item.Message == "WARN slow query orders_by_customer 812ms");
        harness.Ingest.Sent.ShouldContain(item => item.Message == "ERROR 500 POST /checkout NullReferenceException");
        harness.Pacer.Delays.Count.ShouldBe(7);
        harness.Pacer.Delays.ShouldAllBe(delay => delay == DemoScript.Gap);
    }

    [Fact]
    public void Given_circuit_full_When_home_rendered_Then_buttons_disable_with_message()
    {
        // Given: the circuit has already used its 60 lines
        var harness = new PlaygroundHarness();
        for (var i = 0; i < 60; i++)
        {
            harness.Time.Advance(TimeSpan.FromSeconds(1));
            harness.Rate.TryManual().Allowed.ShouldBeTrue();
        }

        harness.Install(this);

        // When: the page renders
        var cut = RenderComponent<Home>();

        // Then: both controls are disabled and the visit message is shown
        cut.Markup.ShouldContain("That's the limit for this visit.");
        cut.Find("[data-action=send]").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("[data-action=simulate]").HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public async Task Given_error_line_When_note_applied_Then_row_uses_level_color_and_latency()
    {
        // Given: a tail that stamps a known note
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddFluentUIComponents();
        Services.AddSingleton<Microsoft.Extensions.Options.IOptions<QikLogOptions>>(
            Microsoft.Extensions.Options.Options.Create(new QikLogOptions { ApiBaseUrl = "" }));
        Services.AddSingleton(new QikLog.Web.Services.QikLogApiClient(new HttpClient()));
        Services.AddSingleton<Microsoft.AspNetCore.Http.IHttpContextAccessor>(new Microsoft.AspNetCore.Http.HttpContextAccessor());
        Services.AddSingleton<Microsoft.Extensions.Logging.ILogger<LiveTailPanel>>(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<LiveTailPanel>.Instance);

        var cut = RenderComponent<LiveTailPanel>(parameters => parameters
            .Add(p => p.Source, "demo-ab12")
            .Add(p => p.Compact, true)
            .Add(p => p.NoteFor, _ => "arrived in 15 ms"));

        // When: a line is appended the way a flushed event is
        await cut.InvokeAsync(() =>
            cut.Instance.AppendForTests(new LogEntry("demo-ab12", LogLevel.Error, "boom", DateTimeOffset.UtcNow)));

        // Then: the note and the error color token are on the row
        cut.Markup.ShouldContain("arrived in 15 ms");
        cut.Markup.ShouldContain("--ql-level-error");
    }

    private static void Send(IRenderedComponent<Home> cut, PlaygroundHarness harness, string message)
    {
        harness.Time.Advance(TimeSpan.FromSeconds(1));
        cut.Find("input").Input(message);
        cut.Render();
        cut.Find("[data-action=send]").Click();
    }
}
