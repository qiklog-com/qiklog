using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using QikLog.Web.Components.Pages;
using QikLog.Web.Demo;
using Shouldly;
using Xunit;

namespace QikLog.Web.Tests;

public sealed class HomeKeyLeakTests : TestContext
{
    public HomeKeyLeakTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddFluentUIComponents();
    }

    [Fact]
    public void Given_configured_keys_When_home_rendered_Then_markup_has_no_key()
    {
        // Given: both credentials are configured with values that must stay on the server
        const string demoKey = "ql_demo_secret_value";
        const string hubKey = "ql_hub_secret_value";
        Services.AddSingleton<Microsoft.Extensions.Options.IOptions<QikLogOptions>>(
            Microsoft.Extensions.Options.Options.Create(new QikLogOptions
            {
                ApiBaseUrl = "",
                HubApiKey = hubKey,
                Demo = new DemoOptions { ApiKey = demoKey }
            }));

        // When: the try-it-now page renders
        var cut = RenderComponent<Home>();

        // Then: neither secret is in the HTML, and the samples use placeholders
        cut.Markup.ShouldNotContain(demoKey);
        cut.Markup.ShouldNotContain(hubKey);
        cut.Markup.ShouldContain("$QIKLOG_API_KEY");
        cut.Markup.ShouldContain("YOUR_API_KEY");
        cut.Find("a[href='/login']").TextContent.ShouldContain("Sign in to get your own key");
    }
}
