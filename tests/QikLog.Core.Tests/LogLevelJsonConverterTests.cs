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
}
