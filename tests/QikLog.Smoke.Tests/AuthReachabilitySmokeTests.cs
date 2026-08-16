using System.Net;
using Shouldly;
using Xunit;

namespace QikLog.Smoke.Tests;

/// <summary>
/// Live reachability for Path B auth and custom domains.
/// Catches "Auth Enabled=false" and TLS/cert stalls that unit tests cannot see.
/// </summary>
[Trait("Category", "Smoke")]
public sealed class AuthReachabilitySmokeTests
{
    [SmokeFact]
    public async Task Given_production_api_When_hitting_healthz_Then_returns_200()
    {
        // Given: the API service is deployed
        // When: GET /healthz
        // Then: 200 (platform is up before we blame auth)
        using var response = await SmokeClient.GetAsync($"{SmokeEnvironment.ApiUrl}/healthz");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [SmokeFact]
    public async Task Given_management_routes_When_called_without_credentials_Then_return_401()
    {
        // Given: AuthEnforcement is on and JWT is required for Manage
        // When: anonymous GET /v1/sources and /v1/keys
        // Then: 401 (auth is reachable; not a silent 200 with Auth Enabled=false quirks)
        using var sources = await SmokeClient.GetAsync($"{SmokeEnvironment.ApiUrl}/v1/sources");
        using var keys = await SmokeClient.GetAsync($"{SmokeEnvironment.ApiUrl}/v1/keys");

        sources.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        keys.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [SmokeFact]
    public async Task Given_custom_app_domain_When_requesting_https_Then_tls_succeeds()
    {
        // Given: app.qiklog.com is attached to Railway web with a real cert
        // When: HTTPS GET (default cert validation)
        // Then: 200 (not SSL name mismatch / connection failure)
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        using var response = await client.GetAsync(SmokeEnvironment.AppCustomDomainUrl + "/");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [SmokeFact]
    public async Task Given_custom_api_domain_When_requesting_healthz_Then_tls_succeeds()
    {
        // Given: api.qiklog.com is attached to Railway api with a real cert
        // When: HTTPS GET /healthz
        // Then: 200
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        using var response = await client.GetAsync(SmokeEnvironment.ApiCustomDomainUrl + "/healthz");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [SmokeFact]
    public async Task Given_web_challenge_When_starting_oidc_Then_redirects_to_zitadel()
    {
        // Given: web Auth Enabled is true
        // When: GET /challenge
        // Then: redirect to the Zitadel authority (OIDC is wired, not a dead stub)
        using var response = await SmokeClient.GetAsync($"{SmokeEnvironment.WebUrl}/challenge");

        response.StatusCode.ShouldBe(HttpStatusCode.Found);
        var location = response.Headers.Location?.ToString();
        location.ShouldNotBeNullOrWhiteSpace();
        location!.ShouldContain("zitadel.cloud", Case.Insensitive);
    }
}
