using Serilog;
using Shouldly;
using Xunit;

namespace QikLog.Smoke.Tests;

/// <summary>
/// Live proof that QikLog.Serilog posts through the same ingest path as curl / CLI send.
/// </summary>
[Trait("Category", "Smoke")]
public sealed class SerilogSinkSmokeTests
{
    [AuthenticatedSmokeFact]
    public async Task Given_serilog_sink_When_information_logged_Then_demo_history_contains_it()
    {
        // Given: authenticated API and the shared demo source
        var key = SmokeEnvironment.ApiKey!;
        var marker = $"serilog-sink {Guid.NewGuid():N}";
        var api = SmokeEnvironment.ApiUrl;

        using (var log = new LoggerConfiguration()
                   .WriteTo.QikLog(api, key, "demo")
                   .CreateLogger())
        {
            // When
            log.Information("{Marker}", marker);
        }

        // Then: history for demo includes the line (same source the landing embed tails)
        string body = "";
        for (var attempt = 1; attempt <= 8; attempt++)
        {
            using var history = await SmokeClient.GetAsync($"{api}/v1/sources/demo/logs?limit=50", key);
            history.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
            body = await history.Content.ReadAsStringAsync();
            if (body.Contains(marker, StringComparison.Ordinal))
                return;

            await Task.Delay(TimeSpan.FromMilliseconds(400 * attempt));
        }

        throw new Xunit.Sdk.XunitException(
            $"Serilog sink never landed '{marker}' on source demo. Last body: {body}");
    }
}
