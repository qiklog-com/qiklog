using System.Net;
using Shouldly;
using Xunit;

namespace QikLog.Smoke.Tests;

/// <summary>
/// Live checks against the deployed Blazor dashboard. Run with:
///   QIKLOG_SMOKE=1 dotnet test tests/QikLog.Smoke.Tests
/// </summary>
[Trait("Category", "Smoke")]
public sealed class WebSmokeTests
{
    private static string Web => SmokeEnvironment.WebUrl;

    [SmokeFact]
    public async Task Home_page_renders()
    {
        using var response = await SmokeClient.GetAsync($"{Web}/");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("qiklog", Case.Insensitive);
        body.ShouldContain("Try it now");
    }

    [SmokeFact]
    public async Task Brand_stylesheet_is_served()
    {
        using var response = await SmokeClient.GetAsync($"{Web}/brand/brand.css");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("--ql-accent-teal");
        body.ShouldContain("--ql-accent-rust");
        body.ShouldContain("#B94700");
    }

    /// <summary>
    /// The tail page builds a SignalR connection to an API that enforces auth. A rejected
    /// hub handshake must degrade to an on-page status, never a 500.
    /// </summary>
    [Theory]
    [InlineData("/tail/demo")]
    [InlineData("/tail/does-not-exist")]
    [InlineData("/embed/tail/demo")]
    public async Task Tail_page_never_returns_a_server_error(string path)
    {
        if (!SmokeEnvironment.Enabled)
            return;

        using var response = await SmokeClient.GetAsync($"{Web}{path}");

        ((int)response.StatusCode).ShouldBeLessThan(
            500,
            $"{path} returned {(int)response.StatusCode}; hub failures must not crash the render");
    }

    [SmokeFact]
    public async Task Embed_tail_page_renders_live_panel_chrome()
    {
        using var response = await SmokeClient.GetAsync($"{Web}/embed/tail/demo");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("LIVE", Case.Insensitive);
        body.ShouldContain("demo", Case.Insensitive);
        // Embed layout: no marketing notice banner from MainLayout
        body.ShouldNotContain("Demo environment, sample data only");
    }

    /// <summary>
    /// Proves the embed path shares the authenticated ingest → hub story with /tail/demo:
    /// a key can POST to source demo and history still reads it back (same source the embed
    /// LiveTailPanel Subscribe joins). Full iframe paint is covered by DocGen/E2E when opted in.
    /// </summary>
    [AuthenticatedSmokeFact]
    public async Task Given_demo_ingest_When_history_read_Then_embed_source_receives_line()
    {
        // Given: authenticated API + shared demo source used by /embed/tail/demo
        var key = SmokeEnvironment.ApiKey!;
        var marker = $"embed-live {Guid.NewGuid():N}";
        var api = SmokeEnvironment.ApiUrl;

        using var ingest = await SmokeClient.PostJsonAsync(
            $"{api}/v1/logs",
            $$"""{"source":"demo","level":"info","message":"{{marker}}"}""",
            key);
        ingest.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        // When: history for demo is read (same source LiveTailPanel Subscribe joins)
        string body = "";
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            using var history = await SmokeClient.GetAsync($"{api}/v1/sources/demo/logs?limit=50", key);
            history.StatusCode.ShouldBe(HttpStatusCode.OK);
            body = await history.Content.ReadAsStringAsync();
            if (body.Contains(marker, StringComparison.Ordinal))
                break;
            await Task.Delay(TimeSpan.FromMilliseconds(400 * attempt));
        }

        // Then: the line is in the demo source the embed watches
        body.ShouldContain(marker);

        using var embed = await SmokeClient.GetAsync($"{Web}/embed/tail/demo");
        embed.StatusCode.ShouldBe(HttpStatusCode.OK);
        var embedBody = await embed.Content.ReadAsStringAsync();
        embedBody.ShouldContain("LIVE", Case.Insensitive);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/login")]
    [InlineData("/manage")]
    [InlineData("/billing")]
    public async Task Core_pages_never_return_a_server_error(string path)
    {
        if (!SmokeEnvironment.Enabled)
            return;

        using var response = await SmokeClient.GetAsync($"{Web}{path}");

        ((int)response.StatusCode).ShouldBeLessThan(500, $"{path} returned {(int)response.StatusCode}");
    }

    /// <summary>
    /// UseExceptionHandler("/Error") needs a real page behind it; otherwise the handler
    /// 404s and ASP.NET throws a second exception on top of the original.
    /// </summary>
    [SmokeFact]
    public async Task Error_page_is_reachable()
    {
        using var response = await SmokeClient.GetAsync($"{Web}/Error");

        response.StatusCode.ShouldBe(
            HttpStatusCode.OK,
            "the configured exception handler path must resolve to a page");
    }

    /// <summary>
    /// Regression guard for X-Forwarded-Proto handling: behind Railway's TLS-terminating
    /// proxy the container sees plain HTTP, so an unforwarded scheme yields an http://
    /// redirect_uri that the identity provider rejects.
    /// </summary>
    [SmokeFact]
    public async Task Challenge_redirects_to_identity_provider_with_https_callback()
    {
        using var response = await SmokeClient.GetAsync($"{Web}/challenge");

        response.StatusCode.ShouldBe(HttpStatusCode.Found);

        var location = response.Headers.Location?.ToString();
        location.ShouldNotBeNullOrWhiteSpace();

        var query = System.Web.HttpUtility.ParseQueryString(new Uri(location!).Query);
        var redirectUri = query["redirect_uri"];

        redirectUri.ShouldNotBeNullOrWhiteSpace();
        redirectUri!.ShouldStartWith("https://", Case.Sensitive);
        redirectUri.ShouldEndWith("/signin-oidc");
    }
}
