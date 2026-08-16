namespace QikLog.Infrastructure.Auth;

/// <summary>OIDC via Zitadel (see docs/AUTH.md). Disabled when <see cref="Enabled"/> is false.</summary>
public sealed class QikLogAuthOptions
{
    public const string SectionName = "QikLog:Auth";

    public bool Enabled { get; set; }

    public string? Authority { get; set; }

    public string ClientId { get; set; } = "qiklog-web";

    public string? ClientSecret { get; set; }

    /// <summary>
    /// Zitadel project id. Zitadel puts this in <c>aud</c> when the web app
    /// requests <see cref="ProjectAudienceScope"/>. Not the string "qiklog-api".
    /// </summary>
    public string ApiAudience { get; set; } = "383416044909259568";

    /// <summary>
    /// Reserved Zitadel scope that adds <see cref="ApiAudience"/> to the access token audience.
    /// </summary>
    public string ProjectAudienceScope =>
        $"urn:zitadel:iam:org:project:id:{ApiAudience}:aud";

    /// <summary>Claim type for Zitadel organization ID (default Zitadel org claim).</summary>
    public string OrganizationClaim { get; set; } = "urn:zitadel:iam:org:id";
}
