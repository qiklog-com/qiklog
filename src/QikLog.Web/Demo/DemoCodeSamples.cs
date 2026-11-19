namespace QikLog.Web.Demo;

/// <summary>Which code-deck template is on screen.</summary>
public enum DemoCodeTab
{
    /// <summary>curl sample.</summary>
    Curl,

    /// <summary>C# sample.</summary>
    CSharp,

    /// <summary>JavaScript sample.</summary>
    JavaScript
}

/// <summary>
/// Copies of <c>www/src/lib/code-deck.ts</c> templates. Rendering fills message,
/// level, and source. The API key placeholders stay as written.
/// </summary>
public static class DemoCodeSamples
{
    /// <summary>curl template. Backslashes are the curl line continuations.</summary>
    public const string CurlTemplate =
        "curl -X POST https://api.qiklog.com/v1/logs \\\n" +
        "  -H \"Content-Type: application/json\" \\\n" +
        "  -H \"Authorization: Bearer $QIKLOG_API_KEY\" \\\n" +
        "  -d '{\"source\":\"demo\",\"level\":\"info\",\"message\":\"__MSG__\"}'";

    /// <summary>C# template.</summary>
    public const string CSharpTemplate =
        "using var client = new HttpClient();\n" +
        "client.DefaultRequestHeaders.Authorization =\n" +
        "    new System.Net.Http.Headers.AuthenticationHeaderValue(\"Bearer\", \"YOUR_API_KEY\");\n" +
        "\n" +
        "await client.PostAsJsonAsync(\"https://api.qiklog.com/v1/logs\", new\n" +
        "{\n" +
        "    source = \"demo\",\n" +
        "    level = \"info\",\n" +
        "    message = \"__MSG__\"\n" +
        "});";

    /// <summary>JavaScript template.</summary>
    public const string JavaScriptTemplate =
        "await fetch(\"https://api.qiklog.com/v1/logs\", {\n" +
        "  method: \"POST\",\n" +
        "  headers: {\n" +
        "    \"Content-Type\": \"application/json\",\n" +
        "    Authorization: \"Bearer YOUR_API_KEY\"\n" +
        "  },\n" +
        "  body: JSON.stringify({\n" +
        "    source: \"demo\",\n" +
        "    level: \"info\",\n" +
        "    message: \"__MSG__\"\n" +
        "  })\n" +
        "});";

    /// <summary>The template for <paramref name="tab"/>.</summary>
    public static string Template(DemoCodeTab tab) => tab switch
    {
        DemoCodeTab.CSharp => CSharpTemplate,
        DemoCodeTab.JavaScript => JavaScriptTemplate,
        _ => CurlTemplate
    };

    /// <summary>Bake the visitor's source, wire level, and message into a template.</summary>
    public static string Render(DemoCodeTab tab, string source, string level, string message)
    {
        var template = Template(tab);
        var withSource = ReplaceFirst(template, "\"demo\"", "\"" + source + "\"");
        var withLevel = ReplaceFirst(withSource, "\"info\"", "\"" + level + "\"");
        var escaped = tab switch
        {
            DemoCodeTab.CSharp => QikLog.Core.DemoMessage.EscapeCSharp(message),
            DemoCodeTab.JavaScript => QikLog.Core.DemoMessage.EscapeJavaScript(message),
            _ => QikLog.Core.DemoMessage.EscapeJson(message)
        };
        return withLevel.Replace("__MSG__", escaped, StringComparison.Ordinal);
    }

    private static string ReplaceFirst(string template, string find, string replace)
    {
        var at = template.IndexOf(find, StringComparison.Ordinal);
        if (at < 0)
            return template;

        return string.Concat(template.AsSpan(0, at), replace, template.AsSpan(at + find.Length));
    }
}
