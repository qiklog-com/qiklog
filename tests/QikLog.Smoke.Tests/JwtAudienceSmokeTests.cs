using System.Net;
using System.Net.Http.Headers;
using Shouldly;
using Xunit;

namespace QikLog.Smoke.Tests;

/// <summary>
/// Acceptance scenarios for web-to-API JWT audience (PR #4) and the Manage path
/// that also needs tenant org claims on the access token (PR #5).
/// Follows docs/TESTING.md: Given/When/Then, fail before fix, pass after.
/// </summary>
[Trait("Category", "Smoke")]
public sealed class JwtAudienceSmokeTests
{
    /// <summary>
    /// COS: signed-in user can call an authenticated API endpoint and get 200.
    /// Also guards Auth Enabled=false (token ignored → 401) and opaque Bearer
    /// tokens (OidcSmokeFact refuses non-JWT). Tenant claim mismatch → 403.
    /// </summary>
    [OidcSmokeFact]
    public async Task Given_signed_in_user_When_calling_manage_sources_Then_returns_200()
    {
        // Given: a user has completed OIDC sign-in; access token is a JWT with
        //        project audience and resourceowner org claim
        // When: they GET /v1/sources (Manage list sources)
        // Then: 200, not 401 (audience/auth off) or 403 (tenant not found)
        using var response = await SendBearerAsync(HttpMethod.Get, "/v1/sources");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.StatusCode.ShouldNotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.ShouldNotBe(HttpStatusCode.Forbidden);
    }

    [OidcSmokeFact]
    public async Task Given_signed_in_user_When_listing_api_keys_Then_returns_200()
    {
        // Given: same signed-in JWT as Manage
        // When: they GET /v1/keys
        // Then: 200 (JWT management path is live end to end)
        using var response = await SendBearerAsync(HttpMethod.Get, "/v1/keys");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [SmokeFact]
    public async Task Given_api_jwt_config_When_token_audience_is_wrong_Then_returns_401()
    {
        // Given: the API validates a single Zitadel project audience
        // When: a caller sends a JWT-shaped token whose aud is the old "qiklog-api" string
        // Then: 401, not a wildcard accept
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{SmokeEnvironment.ApiUrl}/v1/sources");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            "eyJhbGciOiJub25lIn0.eyJhdWQiOiJxaWtsb2ctYXBpIiwic3ViIjoidGVzdCJ9.");
        using var response = await new HttpClient { Timeout = TimeSpan.FromSeconds(30) }.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [SmokeFact]
    public async Task Given_opaque_bearer_token_When_calling_sources_Then_returns_401()
    {
        // Given: Zitadel Auth Token Type was opaque Bearer (not JWT)
        // When: that opaque string is sent as Authorization Bearer
        // Then: JwtBearer rejects it (401). Manage cannot work until token type is JWT.
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{SmokeEnvironment.ApiUrl}/v1/sources");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            "opaque-zitadel-access-token-not-a-jwt");
        using var response = await new HttpClient { Timeout = TimeSpan.FromSeconds(30) }.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [SmokeFact]
    public async Task Given_ingest_requires_api_key_When_posting_without_key_Then_returns_401()
    {
        // Given: QikLog__Ingest__RequireApiKey is enabled on the live API
        // When: an anonymous client posts to /v1/logs
        // Then: ingest still returns 401 (JWT audience work must not open this path)
        using var response = await SmokeClient.PostJsonAsync(
            $"{SmokeEnvironment.ApiUrl}/v1/logs",
            """{"source":"smoke","message":"jwt-audience-guard","level":"info"}""");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static async Task<HttpResponseMessage> SendBearerAsync(HttpMethod method, string path)
    {
        using var request = new HttpRequestMessage(method, $"{SmokeEnvironment.ApiUrl}{path}");
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", SmokeEnvironment.AccessToken);
        return await new HttpClient { Timeout = TimeSpan.FromSeconds(30) }.SendAsync(request);
    }
}
