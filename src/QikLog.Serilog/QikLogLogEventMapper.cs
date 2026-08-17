using Serilog.Events;

namespace QikLog.Serilog;

/// <summary>Maps a Serilog event onto the POST /v1/logs JSON body used by <c>qiklog send</c>.</summary>
internal static class QikLogLogEventMapper
{
    /// <summary>Serilog Verbose..Fatal → QikLog Trace..Critical integers (0–5).</summary>
    public static int ToQikLogLevel(LogEventLevel level) => level switch
    {
        LogEventLevel.Verbose => 0,
        LogEventLevel.Debug => 1,
        LogEventLevel.Information => 2,
        LogEventLevel.Warning => 3,
        LogEventLevel.Error => 4,
        LogEventLevel.Fatal => 5,
        _ => 2
    };

    public static QikLogIngestPayload ToPayload(LogEvent logEvent, string source)
    {
        Dictionary<string, string>? properties = null;
        if (logEvent.Properties.Count > 0)
        {
            properties = new Dictionary<string, string>(logEvent.Properties.Count, StringComparer.Ordinal);
            foreach (var pair in logEvent.Properties)
            {
                var value = RenderScalar(pair.Value);
                if (value is not null)
                    properties[pair.Key] = value;
            }

            if (properties.Count == 0)
                properties = null;
        }

        return new QikLogIngestPayload(
            Source: source,
            Message: logEvent.RenderMessage(),
            Level: ToQikLogLevel(logEvent.Level),
            Timestamp: logEvent.Timestamp.ToUniversalTime(),
            Properties: properties);
    }

    private static string? RenderScalar(LogEventPropertyValue value)
    {
        if (value is ScalarValue { Value: null })
            return null;

        if (value is ScalarValue scalar)
            return Convert.ToString(scalar.Value, System.Globalization.CultureInfo.InvariantCulture);

        return value.ToString();
    }
}

internal sealed record QikLogIngestPayload(
    string Source,
    string Message,
    int Level,
    DateTimeOffset Timestamp,
    IReadOnlyDictionary<string, string>? Properties);
