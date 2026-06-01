using System.Net;
using System.Text;
using Shouldly;
using Xunit;

namespace QikLog.Api.Tests;

public sealed class IngestApiKeyTests : IAsyncLifetime
{
    private readonly QikLogApiWebApplicationFactory _factory = new();
    private HttpClient _authedClient = null!;
    private string _apiKey = "";

    public async Task InitializeAsync()
    {
        _apiKey = await ApiTestData.CreateApiKeyForPrimaryTenantAsync(_factory.Services);
        _authedClient = _factory.CreateClient();
        ApiTestAuth.SetApiKey(_authedClient, _apiKey);
    }

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task Ingest_without_key_returns_unauthorized()
    {
        var bare = _factory.CreateClient();
        var response = await bare.PostAsync(
            "/v1/logs",
            new StringContent("""{"source":"demo","message":"x"}""", Encoding.UTF8, "application/json"));
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Ingest_with_valid_key_returns_accepted()
    {
        var response = await _authedClient.PostAsync(
            "/v1/logs",
            new StringContent("""{"source":"demo","message":"authed"}""", Encoding.UTF8, "application/json"));
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task Ingest_with_invalid_key_returns_forbidden()
    {
        var client = _factory.CreateClient();
        ApiTestAuth.SetApiKey(client, "ql_00000000_invalidsecretpart0000");
        var response = await client.PostAsync(
            "/v1/logs",
            new StringContent("""{"source":"demo","message":"x"}""", Encoding.UTF8, "application/json"));
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
