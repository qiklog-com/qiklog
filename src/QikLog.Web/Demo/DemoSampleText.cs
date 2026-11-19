namespace QikLog.Web.Demo;

/// <summary>Key placeholders shown on the try-it-now page. No credential is included.</summary>
public static class DemoSampleText
{
    public const string Curl =
        "curl -X POST https://api.qiklog.com/v1/logs \\\n" +
        "  -H \"Content-Type: application/json\" \\\n" +
        "  -H \"Authorization: Bearer $QIKLOG_API_KEY\" \\\n" +
        "  -d '{\"source\":\"demo\",\"level\":\"info\",\"message\":\"hello from curl\"}'";

    public const string CSharp =
        "new AuthenticationHeaderValue(\"Bearer\", \"YOUR_API_KEY\");";

    public const string JavaScript =
        "Authorization: \"Bearer YOUR_API_KEY\"";
}
