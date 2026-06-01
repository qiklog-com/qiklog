namespace QikLog.Infrastructure.Data;

/// <summary>Hashed ingest credential. Plaintext is shown once at creation.</summary>
public sealed class ApiKeyEntity
{
    public Guid Id { get; set; }

    public Guid? TenantId { get; set; }

    public TenantEntity? Tenant { get; set; }

    public required string Name { get; set; }

    /// <summary>First segment after <c>ql_</c> for indexed lookup.</summary>
    public required string LookupPrefix { get; set; }

    /// <summary>Argon2id hash of the full plaintext key.</summary>
    public required string SecretHash { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? LastUsedAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public int RateLimitPerMinute { get; set; } = 120;
}
