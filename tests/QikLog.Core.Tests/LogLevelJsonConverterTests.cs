using System.Text.Json;
using Shouldly;
using Xunit;

namespace QikLog.Core.Tests;

public class LogLevelJsonConverterTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new LogLevelJsonConverter() }
    };

    [Theory]
    [InlineData("\"info\"", LogLevel.Info)]
    [InlineData("\"INFO\"", LogLevel.Info)]
    [InlineData("2", LogLevel.Info)]
    [InlineData("\"warn\"", LogLevel.Warning)]
    [InlineData("\"err\"", LogLevel.Error)]
    public void Read_accepts_string_and_integer(string json, LogLevel expected)
    {
        var level = JsonSerializer.Deserialize<LogLevel>(json, Options);
        level.ShouldBe(expected);
    }

    [Fact]
    public void Write_uses_lowercase_name()
    {
        var json = JsonSerializer.Serialize(LogLevel.Warning, Options);
        json.ShouldBe("\"warning\"");
    }

    [Theory]
    [InlineData("null")]
    [InlineData("\"\"")]
    [InlineData("\"   \"")]
    public void Read_rejects_empty_level(string json)
    {
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<LogLevel>(json, Options));
    }

    [Fact]
    public void Read_rejects_unknown_string()
    {
        Should.Throw<JsonException>(() =>
            JsonSerializer.Deserialize<LogLevel>("\"nope\"", Options));
    }

    [Theory]
    [InlineData("99")]
    [InlineData("-1")]
    public void Read_rejects_out_of_range_integer(string json)
    {
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<LogLevel>(json, Options));
    }

    [Theory]
    [InlineData(LogLevel.Critical, "\"crit\"")]
    [InlineData(LogLevel.Error, "\"err\"")]
    public void Read_accepts_common_aliases(LogLevel expected, string json)
    {
        JsonSerializer.Deserialize<LogLevel>(json, Options).ShouldBe(expected);
    }
}
