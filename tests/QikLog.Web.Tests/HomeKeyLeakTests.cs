using Bunit;
using QikLog.Web.Components.Pages;
using Shouldly;
using Xunit;

namespace QikLog.Web.Tests;

public sealed class HomeKeyLeakTests : TestContext
{
    [Fact]
    public void Given_configured_keys_When_home_rendered_Then_markup_has_no_key()
    {
        // Given: both credentials are configured with values that must stay on the server
        const string demoKey = "ql_demo_secret_value";
        const string hubKey = "ql_hub_secret_value";
        var harness = new PlaygroundHarness(demoKey, hubKey);
        harness.Install(this);

        // When: the try-it-now page renders and each code tab is selected
        var cut = RenderComponent<Home>();
        foreach (var tab in new[] { "curl", "csharp", "javascript" })
        {
            cut.Find($"[data-tab={tab}]").Click();

            // Then: neither secret is in the HTML
            cut.Markup.ShouldNotContain(demoKey);
            cut.Markup.ShouldNotContain(hubKey);
        }

        cut.Find("[data-tab=curl]").Click();
        cut.Markup.ShouldContain("$QIKLOG_API_KEY");
        cut.Find("[data-tab=csharp]").Click();
        cut.Markup.ShouldContain("YOUR_API_KEY");
        cut.Find("a[href='/signup']").TextContent.ShouldContain("Create a free account to get your own key");
    }
}
