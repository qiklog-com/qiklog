using System.Text.RegularExpressions;
using Shouldly;
using Xunit;

namespace QikLog.Core.Tests;

public sealed class StageDesignContractTests
{
    private static readonly string[] StageTokenHex =
    [
        "#141216",
        "#17141A",
        "#2A2520",
        "#F3EFE9",
        "#8F857A",
        "#6E675F",
        "#B94700",
        "#FF8A3D",
        "#6EB1D6",
        "#E8A23D",
        "#FF6161",
        "#FFC9C9",
        "#F2DCB2"
    ];

    [Fact]
    public void Stage_tokens_are_scoped_under_ql_stage_and_not_root()
    {
        var css = ReadRepoFile("src/QikLog.Web/wwwroot/css/stage.css");
        var stageBlock = ExtractRule(css, ".ql-stage");

        foreach (var hex in StageTokenHex)
            stageBlock.ShouldContain(hex);

        var defined = Regex.Matches(stageBlock, "#[0-9A-Fa-f]{6}")
            .Select(m => m.Value.ToUpperInvariant())
            .ToHashSet(StringComparer.Ordinal);
        var expected = StageTokenHex.Select(h => h.ToUpperInvariant()).ToHashSet(StringComparer.Ordinal);
        defined.ShouldBe(expected);

        var rootIndex = css.IndexOf(":root", StringComparison.Ordinal);
        if (rootIndex >= 0)
        {
            var rootBlock = ExtractRuleFrom(css, rootIndex);
            Regex.IsMatch(rootBlock, "--ql-").ShouldBeFalse("stage.css must not redefine tokens under :root");
        }
    }

    [Fact]
    public void Motion_law_one_shot_arrive_and_first_glow_only_pulse_blink_infinite()
    {
        var css = ReadRepoFile("src/QikLog.Web/wwwroot/css/stage.css");
        var decls = Regex.Matches(css, @"animation\s*:\s*([^;]+);");
        decls.Count.ShouldBeGreaterThan(0);

        var infiniteNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match decl in decls)
        {
            IsInsideReducedMotionNoPreference(css, decl.Index)
                .ShouldBeTrue($"animation at index {decl.Index} must sit inside prefers-reduced-motion: no-preference");

            var tokens = decl.Groups[1].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            tokens.Length.ShouldBeGreaterThan(0);
            var name = tokens[0];
            if (decl.Groups[1].Value.Contains("infinite", StringComparison.Ordinal))
                infiniteNames.Add(name);

            if (name is "ql-stage-arrive" or "ql-stage-first-glow")
                tokens[^1].ShouldBe("1", name);
        }

        infiniteNames.Count.ShouldBe(2);
        infiniteNames.ShouldContain("ql-stage-pulse");
        infiniteNames.ShouldContain("ql-stage-blink");
    }

    [Fact]
    public void Hover_law_only_transform_background_box_shadow_transition()
    {
        var css = ReadRepoFile("src/QikLog.Web/wwwroot/css/stage.css");
        var allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            "transform",
            "background",
            "box-shadow",
            "transition"
        };
        var banned = new[] { "height", "padding", "margin", "font-size" };

        foreach (Match hover in Regex.Matches(css, @"[^{}]+\:hover[^{]*\{([^}]+)\}"))
        {
            foreach (var statement in hover.Groups[1].Value.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var prop = statement.Split(':')[0].Trim();
                if (prop.Length == 0)
                    continue;

                allowed.ShouldContain(prop);
                banned.ShouldNotContain(prop);
            }
        }
    }

    [Fact]
    public void Stage_law_viewport_row_transition_hover_outside_rm_first_glow_no_border()
    {
        var css = ReadRepoFile("src/QikLog.Web/wwwroot/css/stage.css");

        ExtractRule(css, ".ql-stage-viewport").ShouldContain("user-select: none");

        var rowRule = ExtractRule(css, ".ql-stage-row");
        rowRule.ShouldContain("transition: transform 140ms ease, background 140ms ease");

        var rm = css.IndexOf("@media (prefers-reduced-motion: no-preference)", StringComparison.Ordinal);
        rm.ShouldBeGreaterThan(0);
        var beforeRm = css[..rm];
        beforeRm.ShouldContain(".ql-stage-row:hover");
        beforeRm.ShouldContain("background: var(--ql-panel)");

        var glow = ExtractRule(css, "@keyframes ql-stage-first-glow");
        glow.ShouldNotContain("border-color");
    }

    [Fact]
    public void Truth_in_docs_package_id_and_WriteTo_QikLog()
    {
        var csproj = ReadRepoFile("src/QikLog.Serilog/QikLog.Serilog.csproj");
        var packageId = Regex.Match(csproj, @"<PackageId>([^<]+)</PackageId>").Groups[1].Value;
        packageId.ShouldNotBeNullOrWhiteSpace();

        var panel = ReadRepoFile("src/QikLog.Web/Components/Shared/LiveTailPanel.razor");
        panel.ShouldContain(packageId);
        panel.ShouldContain("WriteTo.QikLog(");

        var ext = ReadRepoFile("src/QikLog.Serilog/QikLogLoggerConfigurationExtensions.cs");
        ext.ShouldContain("public static LoggerConfiguration QikLog(");
        ext.ShouldContain("this LoggerSinkConfiguration");
    }

    [Fact]
    public void Character_gate_bans_typographic_glyphs()
    {
        var files = new[]
        {
            "src/QikLog.Web/Components/Shared/LiveTailPanel.razor",
            "src/QikLog.Web/wwwroot/css/stage.css",
            "tests/QikLog.Web.Tests/StageBufferTests.cs",
            "tests/QikLog.Web.Tests/StagePresentationTests.cs",
            "tests/QikLog.Web.Tests/LiveTailPanelTests.cs",
            "tests/QikLog.Core.Tests/StageDesignContractTests.cs"
        };

        var banned = new (char Glyph, string Name)[]
        {
            ('\u2014', "em dash"),
            ('\u2013', "en dash"),
            ('\u2018', "smart single quote"),
            ('\u2019', "smart single quote"),
            ('\u201C', "smart double quote"),
            ('\u201D', "smart double quote"),
            ('\u00B7', "middle dot"),
            ('\u2026', "typographic ellipsis")
        };

        var hits = new List<string>();
        foreach (var relative in files)
        {
            var text = ReadRepoFile(relative);
            var lines = text.Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                foreach (var (glyph, name) in banned)
                {
                    var col = lines[i].IndexOf(glyph);
                    if (col >= 0)
                        hits.Add($"{relative}:{i + 1}:{col + 1} {name}");
                }
            }
        }

        hits.ShouldBeEmpty(string.Join("; ", hits));
    }

    private static bool IsInsideReducedMotionNoPreference(string css, int index)
    {
        const string needle = "@media (prefers-reduced-motion: no-preference)";
        var start = css.LastIndexOf(needle, index, StringComparison.Ordinal);
        if (start < 0)
            return false;

        var open = css.IndexOf('{', start);
        if (open < 0 || open > index)
            return false;

        return index < MatchingClose(css, open);
    }

    private static string ExtractRule(string css, string selector)
    {
        var needle = selector + " {";
        var start = css.IndexOf(needle, StringComparison.Ordinal);
        start.ShouldBeGreaterThanOrEqualTo(0, $"missing rule {selector}");
        return ExtractRuleFrom(css, start);
    }

    private static string ExtractRuleFrom(string css, int start)
    {
        var open = css.IndexOf('{', start);
        open.ShouldBeGreaterThanOrEqualTo(0);
        var close = MatchingClose(css, open);
        return css[open..(close + 1)];
    }

    private static int MatchingClose(string css, int open)
    {
        var depth = 0;
        for (var i = open; i < css.Length; i++)
        {
            if (css[i] == '{')
                depth++;
            else if (css[i] == '}')
            {
                depth--;
                if (depth == 0)
                    return i;
            }
        }

        throw new InvalidOperationException("unbalanced braces");
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
}
