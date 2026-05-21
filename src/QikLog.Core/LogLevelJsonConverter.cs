using System.Text.Json;
using System.Text.Json.Serialization;

namespace QikLog.Core;

/// <summary>
/// Accepts <see cref="LogLevel"/> on the wire as an integer (0–5) or a case-insensitive name
/// (e.g. <c>"info"</c>, <c>"warning"</c>). Common aliases: <c>warn</c>, <c>err</c>, <c>crit</c>.
/// </summary>
public sealed class LogLevelJsonConverter : JsonConverter<LogLevel>
{
    public override LogLevel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number when reader.TryGetInt32(out var n) && Enum.IsDefined(typeof(LogLevel), n)
                => (LogLevel)n,
            JsonTokenType.String => ParseString(reader.GetString()),
            _ => throw new JsonException("level must be a number or string (e.g. \"info\" or 2).")
        };
    }

    public override void Write(Utf8JsonWriter writer, LogLevel value, JsonSerializerOptions options) =>
        writer.WriteStringValue(ToWireName(value));

    private static LogLevel ParseString(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new JsonException("level must not be empty.");

        if (Enum.TryParse<LogLevel>(raw, ignoreCase: true, out var level))
            return level;

        return raw.Trim().ToLowerInvariant() switch
        {
            "warn" => LogLevel.Warning,
            "err" => LogLevel.Error,
            "crit" => LogLevel.Critical,
            _ => throw new JsonException($"Unknown log level \"{raw}\".")
        };
    }

    private static string ToWireName(LogLevel level) => level switch
    {
        LogLevel.Trace => "trace",
        LogLevel.Debug => "debug",
        LogLevel.Info => "info",
        LogLevel.Warning => "warning",
        LogLevel.Error => "error",
        LogLevel.Critical => "critical",
        _ => level.ToString().ToLowerInvariant()
    };
}
