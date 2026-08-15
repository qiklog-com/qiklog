using Shouldly;
using Xunit;

namespace QikLog.Core.Tests;

public sealed class LandingPageContractTests
{
    [Fact]
    public void Landing_page_is_the_demo_not_a_pitch()
    {
        var index = ReadRepoFile("www/src/pages/index.astro");

        index.ShouldContain("Try it now");
        index.ShouldContain("Log tailing that works in seconds. No signup.");
        index.ShouldContain("<Lockup");
        ReadRepoFile("www/src/components/Lockup.astro").ShouldContain("qiklog-mark.svg");
        index.ShouldNotContain("Get started");
        index.ShouldNotContain("Open app");
        index.ShouldNotContain("coming soon");
        index.ShouldNotContain("—");
        index.ShouldNotContain("–");
    }

    [Fact]
    public void Home_page_shows_curl_and_demo_tail()
    {
        var home = ReadRepoFile("src/QikLog.Web/Components/Pages/Home.razor");

        home.ShouldContain("Try it now");
        home.ShouldContain("/tail/demo");
        home.ShouldContain("ApiBaseUrl");
        home.ShouldContain("waiting for your first log line");
        home.ShouldNotContain("curl -X POST http://localhost:5080");
        home.ShouldNotContain("—");
    }

    [Fact]
    public void Brand_css_ships_field_clinical_tokens()
    {
        var css = ReadRepoFile("src/QikLog.Web/wwwroot/brand/brand.css");
        css.ShouldContain("#FCFAF5");
        css.ShouldContain("#2E2A26");
        css.ShouldContain("#B94700");
        css.ShouldContain("#D9D3CA");
        css.ShouldContain("--ql-accent-rust");
    }

    [Fact]
    public void Marketing_css_ships_field_clinical_tokens()
    {
        var css = ReadRepoFile("www/src/styles/global.css");
        css.ShouldContain("#fcfaf5");
        css.ShouldContain("#2e2a26");
        css.ShouldContain("#b94700");
        css.ShouldContain("#d9d3ca");
        css.ShouldContain("Bricolage Grotesque");
        css.ShouldContain("Public Sans");
        css.ShouldContain("IBM Plex Mono");
    }

    [Fact]
    public void Mark_svg_is_stroke_only_prompt_frame()
    {
        var mark = ReadRepoFile("docs/assets/qiklog-mark.svg");
        mark.ShouldContain("stroke=\"#B94700\"");
        mark.ShouldContain("stroke-width=\"8\"");
        mark.ShouldContain("M43 41 L55 53");
        mark.ShouldNotContain("fill=\"#B94700\"");
    }

    private static string ReadRepoFile(string relativePath)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "QikLog.sln")))
            dir = dir.Parent;

        dir.ShouldNotBeNull("could not find repo root from test output directory");
        var path = Path.Combine(dir!.FullName, relativePath);
        File.Exists(path).ShouldBeTrue($"missing {relativePath}");
        return File.ReadAllText(path);
    }
}
