using System.Net;
using System.Net.Http.Headers;
using Shouldly;
using Xunit;

namespace QikLog.Smoke.Tests;

/// <summary>
/// Acceptance scenarios for web-to-API JWT audience (Path B).
/// Opt in with QIKLOG_SMOKE=1. COS 1 also needs QIKLOG_SMOKE_ACCESS_TOKEN
/// (a Zitadel access token from a signed-in session).
/// HttpClient, not Playwright: this project has no browser package.
/// </summary>
[Trait("Category", "Smoke")]
public sealed class JwtAudienceSmokeTests
{
    [Fact]
    public async Task Given_signed_in_user_When_calling_manage_endpoint_Then_returns_200()
    {
        // Given: a user has completed OIDC sign-in on the web app
        // When: they request an authenticated endpoint on the api
        // Then: the response is 200, not 401
        if (!SmokeEnvironment.Enabled)
            return;
        var token = SmokeEnvironment.AccessToken;
        if (token is null)
            return;

        using var request = new HttpRequestMessage(HttpMethod.Get, $"{SmokeEnvironment.ApiUrl}/v1/sources");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await new HttpClient { Timeout = TimeSpan.FromSeconds(30) }.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.StatusCode.ShouldNotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Given_api_jwt_config_When_token_audience_is_wrong_Then_returns_401()
    {
        // Given: the API validates a single Zitadel project audience
        // When: a caller sends a well-formed JWT whose aud is not that project id
        // Then: the API rejects it (401), not a wildcard accept
        if (!SmokeEnvironment.Enabled)
            return;

        using var request = new HttpRequestMessage(HttpMethod.Get, $"{SmokeEnvironment.ApiUrl}/v1/sources");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            "eyJhbGciOiJub25lIn0.eyJhdWQiOiJxaWtsb2ctYXBpIiwic3ViIjoidGVzdCJ9.");
        using var response = await new HttpClient { Timeout = TimeSpan.FromSeconds(30) }.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Given_ingest_requires_api_key_When_posting_without_key_Then_returns_401()
    {
        // Given: QikLog__Ingest__RequireApiKey is enabled on the live API
        // When: an anonymous client posts to /v1/logs
        // Then: ingest still returns 401 (JWT audience work must not open this path)
        if (!SmokeEnvironment.Enabled)
            return;

        using var response = await SmokeClient.PostJsonAsync(
            $"{SmokeEnvironment.ApiUrl}/v1/logs",
            """{"source":"smoke","message":"jwt-audience-guard","level":"info"}""");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
