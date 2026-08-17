using System.Net;
using System.Text.Json;
using Serilog;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using Shouldly;
using Xunit;

namespace QikLog.Serilog.Tests;

public sealed class QikLogSinkTests
{
    [Theory]
    [InlineData(LogEventLevel.Verbose, 0)]
    [InlineData(LogEventLevel.Debug, 1)]
    [InlineData(LogEventLevel.Information, 2)]
    [InlineData(LogEventLevel.Warning, 3)]
    [InlineData(LogEventLevel.Error, 4)]
    [InlineData(LogEventLevel.Fatal, 5)]
    public void Given_serilog_level_When_mapped_Then_matches_qiklog_enum(
        LogEventLevel serilogLevel,
        int expected)
    {
        // Given / When
        var actual = QikLogLogEventMapper.ToQikLogLevel(serilogLevel);

        // Then: same integers the CLI sends and LogEntryDto accepts
        actual.ShouldBe(expected);
        actual.ShouldBe((int)(QikLog.Core.LogLevel)expected);
    }

    [Fact]
    public async Task Given_information_event_When_flushed_Then_payload_matches_ingest_contract()
    {
        // Given: a sink posting the same JSON qiklog send uses
        var handler = new RecordingHandler();
        using var log = CreateLogger(handler, batchSize: 1);

        // When
        log.Information("hello from serilog {OrderId}", "ord_9");
        await WaitForPosts(handler, 1);

        // Then
        handler.Requests.Count.ShouldBe(1);
        var req = handler.Requests[0];
        req.Method.ShouldBe(HttpMethod.Post);
        req.Uri!.AbsolutePath.ShouldBe("/v1/logs");
        req.AuthScheme.ShouldBe("Bearer");
        req.AuthParameter.ShouldBe("ql_test_key");

        using var json = JsonDocument.Parse(req.Body);
        var root = json.RootElement;
        root.GetProperty("source").GetString().ShouldBe("demo");
        root.GetProperty("message").GetString().ShouldNotBeNullOrWhiteSpace();
        root.GetProperty("message").GetString()!.ShouldContain("hello from serilog");
        root.GetProperty("level").GetInt32().ShouldBe(2);
        root.TryGetProperty("timestamp", out var ts).ShouldBeTrue();
        ts.ValueKind.ShouldNotBe(JsonValueKind.Undefined);
        root.GetProperty("properties").GetProperty("OrderId").GetString().ShouldBe("ord_9");
        req.Body.ShouldNotContain("—");
    }

    [Fact]
    public void Given_three_events_When_batch_size_reached_Then_each_is_posted_to_ingest()
    {
        // Given: ingest is one LogEntry per POST; batching only groups the flush
        var handler = new RecordingHandler();
        using (var log = CreateLogger(handler, batchSize: 3, flush: TimeSpan.FromHours(1)))
        {
            // When: three events fill a batch; dispose flushes the sink
            log.Information("one");
            log.Warning("two");
            log.Error("three");
        }

        // Then
        handler.Requests.Count.ShouldBe(3);
        handler.Requests.Select(r => JsonDocument.Parse(r.Body).RootElement.GetProperty("level").GetInt32())
            .ShouldBe([2, 3, 4]);
    }

    [Fact]
    public void Given_http_failure_When_logging_Then_host_does_not_throw_and_selflog_records()
    {
        // Given
        var handler = new RecordingHandler
        {
            ThrowOnSend = new HttpRequestException("connection refused")
        };
        var selfLog = new StringWriter();
        SelfLog.Enable(selfLog);

        try
        {
            using var log = CreateLogger(handler, batchSize: 1);

            // When / Then: the calling app keeps running
            Should.NotThrow(() => log.Information("still alive"));
            Should.NotThrow(() => log.Dispose());

            selfLog.ToString().ShouldContain("QikLog");
            selfLog.ToString().ShouldContain("connection refused");
        }
        finally
        {
            SelfLog.Disable();
        }
    }

    [Fact]
    public void Given_unauthorized_response_When_logging_Then_selflog_shows_401_not_exception()
    {
        // Given: wrong API key
        var handler = new RecordingHandler { Status = HttpStatusCode.Unauthorized };
        var selfLog = new StringWriter();
        SelfLog.Enable(selfLog);

        try
        {
            using var log = CreateLogger(handler, batchSize: 1);

            // When
            Should.NotThrow(() => log.Error("secret leaked into logs"));
            Should.NotThrow(() => log.Dispose());

            // Then
            selfLog.ToString().ShouldContain("401");
            selfLog.ToString().ShouldNotContain("—");
        }
        finally
        {
            SelfLog.Disable();
        }
    }

    [Fact]
    public void Pack_target_and_readme_exist_for_local_nupkg()
    {
        // Given / When
        var makefile = ReadRepoFile("Makefile");
        var csproj = ReadRepoFile("src/QikLog.Serilog/QikLog.Serilog.csproj");
        var readme = ReadRepoFile("src/QikLog.Serilog/README.md");

        // Then: pack locally; Jamey publishes to nuget.org
        makefile.ShouldContain("pack-serilog");
        makefile.ShouldContain("dotnet pack");
        makefile.ShouldNotContain("nuget push");
        makefile.ShouldNotContain("nuget.org");
        csproj.ShouldContain("IsPackable>true");
        csproj.ShouldContain("PackageReadmeFile");
        readme.ShouldContain("dotnet add package QikLog.Serilog");
        readme.ShouldContain("WriteTo.QikLog");
        readme.ShouldContain("https://api.qiklog.com");
        readme.ShouldNotContain("—");
    }

    private static Logger CreateLogger(
        RecordingHandler handler,
        int batchSize,
        TimeSpan? flush = null)
    {
        return new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.QikLog(
                "https://api.qiklog.com",
                "ql_test_key",
                "demo",
                handler,
                batchSize,
                flush ?? TimeSpan.FromMilliseconds(50))
            .CreateLogger();
    }

    private static async Task WaitForPosts(RecordingHandler handler, int count)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (handler.Requests.Count < count)
        {
            cts.Token.ThrowIfCancellationRequested();
            await Task.Delay(25, cts.Token);
        }
    }

    private static string ReadRepoFile(string relativePath)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "QikLog.sln")))
            dir = dir.Parent;

        dir.ShouldNotBeNull();
        var path = Path.Combine(dir!.FullName, relativePath);
        File.Exists(path).ShouldBeTrue(path);
        return File.ReadAllText(path);
    }
}
