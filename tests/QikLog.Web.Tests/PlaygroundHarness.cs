using Bunit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.FluentUI.AspNetCore.Components;
using QikLog.Core;
using LogLevel = QikLog.Core.LogLevel;
using QikLog.Web.Components.Shared;
using QikLog.Web.Demo;
using QikLog.Web.Services;

namespace QikLog.Web.Tests;

internal sealed class ManualTimeProvider : TimeProvider
{
    public DateTimeOffset Now { get; set; } = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public long Stamp { get; set; } = 1_000_000;

    public override DateTimeOffset GetUtcNow() => Now;

    public override long GetTimestamp() => Stamp;

    public override long TimestampFrequency => 1_000;

    public void Advance(TimeSpan by)
    {
        Now += by;
        Stamp += (long)by.TotalMilliseconds;
    }
}

internal sealed class RecordingIngest : IDemoIngest
{
    public List<(string Source, LogLevel Level, string Message)> Sent { get; } = [];

    public bool Ok { get; set; } = true;

    public Task<bool> SendAsync(string source, LogLevel level, string message, CancellationToken cancellationToken)
    {
        Sent.Add((source, level, message));
        return Task.FromResult(Ok);
    }
}

internal sealed class RecordingPacer : IDemoPacer
{
    public List<TimeSpan> Delays { get; } = [];

    public Task PauseAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        Delays.Add(delay);
        return Task.CompletedTask;
    }
}

internal sealed class PlaygroundHarness
{
    public PlaygroundHarness(string? demoKey = "ql_demo_secret_value", string? hubKey = "ql_hub_secret_value", string? ip = "203.0.113.10")
    {
        Options = new QikLogOptions
        {
            ApiBaseUrl = "",
            HubApiKey = hubKey,
            Demo = new DemoOptions { ApiKey = demoKey }
        };
        Session = new DemoSession("demo-ab12", ip);
        Time = new ManualTimeProvider();
        Rate = new DemoRateGate(Session, new DemoIpWindow(), Time);
    }

    public QikLogOptions Options { get; }

    public DemoSession Session { get; }

    public ManualTimeProvider Time { get; }

    public DemoRateGate Rate { get; }

    public RecordingIngest Ingest { get; } = new();

    public RecordingPacer Pacer { get; } = new();

    public void Install(TestContext context)
    {
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddFluentUIComponents();
        context.Services.AddSingleton<IOptions<QikLogOptions>>(Microsoft.Extensions.Options.Options.Create(Options));
        context.Services.AddSingleton(Session);
        context.Services.AddSingleton(Rate);
        context.Services.AddSingleton<TimeProvider>(Time);
        context.Services.AddSingleton<IDemoIngest>(Ingest);
        context.Services.AddSingleton<IDemoPacer>(Pacer);
        context.Services.AddSingleton(new QikLogApiClient(new HttpClient()));
        context.Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor());
        context.Services.AddSingleton<ILogger<LiveTailPanel>>(NullLogger<LiveTailPanel>.Instance);
    }
}
