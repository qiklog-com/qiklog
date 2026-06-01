using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using QikLog.Core;
using Shouldly;
using Xunit;

namespace QikLog.Api.Tests;

public sealed class LogHistoryEndpointTests : IAsyncLifetime
{
    private readonly QikLogApiWebApplicationFactory _factory = new();
    private HttpClient _client = null!;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new LogLevelJsonConverter() }
    };

    public async Task InitializeAsync()
    {
        var apiKey = await ApiTestData.CreateApiKeyForPrimaryTenantAsync(_factory.Services);
        _client = _factory.CreateClient();
        ApiTestAuth.SetApiKey(_client, apiKey);
    }

    public async Task DisposeAsync() => await _factory.DisposeAsync();

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
