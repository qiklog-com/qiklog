using Shouldly;
using Xunit;

namespace QikLog.Core.Tests;

public sealed class TenantClaimContractTests
{
    [Fact]
    public void Web_and_api_use_the_same_org_claim_helper()
    {
        var web = ReadRepoFile("src/QikLog.Web/WebAuthExtensions.cs");
        web.ShouldContain("TenantClaims.TryGetOrgId");
        web.ShouldContain("TenantClaims.ResourceOwnerScope");
        web.ShouldContain("TenantClaims.GetDisplayName");

        var resolver = ReadRepoFile("src/QikLog.Infrastructure/Tenants/TenantResolver.cs");
        resolver.ShouldContain("TenantClaims.TryGetOrgId");
        resolver.ShouldContain("TenantClaims.GetDisplayName");

        var claims = ReadRepoFile("src/QikLog.Infrastructure/Tenants/TenantClaims.cs");
        claims.ShouldContain("urn:zitadel:iam:user:resourceowner");
        claims.ShouldContain("urn:zitadel:iam:user:resourceowner:id");
        claims.ShouldContain("Shared claim reads for web OIDC provisioning and API JWT tenant resolution");
    }

    [Fact]
    public void Testing_convention_doc_exists_and_names_the_process()
    {
        var doc = ReadRepoFile("docs/TESTING.md");
        doc.ShouldContain("Conditions of Satisfaction");
        doc.ShouldContain("Given / When / Then");
        doc.ShouldContain("QikLog.Smoke.Tests");
        doc.ShouldContain("make test");
        doc.ShouldNotContain("—");
    }

    [Fact]
    public void Api_jwt_bearer_preserves_zitadel_claim_uris()
    {
        var api = ReadRepoFile("src/QikLog.Api/ApiAuthExtensions.cs");
        api.ShouldContain("MapInboundClaims = false");
    }

    private static string ReadRepoFile(string relativePath)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "QikLog.sln")))
            dir = dir.Parent;

        dir.ShouldNotBeNull("could not find repo root from test output directory");
        var path = Path.Combine(dir.FullName, relativePath);
        File.Exists(path).ShouldBeTrue($"missing {relativePath}");
        return File.ReadAllText(path);
    }
}
