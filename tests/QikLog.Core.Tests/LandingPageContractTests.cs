using Shouldly;
using Xunit;

namespace QikLog.Core.Tests;

public sealed class LandingPageContractTests
{
    [Fact]
    public void Landing_page_leads_with_visitor_outcome_not_tool_speed()
    {
        var index = ReadRepoFile("www/src/pages/index.astro");
        var layout = ReadRepoFile("www/src/layouts/BaseLayout.astro");

        // Visible H1 + lede: outcome for the visitor's app (Option B).
        index.ShouldContain("See what your app is doing, live.");
        index.ShouldContain("Add a log line from your code. Watch it stream in the browser. No signup.");
        index.ShouldContain("Try it now");
        index.ShouldContain("<Lockup");
        index.ShouldContain("<TailPreview");
        index.ShouldContain("$9/mo, cancel anytime.");
        index.ShouldContain("PUBLIC_APP_URL");
        index.ShouldContain("const tryUrl = `${appUrl}/`");

        // Meta / OG defaults match the same outcome framing (not the old tool-speed pitch).
        index.ShouldContain("description =");
        index.ShouldContain(
            "See what your app is doing, live. Add a log line from your code. Watch it stream in the browser. No signup.");
        layout.ShouldContain(
            "See what your app is doing, live. Add a log line from your code. Watch it stream in the browser. No signup.");
        layout.ShouldNotContain("log tailing that works in seconds");

        index.ShouldNotContain("Log tailing that works in seconds");
        index.ShouldNotContain("Add logs to your app in seconds");
        index.ShouldNotContain("QikLog Pro");
        index.ShouldNotContain("Upgrade");
        index.ShouldNotContain("/signup");
        index.ShouldNotContain("/docs/");
        ReadRepoFile("www/src/components/Lockup.astro").ShouldContain("qiklog-mark.svg");
        index.ShouldNotContain("Get started");
        index.ShouldNotContain("Open app");
        index.ShouldNotContain("coming soon");
        index.ShouldNotContain("checkout.stripe.com");
        index.ShouldNotContain("—");
        index.ShouldNotContain("–");
    }

    [Fact]
    public void Readme_shows_field_clinical_lockup_and_status_badges()
    {
        var readme = ReadRepoFile("README.md");
        readme.ShouldContain("docs/assets/qiklog-lockup.svg");
        readme.ShouldContain("actions/workflows/ci.yml/badge.svg");
        readme.ShouldContain("badge/.NET-9");
        readme.ShouldContain("badge/status-pre--alpha");
        readme.ShouldContain("www.qiklog.com");
        readme.ShouldContain("app.qiklog.com");
        readme.ShouldNotContain("src/QikLog.Web/wwwroot/brand/lockup.svg");
        readme.ShouldNotContain("—");

        var lockup = ReadRepoFile("docs/assets/qiklog-lockup.svg");
        lockup.ShouldContain("prefers-color-scheme: dark");
        lockup.ShouldContain("Bricolage Grotesque");
        lockup.ShouldContain("class=\"ink\"");
        lockup.ShouldContain("class=\"rust\"");
        lockup.ShouldContain("class=\"mark\"");
        lockup.ShouldNotContain("system-ui");
        lockup.ShouldNotContain("<text");
        lockup.ShouldNotContain("—");
    }

    [Fact]
    public void Pricing_lives_on_its_own_page()
    {
        var pricing = ReadRepoFile("www/src/pages/pricing.astro");
        pricing.ShouldContain("<PricingCard");
        pricing.ShouldContain("/manage");

        var card = ReadRepoFile("www/src/components/PricingCard.astro");
        card.ShouldContain("QikLog Pro");
        card.ShouldContain("$9");
        card.ShouldContain("Upgrade");
        card.ShouldContain("Live log tailing for developers. Unlimited sources, real time streaming, API keys.");
        card.ShouldNotContain("checkout.stripe.com");
        card.ShouldNotContain("—");

        var footer = ReadRepoFile("www/src/components/SiteFooter.astro");
        footer.ShouldContain("href=\"/pricing/\"");
        ReadRepoFile("www/src/components/SiteNav.astro").ShouldContain("href=\"/pricing/\"");
    }

    [Fact]
    public void Landing_scroll_scenes_use_real_live_embed()
    {
        // Given: the marketing landing page
        var index = ReadRepoFile("www/src/pages/index.astro");
        var embed = ReadRepoFile("www/src/components/LiveTailEmbed.astro");
        var scene = ReadRepoFile("www/src/components/ScrollScene.astro");
        var panel = ReadRepoFile("src/QikLog.Web/Components/Shared/LiveTailPanel.razor");
        var embedPage = ReadRepoFile("src/QikLog.Web/Components/Pages/EmbedTail.razor");
        var tailPage = ReadRepoFile("src/QikLog.Web/Components/Pages/Tail.razor");
        var tape = ReadRepoFile("tapes/landing-live.tape");

        // When / Then: five-scene structure, hero copy unchanged, scene 3 is real embed
        index.ShouldContain("id=\"scene-1\"");
        index.ShouldContain("id=\"scene-2\"");
        index.ShouldContain("id=\"scene-3\"");
        index.ShouldContain("id=\"scene-4\"");
        index.ShouldContain("id=\"scene-5\"");
        index.ShouldContain("One POST. Any language.");
        index.ShouldContain("It's already streaming.");
        index.ShouldContain("No dashboard to configure.");
        index.ShouldContain("<LiveTailEmbed");
        index.ShouldContain("See what your app is doing, live.");
        index.ShouldContain("Add a log line from your code. Watch it stream in the browser. No signup.");
        index.ShouldContain("prefers-reduced-motion");
        index.ShouldContain("IntersectionObserver");

        embed.ShouldContain("/embed/tail/");
        embed.ShouldContain("loading=\"lazy\"");
        embed.ShouldContain("LIVE");
        embed.ShouldNotContain("JWT expired 401");
        embed.ShouldNotContain("hello from curl");

        scene.ShouldContain("data-scene");
        scene.ShouldContain("prefers-reduced-motion");

        // Same LiveTailPanel powers full page and embed (no second SignalR client)
        panel.ShouldContain("Subscribe");
        panel.ShouldContain("LogReceived");
        panel.ShouldContain("Compact");
        tailPage.ShouldContain("<LiveTailPanel");
        embedPage.ShouldContain("<LiveTailPanel");
        embedPage.ShouldContain("Compact=\"true\"");
        embedPage.ShouldContain("@layout QikLog.Web.Components.Layout.EmbedLayout");
        embedPage.ShouldContain("/embed/tail/{Source}");

        tape.ShouldContain("landing-live.gif");
        tape.ShouldContain("/embed/tail/demo");
        tape.ShouldContain("JWT expired 401");
        tape.ShouldNotContain("—");
    }

    [Fact]
    public void Embed_route_sets_frame_ancestors_for_www()
    {
        // Given / When
        var program = ReadRepoFile("src/QikLog.Web/Program.cs");

        // Then: Blazor render-mode CSP allows marketing origins; XFO suppressed
        program.ShouldContain("AddInteractiveServerRenderMode");
        program.ShouldContain("ContentSecurityFrameAncestorsPolicy");
        program.ShouldContain("https://www.qiklog.com");
        program.ShouldContain("SuppressXFrameOptionsHeader");
        program.ShouldContain("SameSiteMode.None");
        program.ShouldNotContain("—");
    }

    [Fact]
    public void Footer_links_are_centered()
    {
        // Given: the shared marketing footer
        var footer = ReadRepoFile("www/src/components/SiteFooter.astro");

        // When: alignment is set
        // Then: the note is centered, not left-aligned
        footer.ShouldContain("text-align: center");
        footer.ShouldNotContain("text-align: left");
        footer.ShouldNotContain("—");
    }

    [Fact]
    public void Hero_terminal_is_the_visual_anchor()
    {
        // Given: the landing hero
        var index = ReadRepoFile("www/src/pages/index.astro");
        var preview = ReadRepoFile("www/src/components/TailPreview.astro");

        // When: layout is inspected
        // Then: the demo sits in a stage that takes the larger grid track, copy unchanged
        index.ShouldContain("class=\"stage\"");
        index.ShouldContain("<TailPreview");
        index.ShouldContain("See what your app is doing, live.");
        index.ShouldContain("Add a log line from your code. Watch it stream in the browser. No signup.");
        index.ShouldContain("minmax(28rem, 1.35fr)");
        index.ShouldContain("var(--ql-hairline)");
        index.ShouldNotContain("minmax(16rem, 22rem)");

        preview.ShouldContain("JWT expired 401");
        preview.ShouldContain("width: 100%");
        preview.ShouldNotContain("width: min(100%, 22rem)");
        preview.ShouldNotContain("#2E2A26");
        preview.ShouldNotContain("#B94700");
    }

    [Fact]
    public void Hero_preview_types_a_production_looking_log_line()
    {
        var preview = ReadRepoFile("www/src/components/TailPreview.astro");
        preview.ShouldContain("JWT expired 401");
        preview.ShouldNotContain("hello from curl");
        preview.ShouldNotContain("GET /checkout 200");
        preview.ShouldContain("prefers-reduced-motion");
        preview.ShouldContain("var(--ql-ink)");
        preview.ShouldContain("var(--ql-paper)");
        preview.ShouldContain("var(--ql-rust)");
        preview.ShouldContain("var(--ql-font-mono)");
        preview.ShouldNotContain("#2E2A26");
        preview.ShouldNotContain("#B94700");
        preview.ShouldNotContain("—");
    }

    [Fact]
    public void Try_it_now_in_nav_hits_the_app_root()
    {
        var nav = ReadRepoFile("www/src/components/SiteNav.astro");
        nav.ShouldContain("Try it now");
        nav.ShouldContain("href={tryUrl}");
        nav.ShouldContain("appUrl.replace");
        nav.ShouldNotContain("/signup");
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

        var tail = ReadRepoFile("src/QikLog.Web/Components/Pages/Tail.razor");
        tail.ShouldContain("<LiveTailPanel");
        tail.ShouldContain("/tail/{Source}");
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
