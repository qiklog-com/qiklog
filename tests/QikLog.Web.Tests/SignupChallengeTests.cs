using QikLog.Web;
using Shouldly;
using Xunit;

namespace QikLog.Web.Tests;

/// <summary>Contract for the self-serve sign-up redirect to Zitadel.</summary>
public sealed class SignupChallengeTests
{
    private static readonly string[] AppScopes =
        ["openid", "profile", "email", "offline_access", "urn:zitadel:iam:user:resourceowner"];

    [Fact]
    public void Signup_opens_zitadel_registration_in_the_signup_org()
    {
        var props = SignupChallenge.Build(AppScopes, " 123456789 ");

        props.Prompt.ShouldBe("create");
        props.Scope.ShouldContain("urn:zitadel:iam:org:id:123456789");
        props.RedirectUri.ShouldBe("/manage");
    }

    [Fact]
    public void Signup_keeps_every_normal_scope_so_the_api_can_resolve_the_tenant()
    {
        var props = SignupChallenge.Build(AppScopes, "123");

        foreach (var scope in AppScopes)
            props.Scope.ShouldContain(scope);
    }

    [Fact]
    public void Signup_does_not_duplicate_the_org_scope()
    {
        var props = SignupChallenge.Build([.. AppScopes, "urn:zitadel:iam:org:id:123"], "123");

        props.Scope.Count(s => s == "urn:zitadel:iam:org:id:123").ShouldBe(1);
    }
}
