namespace QikLog.Infrastructure.Auth;

/// <summary>OIDC via Zitadel (see docs/AUTH.md). Disabled when <see cref="Enabled"/> is false.</summary>
public sealed class QikLogAuthOptions
{
    public const string SectionName = "QikLog:Auth";

    public bool Enabled { get; set; }

    public string? Authority { get; set; }

    public string ClientId { get; set; } = "qiklog-web";

    public string? ClientSecret { get; set; }

    public string ApiAudience { get; set; } = "qiklog-api";

    /// <summary>Claim type for Zitadel organization ID (default Zitadel org claim).</summary>
    public string OrganizationClaim { get; set; } = "urn:zitadel:iam:org:id";
}
