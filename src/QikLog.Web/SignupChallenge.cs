using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace QikLog.Web;

/// <summary>Builds the OIDC challenge for self-serve sign-up.</summary>
internal static class SignupChallenge
{
    /// <summary>Zitadel reserved scope that pins login and registration to one org.</summary>
    public static string OrgScope(string orgId) => $"urn:zitadel:iam:org:id:{orgId.Trim()}";

    /// <summary>
    /// The app's normal scopes plus the sign-up org scope, and <c>prompt=create</c> so
    /// Zitadel opens its registration form instead of the login form.
    /// </summary>
    public static OpenIdConnectChallengeProperties Build(IEnumerable<string> baseScopes, string signupOrgId)
    {
        var scopes = baseScopes.ToList();
        var orgScope = OrgScope(signupOrgId);
        if (!scopes.Contains(orgScope, StringComparer.Ordinal))
            scopes.Add(orgScope);

        return new OpenIdConnectChallengeProperties
        {
            RedirectUri = "/manage",
            Prompt = "create",
            Scope = scopes
        };
    }
}
