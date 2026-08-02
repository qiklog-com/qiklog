using Shouldly;
using Xunit;

namespace QikLog.Core.Tests;

public sealed class LogEntryEdgeTests
{
    [Fact]
    public void LogEntry_with_properties_are_value_equal()
    {
        var props = new Dictionary<string, string> { ["a"] = "1" };
        var a = new LogEntry("s", LogLevel.Info, "m", DateTimeOffset.UnixEpoch, props);
        var b = new LogEntry("s", LogLevel.Info, "m", DateTimeOffset.UnixEpoch, props);
        a.ShouldBe(b);
    }

    [Fact]
    public void LogEntry_different_properties_are_not_equal()
    {
        var a = new LogEntry("s", LogLevel.Info, "m", DateTimeOffset.UnixEpoch,
            new Dictionary<string, string> { ["a"] = "1" });
        var b = new LogEntry("s", LogLevel.Info, "m", DateTimeOffset.UnixEpoch,
            new Dictionary<string, string> { ["a"] = "2" });
        a.ShouldNotBe(b);
    }

    [Theory]
    [InlineData(LogLevel.Trace, 0)]
    [InlineData(LogLevel.Debug, 1)]
    [InlineData(LogLevel.Info, 2)]
    [InlineData(LogLevel.Warning, 3)]
    [InlineData(LogLevel.Error, 4)]
    [InlineData(LogLevel.Critical, 5)]
    public void LogLevel_enum_values_are_stable(LogLevel level, int expected)
    {
        ((int)level).ShouldBe(expected);
    }
}
