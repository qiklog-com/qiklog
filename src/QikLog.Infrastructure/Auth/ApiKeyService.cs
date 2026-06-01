using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QikLog.Core;
using QikLog.Core.Management;
using QikLog.Infrastructure.Data;

namespace QikLog.Infrastructure.Auth;

public sealed record ApiKeyCreateResult(Guid Id, string Plaintext, string Name);

public sealed record ApiKeyValidationResult(Guid Id, int RateLimitPerMinute);

public interface IApiKeyService
{
    Task<ApiKeyCreateResult> CreateAsync(string name, CancellationToken cancellationToken);

    Task<ApiKeyValidationResult?> ValidateAsync(string plaintextKey, CancellationToken cancellationToken);

    Task<bool> AnyActiveKeysAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ApiKeySummary>> ListAsync(CancellationToken cancellationToken);

    Task<bool> RevokeAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class ApiKeyService(
    QikLogDbContext db,
    ApiKeyHasher hasher,
    IOptions<IngestAuthOptions> options,
    ILogger<ApiKeyService> log) : IApiKeyService
{
    public async Task<ApiKeyCreateResult> CreateAsync(string name, CancellationToken cancellationToken)
    {
        var (plaintext, lookupPrefix) = ApiKeyFormat.Generate();
        var entity = new ApiKeyEntity
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            LookupPrefix = lookupPrefix,
            SecretHash = hasher.Hash(plaintext),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            RateLimitPerMinute = options.Value.RateLimitPerMinute
        };

        db.ApiKeys.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        log.LogInformation("Created API key {ApiKeyId} with prefix {Prefix}", entity.Id, lookupPrefix);
        return new ApiKeyCreateResult(entity.Id, plaintext, entity.Name);
    }

    public async Task<ApiKeyValidationResult?> ValidateAsync(string plaintextKey, CancellationToken cancellationToken)
    {
        if (!ApiKeyFormat.TryGetLookupPrefix(plaintextKey, out var prefix))
            return null;

        var candidates = await db.ApiKeys
            .Where(k => k.LookupPrefix == prefix && k.IsActive && k.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var candidate in candidates)
        {
            if (!hasher.Verify(plaintextKey, candidate.SecretHash))
                continue;

            candidate.LastUsedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            return new ApiKeyValidationResult(candidate.Id, candidate.RateLimitPerMinute);
        }

        return null;
    }

    public Task<bool> AnyActiveKeysAsync(CancellationToken cancellationToken) =>
        db.ApiKeys.AnyAsync(k => k.IsActive && k.RevokedAt == null, cancellationToken);

    public async Task<IReadOnlyList<ApiKeySummary>> ListAsync(CancellationToken cancellationToken) =>
        await db.ApiKeys
            .AsNoTracking()
            .OrderByDescending(k => k.CreatedAt)
            .Select(k => new ApiKeySummary(
                k.Id,
                k.Name,
                k.LookupPrefix,
                k.IsActive && k.RevokedAt == null,
                k.CreatedAt,
                k.LastUsedAt,
                k.RevokedAt,
                k.RateLimitPerMinute))
            .ToListAsync(cancellationToken);

    public async Task<bool> RevokeAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.ApiKeys.FindAsync([id], cancellationToken);
        if (entity is null)
            return false;

        if (entity.RevokedAt is not null)
            return true;

        entity.IsActive = false;
        entity.RevokedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        log.LogInformation("Revoked API key {ApiKeyId}", id);
        return true;
    }
}
