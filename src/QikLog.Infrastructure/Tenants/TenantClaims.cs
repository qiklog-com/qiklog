using System.Security.Claims;
using QikLog.Infrastructure.Auth;

namespace QikLog.Infrastructure.Tenants;

/// <summary>
/// Shared claim reads for web OIDC provisioning and API JWT tenant resolution.
/// Must stay in sync: web writes tenants under the org id this helper returns;
/// the API looks them up with the same helper against the access token.
/// </summary>
public static class TenantClaims
{
    /// <summary>
    /// Zitadel scope that asserts resource-owner (organization) id/name on JWT access tokens.
    /// Without it, org claims appear on userinfo/id_token only and the API cannot resolve a tenant.
    /// </summary>
    public const string ResourceOwnerScope = "urn:zitadel:iam:user:resourceowner";

    /// <summary>Organization id claim asserted when <see cref="ResourceOwnerScope"/> is requested.</summary>
    public const string ResourceOwnerIdClaim = "urn:zitadel:iam:user:resourceowner:id";

    public static string? TryGetOrgId(ClaimsPrincipal? principal, QikLogAuthOptions auth)
    {
        if (principal is null)
            return null;

        var configured = principal.FindFirst(auth.OrganizationClaim)?.Value;
        if (!string.IsNullOrWhiteSpace(configured))
            return configured.Trim();

        var resourceOwner = principal.FindFirst(ResourceOwnerIdClaim)?.Value;
        return string.IsNullOrWhiteSpace(resourceOwner) ? null : resourceOwner.Trim();
    }

    public static string GetDisplayName(ClaimsPrincipal? principal)
    {
        if (principal is null)
            return "Tenant";

        return principal.FindFirst("name")?.Value
            ?? principal.FindFirst("email")?.Value
            ?? principal.FindFirst(ClaimTypes.Email)?.Value
            ?? "Tenant";
    }
}
