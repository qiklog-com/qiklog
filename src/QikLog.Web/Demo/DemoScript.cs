using LogLevel = QikLog.Core.LogLevel;

namespace QikLog.Web.Demo;

/// <summary>One line in the simulate-traffic script.</summary>
public sealed record DemoScriptLine(LogLevel Level, string Message);

/// <summary>The fixed burst played by Simulate traffic.</summary>
public static class DemoScript
{
    /// <summary>Pause between scripted lines. Seven gaps stay under three seconds.</summary>
    public static readonly TimeSpan Gap = TimeSpan.FromMilliseconds(375);

    /// <summary>Eight realistic lines, in send order.</summary>
    public static readonly IReadOnlyList<DemoScriptLine> Lines =
    [
        new(LogLevel.Info, "200 GET /api/orders 24ms"),
        new(LogLevel.Warning, "WARN slow query orders_by_customer 812ms"),
        new(LogLevel.Error, "ERROR 500 POST /checkout NullReferenceException"),
        new(LogLevel.Info, "201 POST /api/orders 41ms"),
        new(LogLevel.Info, "200 GET /api/orders/ord_18 18ms"),
        new(LogLevel.Info, "payment authorized ord_18"),
        new(LogLevel.Warning, "WARN inventory hold expired ord_18"),
        new(LogLevel.Critical, "CRIT checkout worker missed heartbeat")
    ];
}
