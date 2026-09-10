using System.Net;
using Serilog;
using Serilog.Core;
using Serilog.Debugging;
using Shouldly;
using Xunit;

namespace QikLog.Api.Tests;

/// <summary>
/// The Serilog sink against the real ingest pipeline (Bearer auth, 401/403/429, silent drop).
/// </summary>
public sealed class SerilogSinkIngestTests
{
    [Fact]
    public async Task Given_valid_bearer_key_When_sink_logs_Then_history_contains_the_event()
    {
        await using var factory = new QikLogApiWebApplicationFactory();
        var apiKey = await ApiTestData.CreateApiKeyForPrimaryTenantAsync(factory.Services);
        var marker = $"sink-ok {Guid.NewGuid():N}";

        using (var log = CreateSinkLogger(factory, apiKey, "sink-ok"))
        {
            Should.NotThrow(() => log.Information("{Marker}", marker));
        }

        var body = await ReadHistoryAsync(factory, apiKey, "sink-ok");
        body.ShouldContain(marker);
    }

    [Fact]
    public async Task Given_missing_key_When_sink_logs_Then_selflog_records_401_and_event_is_dropped()
    {
        await using var factory = new QikLogApiWebApplicationFactory();
        var readerKey = await ApiTestData.CreateApiKeyForPrimaryTenantAsync(factory.Services, "reader");
        var marker = $"sink-401 {Guid.NewGuid():N}";
        var selfLog = new StringWriter();
        SelfLog.Enable(selfLog);

        try
        {
            using (var log = CreateSinkLogger(factory, apiKey: "", "sink-401"))
            {
                Should.NotThrow(() => log.Warning("{Marker}", marker));
            }

            selfLog.ToString().ShouldContain("401");
            selfLog.ToString().ShouldContain("QikLog sink ingest failed");
        }
        finally
        {
            SelfLog.Disable();
        }

        var body = await ReadHistoryAsync(factory, readerKey, "sink-401");
        body.ShouldNotContain(marker);
    }

    [Fact]
    public async Task Given_invalid_key_When_sink_logs_Then_selflog_records_403_and_event_is_dropped()
    {
        await using var factory = new QikLogApiWebApplicationFactory();
        var readerKey = await ApiTestData.CreateApiKeyForPrimaryTenantAsync(factory.Services, "reader");
        var marker = $"sink-403 {Guid.NewGuid():N}";
        var selfLog = new StringWriter();
        SelfLog.Enable(selfLog);

        try
        {
            using (var log = CreateSinkLogger(factory, "ql_00000000_invalidsecretpart0000", "sink-403"))
            {
                Should.NotThrow(() => log.Error("{Marker}", marker));
            }

            selfLog.ToString().ShouldContain("403");
            selfLog.ToString().ShouldContain("invalid or revoked API key");
        }
        finally
        {
            SelfLog.Disable();
        }

        var body = await ReadHistoryAsync(factory, readerKey, "sink-403");
        body.ShouldNotContain(marker);
    }

    [Fact]
    public async Task Given_per_key_rate_limit_When_sink_exceeds_it_Then_selflog_records_429_and_extra_event_is_dropped()
    {
        await using var factory = new LowRateLimitWebApplicationFactory();
        var apiKey = await ApiTestData.CreateApiKeyForPrimaryTenantAsync(factory.Services);
        var kept = $"sink-kept {Guid.NewGuid():N}";
        var kept2 = $"sink-kept2 {Guid.NewGuid():N}";
        var dropped = $"sink-dropped {Guid.NewGuid():N}";
        var selfLog = new StringWriter();
        SelfLog.Enable(selfLog);

        try
        {
            using (var log = CreateSinkLogger(factory, apiKey, "sink-429"))
            {
                Should.NotThrow(() =>
                {
                    log.Information("{Marker}", kept);
                    log.Information("{Marker}", kept2);
                    log.Information("{Marker}", dropped);
                });
            }

            selfLog.ToString().ShouldContain("429");
            selfLog.ToString().ShouldContain("rate limit exceeded");
        }
        finally
        {
            SelfLog.Disable();
        }

        var body = await ReadHistoryAsync(factory, apiKey, "sink-429");
        body.ShouldContain(kept);
        body.ShouldContain(kept2);
        body.ShouldNotContain(dropped);
    }

    private static Logger CreateSinkLogger(
        QikLogApiWebApplicationFactory factory,
        string apiKey,
        string source)
    {
        var handler = factory.Server.CreateHandler();
        var origin = (factory.Server.BaseAddress ?? new Uri("http://localhost")).ToString();
        return new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.QikLog(
                origin,
                apiKey,
                source,
                handler,
                batchSizeLimit: 1,
                flushInterval: TimeSpan.FromMilliseconds(50))
            .CreateLogger();
    }

    private static async Task<string> ReadHistoryAsync(
        QikLogApiWebApplicationFactory factory,
        string apiKey,
        string source)
    {
        using var client = factory.CreateClient();
        ApiTestAuth.SetApiKey(client, apiKey);
        using var response = await client.GetAsync($"/v1/sources/{source}/logs?limit=50");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await response.Content.ReadAsStringAsync();
    }
}
