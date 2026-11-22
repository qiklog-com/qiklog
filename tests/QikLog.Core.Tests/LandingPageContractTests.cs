using Shouldly;
using Xunit;

namespace QikLog.Core.Tests;

public sealed class LandingPageContractTests
{
    [Fact]
    public void Given_landing_When_read_Then_the_log_link_is_the_hero_and_the_pitch_is_console_log_for_your_server()
    {
        // The five-scene landing was replaced by the log link hero (docs/MARKETING_PLAN.md).
        var index = ReadRepoFile("www/src/pages/index.astro");
        var layout = ReadRepoFile("www/src/layouts/BaseLayout.astro");
        var hero = ReadRepoFile("www/src/components/LinkHero.astro");
        var deck = ReadRepoFile("www/src/lib/link-deck.ts");

        // Given: the page a stranger lands on
        // Then: the headline names the job in the visitor's words, not a product category
        index.ShouldContain("<h1>console.log for your server.</h1>");
        index.ShouldContain("Get a link. Send it a line. Watch it land.");
        index.ShouldContain("QikLog: console.log for your server");
        layout.ShouldContain("QikLog: console.log for your server");
        layout.ShouldContain("Get a link. Send it a line. Watch it land.");
        layout.ShouldNotContain("See what your app is doing, live");

        // Then: the hero is the product, with nothing else above it
        index.ShouldContain("<SiteNav");
        index.ShouldContain("<LinkHero");
        index.ShouldContain("<SiteFooter");
        index.ShouldNotContain("<HeroPlayground");
        index.ShouldNotContain("<ScrollScene");
        index.ShouldNotContain("<StatsRow");
        index.ShouldNotContain("300ms");
        index.ShouldNotContain("278ms");

        // Then: what it is not, the price in one line, and the .NET hook below the fold
        index.ShouldContain("<strong>No dashboard</strong> to configure.");
        index.ShouldContain("<strong>No agent</strong> to install.");
        index.ShouldContain("<strong>No query language</strong> to learn.");
        index.ShouldContain("Free up to 10,000 lines a month. Pro is $9/mo, cancel anytime.");
        index.ShouldContain("Create a free account");
        index.ShouldContain("/signup");
        index.ShouldContain("Serilog sink");
        index.ShouldContain("PUBLIC_APP_URL");
        index.ShouldContain("PUBLIC_API_URL");
        index.ShouldContain("https://api.qiklog.com");

        // Then: one button, and the address it returns is both where you send and where you watch
        hero.ShouldContain("Get a log link");
        hero.ShouldContain("No account. No agent. No dashboard.");
        hero.ShouldContain("fetch(`${api}/l`, { method: 'POST' })");
        hero.ShouldContain("/embed/l/");
        hero.ShouldContain("Send a test line");
        hero.ShouldContain("fetch(link.url, { method: 'POST', body: 'hello from the browser' })");
        hero.ShouldContain("Anyone with this link can read it, so do not log secrets. It lasts 24 hours and holds 500 lines.");

        // Then: accounts are mentioned only after the first line lands
        hero.ShouldContain("data-keep hidden");
        hero.ShouldContain("qiklog-log");
        hero.ShouldContain("for logs that stay private and do not expire.");

        // Then: the samples need no key, no JSON, and no headers
        deck.ShouldContain("curl __URL__ -d \"it broke\"");
        deck.ShouldContain("label: 'curl'");
        deck.ShouldContain("label: 'JavaScript'");
        deck.ShouldContain("label: 'C#'");
        deck.ShouldNotContain("API_KEY");
        deck.ShouldNotContain("Authorization");
        hero.ShouldNotContain("API_KEY");
        hero.ShouldContain("sample.innerHTML = highlight(");

        foreach (var file in new[] { index, hero, deck })
        {
            file.ShouldNotContain("QikLog Pro");
            file.ShouldNotContain("Upgrade");
            file.ShouldNotContain("coming soon");
            file.ShouldNotContain("checkout.stripe.com");
            file.ShouldNotContain("\u2014");
            file.ShouldNotContain("\u2013");
        }
    }

    [Fact]
    public void Given_the_marketing_host_When_configured_Then_log_link_paths_are_proxied_to_the_api()
    {
        // Given: qiklog.com is a static site, and a log link lives at qiklog.com/l/{id}
        var vercel = ReadRepoFile("www/vercel.json");

        // Then: creating, sending and reading all reach the API through the same address
        vercel.ShouldContain("\"source\": \"/l\"");
        vercel.ShouldContain("\"destination\": \"https://api.qiklog.com/l\"");
        vercel.ShouldContain("\"source\": \"/l/:path*\"");
        vercel.ShouldContain("\"destination\": \"https://api.qiklog.com/l/:path*\"");
    }

    [Fact]
    public void Readme_shows_field_clinical_lockup_and_status_badges()
    {
        var readme = ReadRepoFile("README.md");
        readme.ShouldContain("docs/assets/qiklog-lockup.svg");
        readme.ShouldContain("actions/workflows/ci.yml/badge.svg");
        readme.ShouldContain("badge/.NET-9");
        readme.ShouldContain("badge/status-beta");
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
        pricing.ShouldContain("/billing");
        pricing.ShouldContain("/signup");

        var card = ReadRepoFile("www/src/components/PricingCard.astro");
        card.ShouldContain("Free");
        card.ShouldContain("10,000 log lines a month");
        card.ShouldContain("$9");
        card.ShouldContain("500,000 log lines a month");
        card.ShouldContain("Upgrade to Pro");
        card.ShouldContain("Create a free account");
        // Checkout is live (2026-11-21); the old disclaimer must not come back.
        card.ShouldNotContain("Checkout is not live");
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
        var embed = ReadRepoFile("www/src/components/LiveTailEmbed.astro");
        var scene = ReadRepoFile("www/src/components/ScrollScene.astro");
        var panel = ReadRepoFile("src/QikLog.Web/Components/Shared/LiveTailPanel.razor");
        var embedPage = ReadRepoFile("src/QikLog.Web/Components/Pages/EmbedTail.razor");
        var tailPage = ReadRepoFile("src/QikLog.Web/Components/Pages/Tail.razor");
        var tape = ReadRepoFile("tapes/landing-live.tape");

        // When / Then: five-scene structure, hero copy unchanged, scene 3 is real embed
        // Scene 2 and 3 titles removed 2026-11-21: the tape and the live panel carry those scenes.

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
        panel.ShouldContain("BuildHub");
        panel.ShouldContain("X-QikLog-API-Key");
        panel.ShouldContain("AccessTokenProvider");
        panel.ShouldContain("ql-live-status");
        panel.ShouldNotContain("Appearance.Accent");
        panel.ShouldNotContain("FluentBadge");
        var brand = ReadRepoFile("src/QikLog.Web/wwwroot/brand/brand.css");
        brand.ShouldContain(".ql-live-status[data-state=\"connected\"]");
        brand.ShouldContain("var(--ql-accent-rust)");
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
        footer.ShouldContain("justify-content: center");
        footer.ShouldNotContain("text-align: left");
        footer.ShouldNotContain("class=\"footer container\"");
        footer.ShouldNotContain("—");
    }

    [Fact]
    public void Hero_terminal_is_the_visual_anchor()
    {
        // Given: the tail preview component
        var preview = ReadRepoFile("www/src/components/TailPreview.astro");

        // Then: it fills its column and uses brand tokens, not raw hex
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
        preview.ShouldContain(">ERROR<");
        preview.ShouldContain("log-level is-error");
        preview.ShouldContain("log-source");
        preview.ShouldContain("steps(15");
        preview.ShouldContain("role=\"img\"");
        preview.ShouldContain("<TerminalFrame");
        preview.ShouldNotContain("INFO demo");
        preview.ShouldNotContain("hello from curl");
        preview.ShouldNotContain("GET /checkout 200");
        preview.ShouldContain("#scene-1.is-in");
        preview.ShouldContain("prefers-reduced-motion");
        preview.ShouldContain("var(--ql-rust)");
        ReadRepoFile("www/src/styles/log-line.css").ShouldContain("var(--ql-font-mono)");
        ReadRepoFile("www/src/components/TerminalFrame.astro").ShouldContain("var(--ql-ink)");
        ReadRepoFile("www/src/components/TerminalFrame.astro").ShouldContain("var(--ql-paper)");
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
        nav.ShouldContain("btn-outline");
        nav.ShouldContain("Sign in");
        nav.ShouldContain("/login");
        nav.ShouldContain("Pricing");
        nav.ShouldContain("Docs");
        // Sign up is the one filled button in the nav (2026-11-21).
        nav.ShouldContain("Sign up");
        nav.ShouldContain("/signup");
        nav.ShouldContain("btn-primary");
    }

    [Fact]
    public void Given_landing_When_inspected_Then_snap_slides_chevron_and_one_shot_motion()
    {
        // Given: the marketing landing deck
        var scene = ReadRepoFile("www/src/components/ScrollScene.astro");
        var css = ReadRepoFile("www/src/styles/global.css");

        // When / Then: CSS scroll-snap, delayed chevron, one-shot IO, reduced motion
        css.ShouldContain("scroll-snap-type");
        css.ShouldContain("620ms");
        css.ShouldContain("--ql-shadow-pop");
        scene.ShouldContain("scroll-snap-align");
        scene.ShouldContain("620ms");
        scene.ShouldNotContain("—");
    }

    [Fact]
    public void Given_ingest_slide_When_read_Then_tape_types_real_api_curl()
    {
        // Given: scene 2 is the ingest tell
        var tape = ReadRepoFile("www/src/components/TapeTerminal.astro");

        // When / Then: real production curl, tape treatment, caption last
        tape.ShouldContain("https://api.qiklog.com/v1/logs");
        tape.ShouldContain("Content-Type: application/json");
        tape.ShouldContain("Authorization: Bearer");
        tape.ShouldContain("""{"source":"demo","level":"info","message":"hello from curl"}""");
        tape.ShouldContain("define:vars");
        tape.ShouldContain("data-tape-target");
        tape.ShouldContain("28");
        tape.ShouldContain("<TerminalFrame");
        tape.ShouldContain("variant=\"tape\"");
        ReadRepoFile("www/src/components/TerminalFrame.astro").ShouldContain("REC");
        tape.ShouldNotContain("nuget install");
        tape.ShouldNotContain("/var/log");
        tape.ShouldNotContain("qiklog.io");
        tape.ShouldNotContain("—");
    }

    [Fact]
    public void Given_live_slide_When_read_Then_embed_enters_from_right_with_realistic_placeholder()
    {
        // Given: scene 3 is the real Blazor embed
        var embed = ReadRepoFile("www/src/components/LiveTailEmbed.astro");
        var smoke = ReadRepoFile("tests/QikLog.Smoke.Tests/WebSmokeTests.cs");

        // When / Then: lazy iframe to demo; placeholder lines are HTTP logs, not file tails
        embed.ShouldContain("/embed/tail/");
        embed.ShouldContain("loading=\"lazy\"");
        embed.ShouldContain("200 GET /api/orders 24.31ms");
        embed.ShouldContain("401 POST /api/checkout");
        embed.ShouldContain("Same panel as");
        // No window chrome around the real app panel (2026-11-21).
        embed.ShouldNotContain("<TerminalFrame");
        embed.ShouldNotContain("demo · tail");
        embed.ShouldContain("log-level is-info");
        embed.ShouldContain("log-level is-warn");
        embed.ShouldContain("log-source");
        embed.ShouldNotContain("live-kicker");
        embed.ShouldNotContain("Real stream from source demo");
        embed.ShouldNotContain("live-pill");
        embed.ShouldNotContain("/var/log");
        embed.ShouldNotContain("hello from curl");
        smoke.ShouldContain("Given_demo_ingest_When_history_read_Then_embed_source_receives_line");
        smoke.ShouldContain("/embed/tail/demo");
    }

    [Fact]
    public void Given_site_nav_When_read_Then_it_is_a_sticky_glass_bar()
    {
        // Given: the shared site navigation
        var nav = ReadRepoFile("www/src/components/SiteNav.astro");

        // Then: it stays a 60px sticky glass bar
        nav.ShouldContain("backdrop-filter");
        nav.ShouldContain("height: 60px");
    }

    [Fact]
    public void Given_terminal_chrome_When_read_Then_shared_frame_and_level_tokens()
    {
        // Given: landing terminals share one ink panel
        var frame = ReadRepoFile("www/src/components/TerminalFrame.astro");
        var css = ReadRepoFile("www/src/styles/global.css");
        var levels = ReadRepoFile("www/src/styles/log-line.css");
        var layout = ReadRepoFile("www/src/layouts/BaseLayout.astro");

        // When / Then: chrome, panel shadow, severity fills that stay calm on info/debug
        frame.ShouldContain("variant?: 'plain' | 'tape'");
        frame.ShouldContain("class=\"dot\"");
        frame.ShouldContain("class=\"title\"");
        frame.ShouldContain("var(--ql-shadow-panel)");
        frame.ShouldContain("background: var(--ql-ink)");
        frame.ShouldContain("color: var(--ql-paper)");
        frame.ShouldNotContain("#FF");
        frame.ShouldNotContain("#0f0");
        css.ShouldContain("--ql-shadow-panel:");
        layout.ShouldContain("log-line.css");
        levels.ShouldContain("--ql-level-error-bg");
        levels.ShouldContain("--ql-level-warn-bg");
        levels.ShouldContain("--ql-level-info-fg");
        levels.ShouldContain("--ql-level-debug-fg");
        levels.ShouldContain("#e07068");
        levels.ShouldContain("#c9a227");
        levels.ShouldContain("#7eb8e8");
        levels.ShouldContain("#8a9aab");
        levels.ShouldContain(".log-level.is-error");
        levels.ShouldContain(".log-level.is-warn");
        levels.ShouldContain(".log-level.is-info");
        levels.ShouldContain("background: transparent");
        levels.ShouldNotContain("message.match");
        levels.ShouldNotContain("new RegExp");
        frame.ShouldNotContain("—");
        levels.ShouldNotContain("—");
    }

    [Fact]
    public void Given_hero_playground_When_read_Then_data_driven_deck_proxy_and_gated_stats()
    {
        // Given: ntfy-style hero send + code deck
        var playground = ReadRepoFile("www/src/components/HeroPlayground.astro");
        var deck = ReadRepoFile("www/src/lib/code-deck.ts");
        var message = ReadRepoFile("www/src/lib/demo-message.ts");
        var stats = ReadRepoFile("www/src/lib/hero-stats.ts");
        var proxy = ReadRepoFile("www/api/demo-send.js");
        var panel = ReadRepoFile("src/QikLog.Web/Components/Shared/LiveTailPanel.razor");

        // When / Then
        playground.ShouldContain("Send");
        playground.ShouldNotContain("Simulate");
        playground.ShouldContain("/api/demo-send");
        playground.ShouldContain("could not reach the demo source");
        playground.ShouldContain("2000");
        playground.ShouldContain("3000");
        playground.ShouldContain("arrived in");
        playground.ShouldContain("prefers-reduced-motion");
        playground.ShouldContain("demo · tail");
        deck.ShouldContain("label: 'curl'");
        deck.ShouldContain("label: 'C#'");
        deck.ShouldContain("label: 'JavaScript'");
        deck.ShouldContain("https://api.qiklog.com/v1/logs");
        deck.ShouldContain("$QIKLOG_API_KEY");
        deck.ShouldContain("YOUR_API_KEY");
        deck.ShouldContain("Already on your machine.");
        deck.ShouldContain("No install. Plain HttpClient.");
        deck.ShouldContain("No install. Plain fetch.");
        deck.ShouldContain("__MSG__");
        message.ShouldContain("MAX_LENGTH = 120");
        message.ShouldContain("escapeJson");
        message.ShouldContain("escapeCSharp");
        message.ShouldContain("escapeJavaScript");
        proxy.ShouldContain("QIKLOG_DEMO_API_KEY");
        proxy.ShouldContain("source: 'demo'");
        proxy.ShouldNotContain("ql_");
        stats.ShouldContain("showStreamCounter: false");
        stats.ShouldContain("278ms");
        stats.ShouldContain("median, terminal to browser, measured");
        panel.ShouldContain("qiklogEmbedNotify");
        panel.ShouldContain("(object)batch");
        panel.ShouldContain("NotifyEmbed");
        var embed = ReadRepoFile("src/QikLog.Web/Components/Pages/EmbedTail.razor");
        embed.ShouldContain("NotifyEmbed=\"true\"");
        var home = ReadRepoFile("src/QikLog.Web/Components/Pages/Home.razor");
        home.ShouldNotContain("NotifyEmbed");
        playground.ShouldNotContain("—");
    }

    [Fact]
    public void Home_page_shows_curl_and_demo_tail()
    {
        var home = ReadRepoFile("src/QikLog.Web/Components/Pages/Home.razor");

        home.ShouldContain("Try it now");
        home.ShouldContain("This tail is just yours. It disappears when you leave.");
        home.ShouldContain("Create a free account to get your own key");
        home.ShouldContain("href=\"/signup\"");
        home.ShouldContain("UseDemoCredential");
        home.ShouldNotContain("HubApiKey");
        home.ShouldNotContain("Treat anything you send as public");
        home.ShouldNotContain("/tail/demo");
        home.ShouldNotContain("curl -X POST http://localhost:5080");
        home.ShouldNotContain("—");

        var samples = ReadRepoFile("src/QikLog.Web/Demo/DemoCodeSamples.cs");
        samples.ShouldContain("$QIKLOG_API_KEY");
        samples.ShouldContain("YOUR_API_KEY");
        samples.ShouldContain("__MSG__");
        samples.ShouldNotContain("HubApiKey");

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
        css.ShouldContain("--ql-shadow");
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

    [Fact]
    public void Given_ingest_docs_When_read_Then_hosted_curl_and_auth_headers_are_documented()
    {
        var ingest = ReadRepoFile("www/src/content/docs/ingest-api.md");
        var keys = ReadRepoFile("www/src/content/docs/api-keys.md");
        var start = ReadRepoFile("www/src/content/docs/getting-started.md");

        ingest.ShouldContain("https://api.qiklog.com/v1/logs");
        ingest.ShouldContain("Authorization: Bearer $QIKLOG_API_KEY");
        ingest.ShouldContain("X-QikLog-API-Key");
        ingest.ShouldContain("X-Api-Key");
        ingest.ShouldContain("202 Accepted");
        ingest.ShouldContain("401 Unauthorized");
        ingest.ShouldContain("""{"source":"demo","level":"info","message":"hello from curl"}""");
        ingest.ShouldNotContain("—");

        keys.ShouldContain("X-QikLog-API-Key");
        keys.ShouldContain("https://api.qiklog.com/v1/logs");
        keys.ShouldContain("Authorization: Bearer");

        start.ShouldContain("https://api.qiklog.com/v1/logs");
        start.ShouldContain("Authorization: Bearer $QIKLOG_API_KEY");
    }

    [Fact]
    public void Given_code_samples_When_rendered_Then_they_are_highlighted_and_escaped()
    {
        // Given: the hero's curl / C# / JavaScript sample includes the visitor's own message
        var hl = ReadRepoFile("www/src/lib/highlight.ts");
        var hero = ReadRepoFile("www/src/components/HeroPlayground.astro");

        // Then: the sample goes through the highlighter, which escapes every token
        hero.ShouldContain("sample.innerHTML = highlight(");
        hero.ShouldNotContain("sample.innerHTML = tab.template");
        hl.ShouldContain("escapeHtml");
        hl.ShouldContain("&lt;");
        foreach (var cls in new[] { "tok-cmd", "tok-flag", "tok-url", "tok-str", "tok-key", "tok-kw" })
            hero.ShouldContain(cls);

        // And: the live result panel has no fake window chrome
        ReadRepoFile("www/src/components/LiveTailEmbed.astro").ShouldNotContain("<TerminalFrame");
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
