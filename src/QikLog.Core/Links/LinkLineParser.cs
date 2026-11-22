using System.Text;
using System.Text.Json;

namespace QikLog.Core.Links;

/// <summary>One line sent to a log link, after parsing.</summary>
public sealed record LinkLine(LogLevel Level, string Message);

public enum LinkLineError
{
    None = 0,
    Empty = 1,
    TooLong = 2,
    TooManyLines = 3
}

public sealed record LinkLineParseResult(LinkLineError Error, IReadOnlyList<LinkLine> Lines)
{
    public bool Ok => Error == LinkLineError.None;

    public static LinkLineParseResult Failed(LinkLineError error) => new(error, []);
}

/// <summary>
/// Turns the body of <c>POST /l/{id}</c> into lines. The body is the line: no JSON,
/// headers, or key required. JSON with a <c>message</c> field is also accepted, and a
/// piped multi-line body becomes one line each.
/// </summary>
public static class LinkLineParser
{
    public static LinkLineParseResult Parse(
        string? body,
        string? contentType,
        string? levelOverride,
        int maxLineLength,
        int maxLines)
    {
        if (string.IsNullOrWhiteSpace(body))
            return LinkLineParseResult.Failed(LinkLineError.Empty);

        var explicitLevel = ParseLevel(levelOverride);

        if (IsJson(contentType) && TryParseJson(body, out var jsonMessage, out var jsonLevel))
        {
            var message = Clean(jsonMessage, keepNewlines: true);
            if (message.Length == 0)
                return LinkLineParseResult.Failed(LinkLineError.Empty);
            if (message.Length > maxLineLength)
                return LinkLineParseResult.Failed(LinkLineError.TooLong);

            var level = explicitLevel ?? jsonLevel ?? DetectLevel(message) ?? LogLevel.Info;
            return new LinkLineParseResult(LinkLineError.None, [new LinkLine(level, message)]);
        }

        var lines = new List<LinkLine>();
        foreach (var raw in body.Split('\n'))
        {
            var message = Clean(raw, keepNewlines: false);
            if (message.Length == 0)
                continue;
            if (message.Length > maxLineLength)
                return LinkLineParseResult.Failed(LinkLineError.TooLong);
            if (lines.Count == maxLines)
                return LinkLineParseResult.Failed(LinkLineError.TooManyLines);

            lines.Add(new LinkLine(explicitLevel ?? DetectLevel(message) ?? LogLevel.Info, message));
        }

        return lines.Count == 0
            ? LinkLineParseResult.Failed(LinkLineError.Empty)
            : new LinkLineParseResult(LinkLineError.None, lines);
    }

    /// <summary>Level names people actually type. Null when the text is not a level.</summary>
    public static LogLevel? ParseLevel(string? text) => text?.Trim().ToLowerInvariant() switch
    {
        "trace" => LogLevel.Trace,
        "debug" => LogLevel.Debug,
        "info" or "information" => LogLevel.Info,
        "warn" or "warning" => LogLevel.Warning,
        "error" or "err" => LogLevel.Error,
        "critical" or "crit" or "fatal" => LogLevel.Critical,
        _ => null
    };

    /// <summary>
    /// Reads a leading level token: <c>ERROR ...</c>, <c>warn: ...</c>, <c>[info] ...</c>.
    /// A lowercase word with no colon or bracket is ordinary prose ("error handling works")
    /// and is not treated as a level.
    /// </summary>
    public static LogLevel? DetectLevel(string message)
    {
        var span = message.AsSpan().TrimStart();
        var bracketed = span.Length > 0 && span[0] == '[';
        if (bracketed)
            span = span[1..];

        var length = 0;
        while (length < span.Length && char.IsAsciiLetter(span[length]))
            length++;

        if (length is 0 or > 11)
            return null;

        var token = span[..length];
        var next = length < span.Length ? span[length] : ' ';
        if (next is not (' ' or '\t' or ':' or ']' or '|'))
            return null;

        var marked = bracketed || next is ':' or ']' or '|';
        var upper = true;
        foreach (var ch in token)
        {
            if (!char.IsAsciiLetterUpper(ch))
            {
                upper = false;
                break;
            }
        }

        return marked || upper ? ParseLevel(token.ToString()) : null;
    }

    private static bool IsJson(string? contentType) =>
        contentType is not null
        && contentType.TrimStart().StartsWith("application/json", StringComparison.OrdinalIgnoreCase);

    private static bool TryParseJson(string body, out string message, out LogLevel? level)
    {
        message = "";
        level = null;
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("message", out var messageElement)
                || messageElement.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            message = messageElement.GetString() ?? "";
            if (document.RootElement.TryGetProperty("level", out var levelElement)
                && levelElement.ValueKind == JsonValueKind.String)
            {
                level = ParseLevel(levelElement.GetString());
            }

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>Drops control characters (tabs stay) and trims the ends.</summary>
    private static string Clean(string raw, bool keepNewlines)
    {
        var builder = new StringBuilder(raw.Length);
        foreach (var ch in raw)
        {
            if (ch == '\t' || (keepNewlines && ch == '\n') || !char.IsControl(ch))
                builder.Append(ch);
        }

        return builder.ToString().Trim();
    }
}
