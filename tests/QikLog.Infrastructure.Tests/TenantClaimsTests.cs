using System.Security.Claims;
using QikLog.Infrastructure.Auth;
using QikLog.Infrastructure.Tenants;
using Shouldly;
using Xunit;

namespace QikLog.Infrastructure.Tests;

/// <summary>
/// Unit net for PR #5: web and API must read the same org id from claims.
/// </summary>
public sealed class TenantClaimsTests
{
    [Fact]
    public void TryGetOrgId_returns_configured_organization_claim()
    {
        // Given: principal has urn:zitadel:iam:org:id
        // When: TryGetOrgId
        // Then: that value wins
        var principal = Principal(("urn:zitadel:iam:org:id", " org-a "));
        TenantClaims.TryGetOrgId(principal, new QikLogAuthOptions()).ShouldBe("org-a");
    }

    [Fact]
    public void TryGetOrgId_falls_back_to_resourceowner_id()
    {
        // Given: access token only has resourceowner:id (typical after resourceowner scope)
        // When: TryGetOrgId
        // Then: resourceowner id is used (API can resolve the web-provisioned tenant)
        var principal = Principal((TenantClaims.ResourceOwnerIdClaim, "383416044909259999"));
        TenantClaims.TryGetOrgId(principal, new QikLogAuthOptions())
            .ShouldBe("383416044909259999");
    }

    [Fact]
    public void TryGetOrgId_returns_null_when_no_org_claims()
    {
        // Given: JWT with only sub/name (audience fix alone is not enough)
        // When: TryGetOrgId
        // Then: null → TenantResolver returns TenantNotFound
        var principal = Principal(("sub", "user-1"), ("name", "Dev"));
        TenantClaims.TryGetOrgId(principal, new QikLogAuthOptions()).ShouldBeNull();
    }

    [Fact]
    public void TryGetOrgId_null_principal_returns_null()
    {
        TenantClaims.TryGetOrgId(null, new QikLogAuthOptions()).ShouldBeNull();
    }

    [Fact]
    public void ResourceOwnerScope_matches_zitadel_reserved_scope()
    {
        TenantClaims.ResourceOwnerScope.ShouldBe("urn:zitadel:iam:user:resourceowner");
        TenantClaims.ResourceOwnerIdClaim.ShouldBe("urn:zitadel:iam:user:resourceowner:id");
    }

    [Fact]
    public void GetDisplayName_prefers_name_then_email()
    {
        TenantClaims.GetDisplayName(Principal(("email", "a@b.c"))).ShouldBe("a@b.c");
        TenantClaims.GetDisplayName(Principal(("name", "N"), ("email", "a@b.c"))).ShouldBe("N");
        TenantClaims.GetDisplayName(null).ShouldBe("Tenant");
    }

    private static ClaimsPrincipal Principal(params (string Type, string Value)[] claims)
    {
        var identity = new ClaimsIdentity(claims.Select(c => new Claim(c.Type, c.Value)), "test");
        return new ClaimsPrincipal(identity);
    }
}
