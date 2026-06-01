using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QikLog.Infrastructure.Auth;
using Shouldly;
using Xunit;

namespace QikLog.Api.Tests;

public sealed class AuthenticatedApiFixture : IAsyncLifetime
{
    public AuthenticatedApiWebApplicationFactory Factory { get; } = new();

    public string ApiKey { get; private set; } = "";

    public async Task InitializeAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var keys = scope.ServiceProvider.GetRequiredService<IApiKeyService>();
        var created = await keys.CreateAsync("test", CancellationToken.None);
        ApiKey = created.Plaintext;
    }

    public async Task DisposeAsync() => await Factory.DisposeAsync();
}

public sealed class IngestApiKeyTests(AuthenticatedApiFixture fixture) : IClassFixture<AuthenticatedApiFixture>
{
  private readonly HttpClient _authedClient = CreateAuthedClient(fixture);

    private static HttpClient CreateAuthedClient(AuthenticatedApiFixture fixture)
    {
        var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", fixture.ApiKey);
        return client;
    }

    [Fact]
    public async Task Ingest_without_key_returns_unauthorized_when_required()
    {
        var bare = fixture.Factory.CreateClient();
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
    public async Task Ingest_with_invalid_key_returns_unauthorized()
    {
        var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "ql_00000000_invalidsecretpart0000");
        var response = await client.PostAsync(
            "/v1/logs",
            new StringContent("""{"source":"demo","message":"x"}""", Encoding.UTF8, "application/json"));
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}

public sealed class AuthenticatedApiWebApplicationFactory : QikLogApiWebApplicationFactory
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["QikLog:Ingest:RequireApiKey"] = "true"
            });
        });
    }
}
