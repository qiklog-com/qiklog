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
        body.ShouldContain("qiklog");
    }

    [SmokeFact]
    public async Task Brand_stylesheet_is_served()
    {
        using var response = await SmokeClient.GetAsync($"{Web}/brand/brand.css");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("--ql-accent-teal");
    }

    /// <summary>
    /// The tail page builds a SignalR connection to an API that enforces auth. A rejected
    /// hub handshake must degrade to an on-page status, never a 500.
    /// </summary>
    [Theory]
    [InlineData("/tail/demo")]
    [InlineData("/tail/does-not-exist")]
    public async Task Tail_page_never_returns_a_server_error(string path)
    {
        if (!SmokeEnvironment.Enabled)
            return;

        using var response = await SmokeClient.GetAsync($"{Web}{path}");

        ((int)response.StatusCode).ShouldBeLessThan(
            500,
            $"{path} returned {(int)response.StatusCode}; hub failures must not crash the render");
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
