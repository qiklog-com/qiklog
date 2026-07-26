using System.Net;
using Shouldly;
using Xunit;

namespace QikLog.Smoke.Tests;

/// <summary>
/// Live checks against the deployed API. Run with:
///   QIKLOG_SMOKE=1 dotnet test tests/QikLog.Smoke.Tests
/// </summary>
[Trait("Category", "Smoke")]
public sealed class ApiSmokeTests
{
    private static string Api => SmokeEnvironment.ApiUrl;

    [SmokeFact]
    public async Task Healthz_reports_ok_with_postgres_reachable()
    {
        using var response = await SmokeClient.GetAsync($"{Api}/healthz");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("\"status\":\"ok\"");
        body.ShouldContain("\"postgres\":\"ok\"");
    }

    [SmokeFact]
    public async Task Health_endpoint_responds()
    {
        using var response = await SmokeClient.GetAsync($"{Api}/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [SmokeFact]
    public async Task Ingest_without_api_key_is_rejected()
    {
        using var response = await SmokeClient.PostJsonAsync(
            $"{Api}/v1/logs",
            """{"source":"smoke","message":"smoke test","level":"info"}""");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("/v1/keys")]
    [InlineData("/v1/sources")]
    [InlineData("/v1/sources/demo/logs")]
    public async Task Management_and_history_endpoints_require_credentials(string path)
    {
        if (!SmokeEnvironment.Enabled)
            return;

        using var response = await SmokeClient.GetAsync($"{Api}{path}");

        response.StatusCode.ShouldBe(
            HttpStatusCode.Unauthorized,
            $"{path} must not be readable without a tenant credential");
    }

    [SmokeFact]
    public async Task Hub_negotiate_requires_authentication()
    {
        using var response = await SmokeClient.PostJsonAsync($"{Api}/hubs/logs/negotiate?negotiateVersion=1");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// End-to-end proof that a real credential can write and read back. A silent
    /// deserialisation failure in the dashboard's API client made history look empty
    /// even though this path returned 200, so the assertion checks the payload.
    /// </summary>
    [AuthenticatedSmokeFact]
    public async Task Authenticated_ingest_round_trips_through_history()
    {
        var key = SmokeEnvironment.ApiKey;
        var source = "smoke-roundtrip";
        var marker = $"smoke round trip {Guid.NewGuid():N}";

        using var ingest = await SmokeClient.PostJsonAsync(
            $"{Api}/v1/logs",
            $$"""{"source":"{{source}}","level":"info","message":"{{marker}}"}""",
            key);

        ingest.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        using var history = await SmokeClient.GetAsync($"{Api}/v1/sources/{source}/logs?limit=50", key);

        history.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await history.Content.ReadAsStringAsync();
        body.ShouldContain(marker);
    }

    [AuthenticatedSmokeFact]
    public async Task Authenticated_hub_negotiate_succeeds()
    {
        using var response = await SmokeClient.PostJsonAsync(
            $"{Api}/hubs/logs/negotiate?negotiateVersion=1",
            apiKey: SmokeEnvironment.ApiKey);

        response.StatusCode.ShouldBe(
            HttpStatusCode.OK,
            "the dashboard uses this credential to stream live tail");
    }

    [SmokeFact]
    public async Task Dev_key_endpoint_is_not_exposed_in_production()
    {
        using var response = await SmokeClient.PostJsonAsync(
            $"{Api}/v1/dev/keys",
            """{"name":"smoke"}""");

        response.StatusCode.ShouldNotBe(HttpStatusCode.Created);
        response.StatusCode.ShouldNotBe(HttpStatusCode.OK);
    }
}
