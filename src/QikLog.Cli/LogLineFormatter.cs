using QikLog.Core;

namespace QikLog.Cli;

/// <summary>Formats a <see cref="LogEntry"/> the way CLI watch prints live lines.</summary>
internal static class LogLineFormatter
{
    public static string Format(LogEntry entry)
    {
        var level = LevelLabel(entry.Level);
        return $"{entry.Timestamp:HH:mm:ss.fff} {level,-5} {entry.Source} {entry.Message}";
    }

    public static string LevelLabel(LogLevel level) => level switch
    {
        LogLevel.Trace => "TRACE",
        LogLevel.Debug => "DEBUG",
        LogLevel.Info => "INFO",
        LogLevel.Warning => "WARN",
        LogLevel.Error => "ERROR",
        LogLevel.Critical => "CRIT",
        _ => level.ToString().ToUpperInvariant()
    };
}
