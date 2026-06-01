namespace QikLog.Infrastructure.Auth;

/// <summary>Used when Postgres is not configured (auth and keys unavailable).</summary>
public sealed class NullApiKeyService : IApiKeyService
{
    public Task<ApiKeyCreateResult> CreateAsync(string name, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("API keys require a Postgres connection string.");

    public Task<ApiKeyValidationResult?> ValidateAsync(string plaintextKey, CancellationToken cancellationToken) =>
        Task.FromResult<ApiKeyValidationResult?>(null);

    public Task<bool> AnyActiveKeysAsync(CancellationToken cancellationToken) =>
        Task.FromResult(false);
}
