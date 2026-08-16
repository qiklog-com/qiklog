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

    /// <summary>
    /// True when the opt-in send→watch latency measurement may hit a live API.
    /// Independent of <see cref="Enabled"/> so timing is never part of <c>make smoke</c>.
    /// </summary>
    public static bool TimingEnabled =>
        string.Equals(Environment.GetEnvironmentVariable("QIKLOG_TIMING"), "1", StringComparison.Ordinal);

    /// <summary>Origin of the Blazor dashboard under test, without a trailing slash.</summary>
    public static string WebUrl => Normalize(
        Environment.GetEnvironmentVariable("QIKLOG_SMOKE_WEB_URL"), DefaultWebUrl);

    /// <summary>Origin of the ingest/management API under test, without a trailing slash.</summary>
    public static string ApiUrl => Normalize(
        Environment.GetEnvironmentVariable("QIKLOG_SMOKE_API_URL"), DefaultApiUrl);

    /// <summary>
    /// Optional Zitadel access token for JWT manage-endpoint smoke.
    /// </summary>
    public static string? AccessToken
    {
        get
        {
            var token = Environment.GetEnvironmentVariable("QIKLOG_SMOKE_ACCESS_TOKEN");
            return string.IsNullOrWhiteSpace(token) ? null : token.Trim();
        }
    }

    /// <summary>
    /// True when <see cref="AccessToken"/> has three JWT segments.
    /// Opaque Bearer tokens from Zitadel fail this and must not be used for Manage smoke.
    /// </summary>
    public static bool AccessTokenLooksLikeJwt
    {
        get
        {
            var token = AccessToken;
            if (token is null)
                return false;
            var parts = token.Split('.');
            return parts.Length == 3
                && parts.All(static p => p.Length > 0);
        }
    }

    /// <summary>Custom domain for the Blazor app (TLS smoke).</summary>
    public const string AppCustomDomainUrl = "https://app.qiklog.com";

    /// <summary>Custom domain for the API (TLS smoke).</summary>
    public const string ApiCustomDomainUrl = "https://api.qiklog.com";

    /// <summary>
    /// Optional API key enabling the authenticated round-trip check. Without it those
    /// tests skip, because minting a key is a manual step against a real deployment.
    /// </summary>
    public static string? ApiKey
    {
        get
        {
            var key = Environment.GetEnvironmentVariable("QIKLOG_SMOKE_API_KEY");
            return string.IsNullOrWhiteSpace(key) ? null : key.Trim();
        }
    }

    private static string Normalize(string? value, string fallback) =>
        (string.IsNullOrWhiteSpace(value) ? fallback : value).TrimEnd('/');
}
