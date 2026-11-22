using QikLog.Core.Links;
using Shouldly;
using Xunit;

namespace QikLog.Core.Tests;

public sealed class LinkLineParserTests
{
    private const string FormType = "application/x-www-form-urlencoded";

    private static LinkLineParseResult Parse(string? body, string? contentType = FormType, string? level = null) =>
        LinkLineParser.Parse(body, contentType, level, maxLineLength: 2048, maxLines: 50);

    [Fact]
    public void Given_curl_d_body_When_parsed_Then_the_body_is_the_line_at_info()
    {
        // Given: what `curl <url> -d "it broke"` sends
        // When
        var result = Parse("it broke");

        // Then
        result.Ok.ShouldBeTrue();
        result.Lines.ShouldHaveSingleItem().ShouldBe(new LinkLine(LogLevel.Info, "it broke"));
    }

    [Theory]
    [InlineData("ERROR 500 POST /checkout NullReferenceException", LogLevel.Error)]
    [InlineData("WARN slow query orders_by_customer 812ms", LogLevel.Warning)]
    [InlineData("error: payment failed", LogLevel.Error)]
    [InlineData("[warn] cache miss", LogLevel.Warning)]
    [InlineData("[DEBUG] entering handler", LogLevel.Debug)]
    [InlineData("FATAL out of memory", LogLevel.Critical)]
    [InlineData("Critical: disk full", LogLevel.Critical)]
    [InlineData("TRACE|span start", LogLevel.Trace)]
    public void Given_a_leading_level_token_When_parsed_Then_that_level_is_used(string body, LogLevel expected)
    {
        var line = Parse(body).Lines.ShouldHaveSingleItem();
        line.Level.ShouldBe(expected);
        line.Message.ShouldBe(body);
    }

    [Theory]
    [InlineData("error handling works now")]
    [InlineData("debug mode is on")]
    [InlineData("warning signs everywhere")]
    [InlineData("ERRORS were found")]
    [InlineData("200 GET /api/orders 24ms")]
    public void Given_ordinary_prose_When_parsed_Then_it_stays_info(string body)
    {
        Parse(body).Lines.ShouldHaveSingleItem().Level.ShouldBe(LogLevel.Info);
    }

    [Fact]
    public void Given_a_level_query_value_When_parsed_Then_it_wins_over_the_token()
    {
        Parse("ERROR but really fine", level: "debug").Lines.ShouldHaveSingleItem().Level.ShouldBe(LogLevel.Debug);
    }

    [Fact]
    public void Given_json_with_message_and_level_When_parsed_Then_both_are_used()
    {
        var result = Parse("""{"message":"slow query","level":"warn"}""", "application/json; charset=utf-8");

        result.Lines.ShouldHaveSingleItem().ShouldBe(new LinkLine(LogLevel.Warning, "slow query"));
    }

    [Theory]
    [InlineData("""{"msg":"no message field"}""")]
    [InlineData("""["a","b"]""")]
    [InlineData("{not json")]
    public void Given_json_without_a_message_When_parsed_Then_the_text_is_logged_as_is(string body)
    {
        Parse(body, "application/json").Lines.ShouldHaveSingleItem().Message.ShouldBe(body);
    }

    [Fact]
    public void Given_a_piped_multi_line_body_When_parsed_Then_each_line_is_its_own_entry()
    {
        var result = Parse("starting up\r\n\r\nERROR boom\nlistening on 8080\n", "text/plain");

        result.Lines.Select(l => l.Message).ShouldBe(["starting up", "ERROR boom", "listening on 8080"]);
        result.Lines[1].Level.ShouldBe(LogLevel.Error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   \n\r\n  ")]
    public void Given_an_empty_body_When_parsed_Then_it_is_refused(string? body)
    {
        Parse(body).Error.ShouldBe(LinkLineError.Empty);
    }

    [Fact]
    public void Given_a_line_over_the_limit_When_parsed_Then_it_is_refused()
    {
        var result = LinkLineParser.Parse(new string('x', 11), FormType, null, maxLineLength: 10, maxLines: 5);

        result.Error.ShouldBe(LinkLineError.TooLong);
        result.Lines.ShouldBeEmpty();
    }

    [Fact]
    public void Given_more_lines_than_allowed_When_parsed_Then_it_is_refused()
    {
        var result = LinkLineParser.Parse("a\nb\nc", "text/plain", null, maxLineLength: 10, maxLines: 2);

        result.Error.ShouldBe(LinkLineError.TooManyLines);
    }

    [Fact]
    public void Given_control_characters_When_parsed_Then_they_are_dropped_and_tabs_kept()
    {
        var line = Parse("a\u0007b\tc\u001b[31m").Lines.ShouldHaveSingleItem();

        line.Message.ShouldBe("ab\tc[31m");
    }
}
