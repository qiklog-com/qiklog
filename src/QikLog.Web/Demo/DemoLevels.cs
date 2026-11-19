using LogLevel = QikLog.Core.LogLevel;

namespace QikLog.Web.Demo;

/// <summary>Wire names for the playground level chips.</summary>
public static class DemoLevels
{
    /// <summary>JSON level string the ingest API accepts.</summary>
    public static string Wire(LogLevel level) => level switch
    {
        LogLevel.Warning => "warning",
        LogLevel.Error => "error",
        LogLevel.Critical => "critical",
        _ => "info"
    };
}
