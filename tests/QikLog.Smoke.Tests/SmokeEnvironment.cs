namespace QikLog.Smoke.Tests;

/// <summary>
/// Configuration for smoke tests that run against a live deployment.
/// Opt in with <c>QIKLOG_SMOKE=1</c>; override targets with
/// <c>QIKLOG_SMOKE_WEB_URL</c> / <c>QIKLOG_SMOKE_API_URL</c>.
/// </summary>
public static class SmokeEnvironment
{
    /// <summary>Default Railway production dashboard origin.</summary>
    public const string DefaultWebUrl = "https://qiklog.up.railway.app";

    /// <summary>Default Railway production API origin.</summary>
    public const string DefaultApiUrl = "https://qiklog-api.up.railway.app";

    /// <summary>True when smoke tests are allowed to hit the network.</summary>
    public static bool Enabled =>
        string.Equals(Environment.GetEnvironmentVariable("QIKLOG_SMOKE"), "1", StringComparison.Ordinal);

    /// <summary>Origin of the Blazor dashboard under test, without a trailing slash.</summary>
    public static string WebUrl => Normalize(
        Environment.GetEnvironmentVariable("QIKLOG_SMOKE_WEB_URL"), DefaultWebUrl);

    /// <summary>Origin of the ingest/management API under test, without a trailing slash.</summary>
    public static string ApiUrl => Normalize(
        Environment.GetEnvironmentVariable("QIKLOG_SMOKE_API_URL"), DefaultApiUrl);

    private static string Normalize(string? value, string fallback) =>
        (string.IsNullOrWhiteSpace(value) ? fallback : value).TrimEnd('/');
}
