using Microsoft.Playwright;
using Shouldly;
using Xunit;

namespace QikLog.DocGen.Tests;

/// <summary>
/// Captures dashboard screenshots for www docs. Run with stack up:
///   make up-d && QIKLOG_E2E=1 dotnet test tests/QikLog.DocGen.Tests
/// 🐾 Garfield — doc freshness patrol
/// </summary>
[Trait("Category", "E2E")]
public sealed class DocCaptureTests
{
    private static readonly bool RunE2E =
        string.Equals(Environment.GetEnvironmentVariable("QIKLOG_E2E"), "1", StringComparison.Ordinal);

    private static string WebBaseUrl =>
        Environment.GetEnvironmentVariable("QIKLOG_WEB_URL") ?? "http://localhost:5081";

    private static string ScreenshotDir
    {
        get
        {
            var root = Environment.GetEnvironmentVariable("QIKLOG_SCREENSHOT_DIR");
            if (!string.IsNullOrWhiteSpace(root))
                return root;

            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "QikLog.sln")))
                dir = dir.Parent;

            return Path.Combine(
                dir?.FullName ?? AppContext.BaseDirectory,
                "www",
                "public",
                "docs",
                "screenshots");
        }
    }

    [Fact]
    public async Task Capture_dashboard_screenshots()
    {
        if (!RunE2E)
            return;

        Directory.CreateDirectory(ScreenshotDir);

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 800 },
            ColorScheme = ColorScheme.Dark
        });

        await page.GotoAsync($"{WebBaseUrl.TrimEnd('/')}/", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(ScreenshotDir, "home.png"),
            FullPage = true
        });

        await page.GotoAsync($"{WebBaseUrl.TrimEnd('/')}/manage", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForTimeoutAsync(1500);
        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(ScreenshotDir, "manage.png"),
            FullPage = true
        });

        await page.GotoAsync($"{WebBaseUrl.TrimEnd('/')}/tail/demo", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForTimeoutAsync(2000);
        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(ScreenshotDir, "tail-demo.png"),
            FullPage = true
        });

        Directory.GetFiles(ScreenshotDir, "*.png").Length.ShouldBeGreaterThanOrEqualTo(3);
    }
}
