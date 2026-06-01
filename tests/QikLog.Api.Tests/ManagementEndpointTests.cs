using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using QikLog.Core.Management;
using Shouldly;
using Xunit;

namespace QikLog.Api.Tests;

public sealed class ManagementApiWebApplicationFactory : QikLogApiWebApplicationFactory
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["QikLog:Management:Enabled"] = "true"
            });
        });
    }
}

public sealed class ManagementEndpointTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly ManagementApiWebApplicationFactory _factory = new();
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task List_keys_returns_ok()
    {
        var response = await _client.GetAsync("/v1/keys");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var keys = await response.Content.ReadFromJsonAsync<List<ApiKeySummary>>(JsonOptions);
        keys.ShouldNotBeNull();
    }

    [Fact]
    public async Task Create_list_and_revoke_key()
    {
        var createResponse = await _client.PostAsync(
            "/v1/keys",
            new StringContent("""{"name":"ui test"}""", Encoding.UTF8, "application/json"));
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var doc = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = doc.GetProperty("id").GetGuid();
        doc.GetProperty("key").GetString().ShouldNotBeNullOrWhiteSpace();

        var keys = await _client.GetFromJsonAsync<List<ApiKeySummary>>("/v1/keys", JsonOptions);
        keys!.Single(k => k.Id == id).IsActive.ShouldBeTrue();

        var revoke = await _client.PostAsync($"/v1/keys/{id}/revoke", null);
        revoke.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        keys = await _client.GetFromJsonAsync<List<ApiKeySummary>>("/v1/keys", JsonOptions);
        keys!.Single(k => k.Id == id).IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task List_sources_after_ingest()
    {
        var sourceName = $"mgmt-{Guid.NewGuid():N}"[..12];
        var ingestJson = JsonSerializer.Serialize(new { source = sourceName, message = "hello" });
        await _client.PostAsync(
            "/v1/logs",
            new StringContent(ingestJson, Encoding.UTF8, "application/json"));

        var sources = await _client.GetFromJsonAsync<List<SourceSummary>>("/v1/sources", JsonOptions);
        sources.ShouldNotBeNull();
        sources!.Any(s => s.Name == sourceName).ShouldBeTrue();
    }

    [Fact]
    public async Task Management_disabled_returns_not_found()
    {
        await using var bareFactory = new QikLogApiWebApplicationFactory();
        var client = bareFactory.CreateClient();
        var response = await client.GetAsync("/v1/keys");
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
