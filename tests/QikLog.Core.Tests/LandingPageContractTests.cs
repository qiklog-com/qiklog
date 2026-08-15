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
        index.ShouldContain("QikLog Pro");
        index.ShouldContain("$9");
        index.ShouldContain("Upgrade");
        index.ShouldContain("/manage");
        index.ShouldContain("Live log tailing for developers. Unlimited sources, real time streaming, API keys.");
        ReadRepoFile("www/src/components/Lockup.astro").ShouldContain("qiklog-mark.svg");
        index.ShouldNotContain("Get started");
        index.ShouldNotContain("Open app");
        index.ShouldNotContain("coming soon");
        index.ShouldNotContain("checkout.stripe.com");
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

    [Fact]
    public void Head_wires_brand_icons_and_og_image()
    {
        var head = ReadRepoFile("www/src/layouts/BaseLayout.astro");
        head.ShouldContain("favicon.ico");
        head.ShouldContain("favicon.svg");
        head.ShouldContain("apple-touch-icon.png");
        head.ShouldContain("site.webmanifest");
        head.ShouldContain("og-image.png");
        head.ShouldContain("og:image:width");
        head.ShouldContain("1200");
        head.ShouldContain("630");

        var app = ReadRepoFile("src/QikLog.Web/Components/App.razor");
        app.ShouldContain("favicon.ico");
        app.ShouldContain("apple-touch-icon.png");
        app.ShouldContain("manifest.webmanifest");
    }

    [Fact]
    public void Brand_kit_rasters_match_spec_sizes()
    {
        AssertPngSize("www/public/apple-touch-icon.png", 180, 180);
        AssertPngSize("www/public/icon-192.png", 192, 192);
        AssertPngSize("www/public/icon-512.png", 512, 512);
        AssertPngSize("www/public/og-image.png", 1200, 630);
        AssertPngSize("src/QikLog.Web/wwwroot/apple-touch-icon.png", 180, 180);
        AssertPngSize("src/QikLog.Web/wwwroot/icon-192.png", 192, 192);
        AssertPngSize("src/QikLog.Web/wwwroot/icon-512.png", 512, 512);
        AssertPngSize("src/QikLog.Web/wwwroot/og-image.png", 1200, 630);

        var ico = ReadRepoBytes("www/public/favicon.ico");
        ico.Length.ShouldBeGreaterThan(100);
        ico[0].ShouldBe((byte)0);
        ico[1].ShouldBe((byte)0);
        ico[2].ShouldBe((byte)1);
        ico[3].ShouldBe((byte)0);
        ico[4].ShouldBe((byte)3);
    }

    private static void AssertPngSize(string relativePath, int width, int height)
    {
        var bytes = ReadRepoBytes(relativePath);
        bytes[0].ShouldBe((byte)0x89);
        var actualWidth = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
        var actualHeight = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
        actualWidth.ShouldBe(width, relativePath);
        actualHeight.ShouldBe(height, relativePath);
    }

    private static DirectoryInfo RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "QikLog.sln")))
            dir = dir.Parent;

        dir.ShouldNotBeNull("could not find repo root from test output directory");
        return dir!;
    }

    private static string ReadRepoFile(string relativePath)
    {
        var path = Path.Combine(RepoRoot().FullName, relativePath);
        File.Exists(path).ShouldBeTrue($"missing {relativePath}");
        return File.ReadAllText(path);
    }

    private static byte[] ReadRepoBytes(string relativePath)
    {
        var path = Path.Combine(RepoRoot().FullName, relativePath);
        File.Exists(path).ShouldBeTrue($"missing {relativePath}");
        return File.ReadAllBytes(path);
    }
}
