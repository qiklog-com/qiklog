namespace QikLog.Infrastructure.Data;

/// <summary>QikLog tenant; maps 1:1 to a Zitadel organization when auth is enabled.</summary>
public sealed class TenantEntity
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    /// <summary>Zitadel org ID claim when using OIDC.</summary>
    public string? ZitadelOrgId { get; set; }

    public string Plan { get; set; } = "free";

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<ApiKeyEntity> ApiKeys { get; set; } = [];
}
