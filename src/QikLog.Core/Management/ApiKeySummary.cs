using System.Text.Json.Serialization;

namespace QikLog.Core.Management;

/// <summary>API key metadata for management UI (never includes the secret).</summary>
public sealed record ApiKeySummary(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("lookupPrefix")] string LookupPrefix,
    [property: JsonPropertyName("isActive")] bool IsActive,
    [property: JsonPropertyName("createdAt")] DateTimeOffset CreatedAt,
    [property: JsonPropertyName("lastUsedAt")] DateTimeOffset? LastUsedAt,
    [property: JsonPropertyName("revokedAt")] DateTimeOffset? RevokedAt,
    [property: JsonPropertyName("rateLimitPerMinute")] int RateLimitPerMinute);
