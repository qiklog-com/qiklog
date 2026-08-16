using Shouldly;
using Xunit;

namespace QikLog.Core.Tests;

public sealed class JwtAudienceContractTests
{
    [Fact]
    public void Web_oidc_requests_zitadel_project_audience_scope()
    {
        var web = ReadRepoFile("src/QikLog.Web/WebAuthExtensions.cs");
        web.ShouldContain("ProjectAudienceScope");
        web.ShouldContain("options.Scope.Add(auth.ProjectAudienceScope)");
        web.ShouldNotContain("AudienceValidator");
        web.ShouldNotContain("—");
    }

    [Fact]
    public void Api_jwt_bearer_validates_configured_audience_without_bypass()
    {
        var api = ReadRepoFile("src/QikLog.Api/ApiAuthExtensions.cs");
        api.ShouldContain("options.Audience = auth.ApiAudience");
        api.ShouldContain("ValidateAudience = true");
        api.ShouldContain("ValidateIssuer = true");
        api.ShouldContain("MapInboundClaims = false");
        api.ShouldNotContain("AudienceValidator");
        api.ShouldNotContain("ValidAudiences");
    }

    [Fact]
    public void Appsettings_defaults_use_project_id_not_qiklog_api_string()
    {
        ReadRepoFile("src/QikLog.Api/appsettings.json").ShouldContain("383416044909259568");
        ReadRepoFile("src/QikLog.Api/appsettings.json").ShouldNotContain("qiklog-api");
        ReadRepoFile("src/QikLog.Web/appsettings.json").ShouldContain("383416044909259568");
        ReadRepoFile("src/QikLog.Web/appsettings.json").ShouldNotContain("qiklog-api");
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
