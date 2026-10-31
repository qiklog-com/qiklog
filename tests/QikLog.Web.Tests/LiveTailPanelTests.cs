using Bunit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.FluentUI.AspNetCore.Components;
using NSubstitute;
using QikLog.Web.Components.Shared;
using QikLog.Web.Services;
using Shouldly;
using Xunit;
using LogEntry = QikLog.Core.LogEntry;
using LogLevel = QikLog.Core.LogLevel;

namespace QikLog.Web.Tests;

public sealed class LiveTailPanelTests : TestContext
{
    public LiveTailPanelTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddFluentUIComponents();
        Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor());
        Services.AddSingleton<ILogger<LiveTailPanel>>(NullLogger<LiveTailPanel>.Instance);
        Services.AddSingleton<IOptions<QikLogOptions>>(
            Options.Create(new QikLogOptions { ApiBaseUrl = "" }));
    }

    [Fact]
    public void Empty_full_mode_renders_Opening_night_and_three_quickstart_steps()
    {
        UseHistory([]);

        var cut = RenderPanel(compact: false);

        cut.Markup.ShouldContain("Opening night.");
        cut.Markup.ShouldContain("dotnet add package QikLog.Serilog");
        cut.Markup.ShouldContain("WriteTo.QikLog(");
        cut.Markup.ShouldContain("Authorization: Bearer");
        cut.FindAll("ol.ql-stage-opening-steps li").Count.ShouldBe(3);
    }

    [Fact]
    public void Empty_compact_mode_renders_waiting_line()
    {
        UseHistory([]);

        var cut = RenderPanel(compact: true);

        cut.Markup.ShouldContain("waiting for your first event");
        cut.Markup.ShouldNotContain("Opening night.");
    }

    [Fact]
    public void Stream_with_entries_renders_time_level_source_message()
    {
        var ts = DateTimeOffset.Parse("2026-03-08T14:05:06.123Z");
        UseHistory([new LogEntry("demo", LogLevel.Info, "hello stage", ts)]);

        var cut = RenderPanel(compact: false);

        cut.Find(".ql-stage-time").TextContent.ShouldBe("14:05:06.123");
        cut.Find(".ql-stage-lvl").TextContent.ShouldBe("INFO");
        cut.Find(".ql-stage-source").TextContent.ShouldBe("demo");
        cut.Find(".ql-stage-msg").TextContent.ShouldBe("hello stage");
        cut.Markup.ShouldNotContain("ql-stage-first");
        cut.Markup.ShouldNotContain("That is your app, live.");
    }

    [Fact]
    public void First_ever_row_carries_ql_stage_first_and_caption()
    {
        UseHistory([]);

        var cut = RenderPanel(compact: false);
        var ts = DateTimeOffset.Parse("2026-03-08T14:05:06.123Z");
        cut.Instance.AppendLiveForTests([new LogEntry("demo", LogLevel.Info, "hello from app", ts)]);
        cut.Render();

        cut.Find(".ql-stage-first").ShouldNotBeNull();
        cut.Find(".ql-stage-first-caption").TextContent.ShouldBe("That is your app, live.");
        cut.Find(".ql-stage-time").TextContent.ShouldBe("14:05:06.123");
        cut.Find(".ql-stage-lvl").TextContent.ShouldBe("INFO");
        cut.Find(".ql-stage-source").TextContent.ShouldBe("demo");
        cut.Find(".ql-stage-msg").TextContent.ShouldBe("hello from app");
    }

    private IRenderedComponent<LiveTailPanel> RenderPanel(bool compact) =>
        RenderComponent<LiveTailPanel>(parameters => parameters
            .Add(p => p.Source, "demo")
            .Add(p => p.Compact, compact));

    private void UseHistory(IReadOnlyList<LogEntry> history)
    {
        var api = Substitute.For<IQikLogApiClient>();
        api.GetSourceHistoryAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(history);
        Services.AddSingleton(api);
    }
}
