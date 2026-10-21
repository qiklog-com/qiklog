using System.Net;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Shouldly;
using Xunit;

namespace QikLog.Web.Tests;

public sealed class RouteAuthorizationTests
{
    private const string AuthorizeEndpoint = "https://idp.test/oauth/v2/authorize";

    [Theory]
    [InlineData("/manage")]
    [InlineData("/billing")]
    public async Task Given_oidc_enabled_and_anonymous_When_opening_account_page_Then_redirected_to_sign_in(string path)
    {
        // Given: the web app with OIDC on, and a visitor with no session
        await using var factory = new WebFactory(authEnabled: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });

        // When: they open an account page
        using var response = await client.GetAsync(path);

        // Then: challenged to the IdP instead of rendering a page that cannot work
        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().ShouldStartWith(AuthorizeEndpoint);
    }

    [Fact]
    public async Task Given_oidc_enabled_and_anonymous_When_opening_home_Then_page_renders()
    {
        // Given: OIDC on, anonymous visitor
        await using var factory = new WebFactory(authEnabled: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });

        // When: they open the home page
        using var response = await client.GetAsync("/");

        // Then: public pages stay public
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Given_oidc_disabled_When_opening_manage_Then_page_renders_for_local_demo()
    {
        // Given: local demo mode, OIDC off
        await using var factory = new WebFactory(authEnabled: false);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });

        // When: opening /manage
        using var response = await client.GetAsync("/manage");

        // Then: no sign-in wall when there is nothing to sign in to
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private sealed class WebFactory(bool authEnabled) : WebApplicationFactory<Program>
    {
        private readonly string _keysPath = Path.Combine(Path.GetTempPath(), $"qiklog-web-tests-{Guid.NewGuid():N}");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("DataProtection:KeysPath", _keysPath);
            // Unroutable on purpose: pages that prerender API calls fail fast.
            builder.UseSetting("QikLog:ApiBaseUrl", "http://127.0.0.1:9");
            builder.UseSetting("QikLog:Auth:Enabled", authEnabled ? "true" : "false");
            builder.UseSetting("QikLog:Auth:Authority", authEnabled ? "https://idp.test" : "");
            builder.UseSetting("QikLog:Auth:ClientId", "qiklog-web-test");

            if (!authEnabled)
                return;

            builder.ConfigureTestServices(services =>
            {
                // No network: hand the OIDC handler a fixed discovery document.
                services.PostConfigure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
                {
                    var configuration = new OpenIdConnectConfiguration
                    {
                        Issuer = "https://idp.test",
                        AuthorizationEndpoint = AuthorizeEndpoint,
                        TokenEndpoint = "https://idp.test/oauth/v2/token"
                    };
                    options.Configuration = configuration;
                    options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
                });
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (Directory.Exists(_keysPath))
                Directory.Delete(_keysPath, recursive: true);
        }
    }
}
