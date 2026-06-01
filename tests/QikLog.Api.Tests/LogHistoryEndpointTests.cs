using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using QikLog.Core;
using Shouldly;
using Xunit;

namespace QikLog.Api.Tests;

public sealed class LogHistoryEndpointTests(QikLogApiWebApplicationFactory factory) : IClassFixture<QikLogApiWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new LogLevelJsonConverter() }
    };

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Get_source_logs_returns_recent_entries_newest_last()
    {
        await _client.PostAsync(
            "/v1/logs",
            new StringContent("""{"source":"hist","message":"first"}""", Encoding.UTF8, "application/json"));
        await _client.PostAsync(
            "/v1/logs",
            new StringContent("""{"source":"hist","message":"second"}""", Encoding.UTF8, "application/json"));

        var response = await _client.GetAsync("/v1/sources/hist/logs?limit=10");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var entries = await response.Content.ReadFromJsonAsync<List<LogEntry>>(JsonOptions);
        entries!.Count.ShouldBe(2);
        entries[0].Message.ShouldBe("first");
        entries[1].Message.ShouldBe("second");
    }
}
