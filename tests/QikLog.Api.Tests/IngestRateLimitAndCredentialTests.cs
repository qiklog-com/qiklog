using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QikLog.Infrastructure.Auth;
using QikLog.Infrastructure.Tenants;
using Shouldly;
using Xunit;

namespace QikLog.Api.Tests;

public sealed class LowRateLimitWebApplicationFactory : QikLogApiWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["QikLog:Ingest:RateLimitPerMinute"] = "2"
            });
        });
    }
}

public sealed class IngestRateLimitAndCredentialTests
{
    [Fact]
    public async Task Ingest_exceeding_per_key_rate_limit_returns_429()
    {
        await using var factory = new LowRateLimitWebApplicationFactory();
        var apiKey = await ApiTestData.CreateApiKeyForPrimaryTenantAsync(factory.Services);
        var client = factory.CreateClient();
        ApiTestAuth.SetApiKey(client, apiKey);

        var first = await PostAsync(client, "r1");
        var second = await PostAsync(client, "r2");
        var third = await PostAsync(client, "r3");

        first.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        second.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        third.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Ingest_accepts_legacy_X_Api_Key_header()
    {
        await using var factory = new QikLogApiWebApplicationFactory();
        var apiKey = await ApiTestData.CreateApiKeyForPrimaryTenantAsync(factory.Services);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

        var response = await PostAsync(client, "legacy");
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task Ingest_accepts_Authorization_Bearer_api_key()
    {
        await using var factory = new QikLogApiWebApplicationFactory();
        var apiKey = await ApiTestData.CreateApiKeyForPrimaryTenantAsync(factory.Services);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        var response = await PostAsync(client, "bearer-key");
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task Ingest_accepts_api_key_query_param()
    {
        await using var factory = new QikLogApiWebApplicationFactory();
        var apiKey = await ApiTestData.CreateApiKeyForPrimaryTenantAsync(factory.Services);
        var client = factory.CreateClient();
        var response = await client.PostAsync(
            $"/v1/logs?api_key={Uri.EscapeDataString(apiKey)}",
            new StringContent("""{"source":"query","message":"q"}""", Encoding.UTF8, "application/json"));
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task Ingest_after_revoke_returns_forbidden()
    {
        await using var factory = new QikLogApiWebApplicationFactory();
        string plaintext;
        Guid keyId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var tenant = scope.ServiceProvider.GetRequiredService<ITenantContext>();
            tenant.TenantId = QikLog.Api.Auth.Testing.TestTenants.Primary;
            var keys = scope.ServiceProvider.GetRequiredService<IApiKeyService>();
            var created = await keys.CreateAsync("revokeme", CancellationToken.None);
            plaintext = created.Plaintext;
            keyId = created.Id;
            (await keys.RevokeAsync(keyId, CancellationToken.None)).ShouldBeTrue();
        }

        var client = factory.CreateClient();
        ApiTestAuth.SetApiKey(client, plaintext);
        var response = await PostAsync(client, "revoked");
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task History_does_not_apply_ingest_rate_limit()
    {
        await using var factory = new LowRateLimitWebApplicationFactory();
        var apiKey = await ApiTestData.CreateApiKeyForPrimaryTenantAsync(factory.Services);
        var client = factory.CreateClient();
        ApiTestAuth.SetApiKey(client, apiKey);

        (await PostAsync(client, "h1")).StatusCode.ShouldBe(HttpStatusCode.Accepted);
        (await PostAsync(client, "h2")).StatusCode.ShouldBe(HttpStatusCode.Accepted);

        for (var i = 0; i < 5; i++)
        {
            var history = await client.GetAsync("/v1/sources/rate/logs?limit=10");
            history.StatusCode.ShouldBe(HttpStatusCode.OK);
        }
    }

    private static Task<HttpResponseMessage> PostAsync(HttpClient client, string message) =>
        client.PostAsync(
            "/v1/logs",
            new StringContent(
                $$"""{"source":"rate","message":"{{message}}"}""",
                Encoding.UTF8,
                "application/json"));
}
