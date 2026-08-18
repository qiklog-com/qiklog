using Shouldly;
using Xunit;

namespace QikLog.Core.Tests;

public sealed class DemoMessageTests
{
    [Fact]
    public void Given_null_or_blank_When_sanitize_Then_empty()
    {
        // Given: missing or whitespace-only input
        // When: sanitize
        // Then: empty string, never null
        DemoMessage.Sanitize(null).ShouldBe("");
        DemoMessage.Sanitize("   ").ShouldBe("");
    }

    [Fact]
    public void Given_control_chars_and_oversize_When_sanitize_Then_stripped_and_capped()
    {
        // Given: controls plus a 200-char payload
        var raw = "hello\n\tworld" + new string('x', 200);

        // When
        var clean = DemoMessage.Sanitize(raw);

        // Then: no controls, max 120
        clean.ShouldNotContain("\n");
        clean.ShouldNotContain("\t");
        clean.Length.ShouldBe(DemoMessage.MaxLength);
        clean.ShouldStartWith("helloworld");
    }

    [Fact]
    public void Given_quotes_and_slashes_When_escape_json_Then_safe_in_string_literal()
    {
        // Given
        var message = "say \"hi\" \\ path";

        // When
        var json = DemoMessage.EscapeJson(message);
        var csharp = DemoMessage.EscapeCSharp(message);
        var js = DemoMessage.EscapeJavaScript(message);

        // Then
        json.ShouldBe("say \\\"hi\\\" \\\\ path");
        csharp.ShouldBe("say \\\"hi\\\" \\\\ path");
        js.ShouldBe(json);
    }
}
