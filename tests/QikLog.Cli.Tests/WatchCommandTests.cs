using QikLog.Cli;
using QikLog.Core;
using Shouldly;
using Xunit;

namespace QikLog.Cli.Tests;

public sealed class LogLineFormatterTests
{
    [Fact]
    public void Given_log_entry_When_formatting_Then_includes_time_level_source_message()
    {
        // Given: a log entry like the browser tail receives
        var entry = new LogEntry(
            "demo",
            LogLevel.Warning,
            "disk nearly full",
            new DateTimeOffset(2026, 8, 16, 21, 5, 6, 789, TimeSpan.Zero));

        // When: the CLI formats it for stdout
        var line = LogLineFormatter.Format(entry);

        // Then: timestamp, level label, source, and message are present
        line.ShouldBe("21:05:06.789 WARN  demo disk nearly full");
    }

    [Theory]
    [InlineData(LogLevel.Info, "INFO")]
    [InlineData(LogLevel.Error, "ERROR")]
    [InlineData(LogLevel.Critical, "CRIT")]
    public void Given_level_When_labeling_Then_uses_short_wire_name(LogLevel level, string expected)
    {
        // Given / When / Then
        LogLineFormatter.LevelLabel(level).ShouldBe(expected);
    }
}

public sealed class WatchSessionAuthTests
{
    [Fact]
    public async Task Given_missing_api_key_When_watch_Then_fails_clearly_without_hang()
    {
        // Given: no --key and no QIKLOG_API_KEY
        var prior = Environment.GetEnvironmentVariable("QIKLOG_API_KEY");
        Environment.SetEnvironmentVariable("QIKLOG_API_KEY", null);
        try
        {
            await using var output = new StringWriter();
            await using var error = new StringWriter();

            // When: watch runs
            var exit = await WatchSession.RunAsync(
                "http://127.0.0.1:9",
                "demo",
                apiKey: null,
                output,
                error,
                CancellationToken.None);

            // Then: non-zero exit and a clear stderr message (no hang)
            exit.ShouldBe(1);
            error.ToString().ShouldContain("missing API key");
        }
        finally
        {
            Environment.SetEnvironmentVariable("QIKLOG_API_KEY", prior);
        }
    }

    [Fact]
    public async Task Given_cancelled_token_When_watch_Then_exits_zero()
    {
        // Given: a key is present but the token is already cancelled (Ctrl+C analogue)
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await using var output = new StringWriter();
        await using var error = new StringWriter();

        // When: watch is started under cancellation
        var exit = await WatchSession.RunAsync(
            "http://127.0.0.1:9",
            "demo",
            apiKey: "ql_test_placeholder",
            output,
            error,
            cts.Token);

        // Then: clean exit, no hang
        exit.ShouldBe(0);
    }

    [Fact]
    public void Given_key_option_When_resolving_Then_prefers_explicit_over_env()
    {
        // Given
        var prior = Environment.GetEnvironmentVariable("QIKLOG_API_KEY");
        Environment.SetEnvironmentVariable("QIKLOG_API_KEY", "from-env");
        try
        {
            // When / Then
            WatchSession.ResolveApiKey("from-flag").ShouldBe("from-flag");
            WatchSession.ResolveApiKey(null).ShouldBe("from-env");
        }
        finally
        {
            Environment.SetEnvironmentVariable("QIKLOG_API_KEY", prior);
        }
    }
}
