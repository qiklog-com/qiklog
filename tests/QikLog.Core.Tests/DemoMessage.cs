using System.Text;

namespace QikLog.Core.Tests;

/// <summary>
/// Spec for the landing demo-send sanitizer and per-language sample escaping.
/// Keep in lockstep with <c>www/src/lib/demo-message.ts</c> and <c>www/api/demo-send.js</c>.
/// </summary>
internal static class DemoMessage
{
    public const int MaxLength = 120;

    public static string Sanitize(string? raw)
    {
        if (string.IsNullOrEmpty(raw))
            return "";

        var builder = new StringBuilder(raw.Length);
        foreach (var ch in raw.Trim())
        {
            if (ch is >= '\u0000' and <= '\u001F' or '\u007F')
                continue;
            builder.Append(ch);
            if (builder.Length >= MaxLength)
                break;
        }

        return builder.ToString();
    }

    public static string EscapeJson(string value)
    {
        var builder = new StringBuilder(value.Length + 8);
        foreach (var ch in value)
        {
            switch (ch)
            {
                case '\\': builder.Append("\\\\"); break;
                case '"': builder.Append("\\\""); break;
                case '\n': builder.Append("\\n"); break;
                case '\r': builder.Append("\\r"); break;
                case '\t': builder.Append("\\t"); break;
                default: builder.Append(ch); break;
            }
        }

        return builder.ToString();
    }

    public static string EscapeCSharp(string value)
    {
        var builder = new StringBuilder(value.Length + 8);
        foreach (var ch in value)
        {
            switch (ch)
            {
                case '\\': builder.Append("\\\\"); break;
                case '"': builder.Append("\\\""); break;
                case '\n': builder.Append("\\n"); break;
                case '\r': builder.Append("\\r"); break;
                case '\t': builder.Append("\\t"); break;
                default: builder.Append(ch); break;
            }
        }

        return builder.ToString();
    }

    public static string EscapeJavaScript(string value) => EscapeJson(value);
}
