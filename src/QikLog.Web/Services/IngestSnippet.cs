using System.Text.Json;

namespace QikLog.Web.Services;

/// <summary>Copy-paste ingest examples shown in the dashboard, built from the live API URL.</summary>
internal static class IngestSnippet
{
    public static string Curl(string apiBaseUrl, string source)
    {
        var api = apiBaseUrl.TrimEnd('/');
        // JsonSerializer escapes quotes and apostrophes (", '), so the body is
        // valid JSON and safe inside the single-quoted shell argument.
        var body = $$"""{"source":{{JsonSerializer.Serialize(source)}},"level":"info","message":"hello from qiklog"}""";
        return $"curl -X POST {api}/v1/logs \\\n" +
               "  -H \"Content-Type: application/json\" \\\n" +
               "  -H \"Authorization: Bearer $QIKLOG_API_KEY\" \\\n" +
               $"  -d '{body}'";
    }
}
