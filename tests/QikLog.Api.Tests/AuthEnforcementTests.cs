using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using QikLog.Api.Auth.Testing;
using QikLog.Infrastructure.Auth;
using QikLog.Infrastructure.Tenants;
using Shouldly;
using Xunit;

namespace QikLog.Api.Tests;

public sealed class AuthEnforcementTests : IAsyncLifetime
{
    private readonly QikLogApiWebApplicationFactory _factory = new();
    private HttpClient _client = null!;
    private string _apiKey = "";

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        _apiKey = await ApiTestData.CreateApiKeyForPrimaryTenantAsync(_factory.Services);
    }

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task Management_list_keys_without_token_returns_unauthorized()
    {
        var response = await _client.GetAsync("/v1/keys");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Management_list_keys_with_malformed_jwt_returns_unauthorized()
    {
        var client = _factory.CreateClient();
        ApiTestAuth.SetMalformedJwt(client);
        var response = await client.GetAsync("/v1/keys");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Management_list_keys_with_unknown_tenant_returns_forbidden()
    {
        var client = _factory.CreateClient();
        ApiTestAuth.SetUnknownTenantJwt(client);
        var response = await client.GetAsync("/v1/keys");
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Management_list_keys_with_valid_jwt_returns_ok()
    {
        var client = _factory.CreateClient();
        ApiTestAuth.SetValidJwt(client);
        var response = await client.GetAsync("/v1/keys");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ingest_without_api_key_returns_unauthorized()
    {
        var response = await _client.PostAsync(
            "/v1/logs",
            new StringContent("""{"source":"x","message":"y"}""", Encoding.UTF8, "application/json"));
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Ingest_with_invalid_api_key_returns_forbidden()
    {
        var client = _factory.CreateClient();
        ApiTestAuth.SetApiKey(client, "ql_00000000_invalidsecretpart0000");
        var response = await client.PostAsync(
            "/v1/logs",
            new StringContent("""{"source":"x","message":"y"}""", Encoding.UTF8, "application/json"));
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Ingest_with_valid_api_key_returns_accepted()
    {
        var client = _factory.CreateClient();
        ApiTestAuth.SetApiKey(client, _apiKey);
        var response = await client.PostAsync(
            "/v1/logs",
            new StringContent("""{"source":"auth","message":"ok"}""", Encoding.UTF8, "application/json"));
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task History_without_auth_returns_unauthorized()
    {
        var response = await _client.GetAsync("/v1/sources/demo/logs");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task History_with_jwt_returns_ok()
    {
        var client = _factory.CreateClient();
        ApiTestAuth.SetValidJwt(client);
        var response = await client.GetAsync("/v1/sources/demo/logs");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task History_with_api_key_returns_ok()
    {
        var client = _factory.CreateClient();
        ApiTestAuth.SetApiKey(client, _apiKey);
        var response = await client.GetAsync("/v1/sources/demo/logs");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ingest_with_key_missing_tenant_returns_forbidden()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var tenant = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        tenant.TenantId = null;
        var keys = scope.ServiceProvider.GetRequiredService<IApiKeyService>();
        var created = await keys.CreateAsync("no-tenant", CancellationToken.None);

        var client = _factory.CreateClient();
        ApiTestAuth.SetApiKey(client, created.Plaintext);
        var response = await client.PostAsync(
            "/v1/logs",
            new StringContent("""{"source":"x","message":"y"}""", Encoding.UTF8, "application/json"));
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
