namespace QikLog.Web.Services;

internal enum ApiCredentialKind
{
    None,
    AccessToken,
    HubApiKey
}

internal sealed record ApiCredential(ApiCredentialKind Kind, string? Value)
{
    public static readonly ApiCredential None = new(ApiCredentialKind.None, null);

    public static ApiCredential AccessToken(string token) => new(ApiCredentialKind.AccessToken, token);

    public static ApiCredential HubApiKey(string key) => new(ApiCredentialKind.HubApiKey, key);
}

/// <summary>
/// Single rule for which credential the web app presents to the API (HTTP and SignalR).
/// The hub API key belongs to the invite-beta bootstrap tenant, so it is only for
/// anonymous demo viewing and the marketing embed. A signed-in user gets their own
/// token or nothing; falling back to the hub key would show them another tenant's logs.
/// </summary>
internal static class ApiCredentialPolicy
{
    public static ApiCredential Choose(bool isAuthenticated, string? accessToken, string? hubApiKey, bool isEmbed)
    {
        var hubKey = string.IsNullOrWhiteSpace(hubApiKey) ? null : hubApiKey.Trim();

        if (isEmbed)
            return hubKey is null ? ApiCredential.None : ApiCredential.HubApiKey(hubKey);

        if (isAuthenticated)
        {
            return string.IsNullOrWhiteSpace(accessToken)
                ? ApiCredential.None
                : ApiCredential.AccessToken(accessToken.Trim());
        }

        return hubKey is null ? ApiCredential.None : ApiCredential.HubApiKey(hubKey);
    }
}
