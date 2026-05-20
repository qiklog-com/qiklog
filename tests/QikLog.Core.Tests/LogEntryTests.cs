using Shouldly;
using Xunit;

namespace QikLog.Core.Tests;

public class LogEntryTests
{
    [Fact]
    public void LogEntry_records_are_value_equal()
    {
        var ts = DateTimeOffset.UtcNow;
        var a = new LogEntry("api", LogLevel.Info, "hello", ts);
        var b = new LogEntry("api", LogLevel.Info, "hello", ts);
        a.ShouldBe(b);
    }

    [Fact]
    public void LogEntry_properties_default_to_null()
    {
        var entry = new LogEntry("api", LogLevel.Info, "hello", DateTimeOffset.UtcNow);
        entry.Properties.ShouldBeNull();
    }
}
