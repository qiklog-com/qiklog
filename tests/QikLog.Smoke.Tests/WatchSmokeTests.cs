using System.Diagnostics;
using Microsoft.AspNetCore.SignalR.Client;
using QikLog.Cli;
using QikLog.Core;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace QikLog.Smoke.Tests;

/// <summary>
/// Live CLI watch path: SignalR Subscribe + LogReceived, same hub as the browser.
/// </summary>
[Trait("Category", "Smoke")]
public sealed class WatchSmokeTests
{
    private static string Api => SmokeEnvironment.ApiUrl;

    [AuthenticatedSmokeFact]
    public async Task Given_watch_subscribed_When_send_Then_line_arrives_on_hub()
    {
        // Given: authenticated SignalR watch on a dedicated source (Subscribe does not replay history)
        var key = SmokeEnvironment.ApiKey!;
        var source = $"smoke-watch-{Guid.NewGuid():N}"[..20];
        var marker = $"watch-live {Guid.NewGuid():N}";

        await using var connection = WatchSession.BuildConnection($"{Api}/hubs/logs", key);
        var received = new TaskCompletionSource<LogEntry>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<LogEntry>("LogReceived", entry =>
        {
            if (entry.Message.Contains(marker, StringComparison.Ordinal))
                received.TrySetResult(entry);
        });

        await connection.StartAsync();
        await connection.InvokeAsync("Subscribe", source);

        // When: a log is sent via the CLI send path (not curl)
        var send = await LogSender.SendAsync(Api, key, source, marker, LogLevel.Info, CancellationToken.None);
        send.ExitCode.ShouldBe(0, send.Error ?? "send failed");

        // Then: the line appears on the hub without CLI-side polling
        var completed = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(15)));
        completed.ShouldBe(received.Task, "timed out waiting for LogReceived after CLI send");
        var entry = await received.Task;
        entry.Source.ShouldBe(source);
        entry.Message.ShouldBe(marker);
    }

    [AuthenticatedSmokeFact]
    public async Task Given_wrong_api_key_When_watch_connect_Then_fails_clearly()
    {
        // Given / When: watch with a nonsense key
        await using var stdout = new StringWriter();
        await using var stderr = new StringWriter();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

        var exit = await WatchSession.RunAsync(
            Api,
            "demo",
            apiKey: "ql_dead_beef_not_a_real_key_xxxxxxxx",
            stdout,
            stderr,
            cts.Token);

        // Then: fails with auth messaging, does not hang forever
        exit.ShouldBe(1);
        stderr.ToString().ShouldContain("authentication rejected");
    }
}

/// <summary>
/// Opt-in latency measurement for marketing numbers. Reports ms; does not assert a threshold.
/// </summary>
[Trait("Category", "Smoke")]
public sealed class WatchTimingTests(ITestOutputHelper output)
{
    [TimingFact]
    public async Task Measure_same_machine_send_to_signalr_receive_latency()
    {
        // Given: watch subscribed; single-process clock; CLI LogSender (same as `qiklog send`)
        var api = SmokeEnvironment.ApiUrl;
        var key = SmokeEnvironment.ApiKey!;
        var source = $"timing-{Guid.NewGuid():N}"[..16];
        var marker = $"timing {Guid.NewGuid():N}";

        await using var connection = WatchSession.BuildConnection($"{api}/hubs/logs", key);
        var received = new TaskCompletionSource<long>(TaskCreationOptions.RunContinuationsAsynchronously);
        var clock = Stopwatch.StartNew();

        connection.On<LogEntry>("LogReceived", entry =>
        {
            if (entry.Message.Contains(marker, StringComparison.Ordinal))
                received.TrySetResult(clock.ElapsedMilliseconds);
        });

        await connection.StartAsync();
        await connection.InvokeAsync("Subscribe", source);

        // When: send via CLI send path and record elapsed until LogReceived
        var sendStartedMs = clock.ElapsedMilliseconds;
        var send = await LogSender.SendAsync(api, key, source, marker, LogLevel.Info, CancellationToken.None);
        send.ExitCode.ShouldBe(0, send.Error ?? "send failed");

        var completed = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(20)));
        completed.ShouldBe(received.Task, "timed out waiting for LogReceived during timing run");

        var receivedAtMs = await received.Task;
        var latencyMs = receivedAtMs - sendStartedMs;

        // Then: report the measurement (not a pass/fail threshold)
        output.WriteLine($"QikLog send→SignalR receive latency: {latencyMs} ms (same machine, single process clock)");
        Console.WriteLine($"QikLog send→SignalR receive latency: {latencyMs} ms");
        latencyMs.ShouldBeGreaterThanOrEqualTo(0);
    }
}
