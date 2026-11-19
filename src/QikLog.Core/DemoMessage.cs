using System.Text;

namespace QikLog.Core;

/// <summary>
/// Sanitizer and sample escaping for the try-it-now box.
/// Keep in lockstep with <c>www/src/lib/demo-message.ts</c> and <c>www/api/demo-send.js</c>.
/// </summary>
public static class DemoMessage
{
    /// <summary>Maximum message length after control characters are removed.</summary>
    public const int MaxLength = 120;

    /// <summary>Trim, drop C0 controls and DEL, then cap at <see cref="MaxLength"/>.</summary>
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

    /// <summary>Escape a message for a JSON string literal.</summary>
    public static string EscapeJson(string value) => Escape(value);

    /// <summary>Escape a message for a C# string literal. Same rules as JSON.</summary>
    public static string EscapeCSharp(string value) => Escape(value);

    /// <summary>Escape a message for a JavaScript string literal. Same rules as JSON.</summary>
    public static string EscapeJavaScript(string value) => Escape(value);

    private static string Escape(string value)
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
}
