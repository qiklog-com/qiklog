using Microsoft.AspNetCore.Authentication;
using QikLog.Core;
using QikLog.Infrastructure.Auth;
using QikLog.Infrastructure.Tenants;

namespace QikLog.Api.Auth;

public enum TenantAuthFailure
{
    None,
    MissingCredentials,
    InvalidCredentials,
    TenantNotFound,
    RateLimited
}

public sealed record TenantAuthSuccess(Guid TenantId, Guid? ApiKeyId);

public sealed class TenantAuthenticationService(
    IApiKeyService apiKeys,
    TenantResolver tenantResolver,
    ApiKeyRateLimiter rateLimiter)
{
    public async Task<(TenantAuthSuccess? Success, TenantAuthFailure Failure)> AuthenticateAsync(
        HttpContext context,
        AuthMode mode,
        bool applyIngestRateLimit,
        CancellationToken cancellationToken)
    {
        if (mode.HasFlag(AuthMode.Jwt))
        {
            var jwt = await TryJwtAsync(context, cancellationToken);
            if (jwt.Success is not null)
                return jwt;

            if (mode == AuthMode.Jwt)
                return jwt;
        }

        if (mode.HasFlag(AuthMode.ApiKey))
            return await TryApiKeyAsync(context, applyIngestRateLimit, cancellationToken);

        return (null, TenantAuthFailure.MissingCredentials);
    }

    private async Task<(TenantAuthSuccess? Success, TenantAuthFailure Failure)> TryJwtAsync(
        HttpContext context,
        CancellationToken cancellationToken)
    {
        // Enforcement can be on while OIDC is off (invite-only / API-key-only). In that
        // case AddAuthentication was never called; AuthenticateAsync would throw and
        // turn every JwtOrApiKey route (history, hub) into a 500 instead of falling
        // through to the API-key path.
        if (context.RequestServices.GetService<IAuthenticationService>() is null)
            return (null, TenantAuthFailure.MissingCredentials);

        var authenticate = await context.AuthenticateAsync();
        if (!authenticate.Succeeded || authenticate.Principal is null)
            return (null, TenantAuthFailure.MissingCredentials);

        var resolution = await tenantResolver.ResolveFromPrincipalAsync(
            authenticate.Principal,
            cancellationToken);

        return resolution.Status switch
        {
            TenantResolutionStatus.Success => (new TenantAuthSuccess(resolution.TenantId!.Value, null), TenantAuthFailure.None),
            TenantResolutionStatus.TenantNotFound => (null, TenantAuthFailure.TenantNotFound),
            _ => (null, TenantAuthFailure.MissingCredentials)
        };
    }

    private async Task<(TenantAuthSuccess? Success, TenantAuthFailure Failure)> TryApiKeyAsync(
        HttpContext context,
        bool applyIngestRateLimit,
        CancellationToken cancellationToken)
    {
        if (!TryGetApiKeyFromRequest(context.Request, out var plaintext))
            return (null, TenantAuthFailure.MissingCredentials);

        var validation = await apiKeys.ValidateAsync(plaintext, cancellationToken);
        if (validation is null)
            return (null, TenantAuthFailure.InvalidCredentials);

        if (validation.TenantId is null)
            return (null, TenantAuthFailure.TenantNotFound);

        if (applyIngestRateLimit
            && context.Request.Path.StartsWithSegments("/v1/logs", StringComparison.OrdinalIgnoreCase)
            && !rateLimiter.TryAcquire(validation.Id, validation.RateLimitPerMinute))
            return (null, TenantAuthFailure.RateLimited);

        return (new TenantAuthSuccess(validation.TenantId.Value, validation.Id), TenantAuthFailure.None);
    }

    public static bool TryGetApiKeyFromRequest(HttpRequest request, out string plaintext)
    {
        plaintext = "";

        if (request.Headers.TryGetValue("X-QikLog-API-Key", out var primary))
        {
            plaintext = primary.ToString().Trim();
            return ApiKeyFormat.TryGetLookupPrefix(plaintext, out _);
        }

        if (request.Headers.TryGetValue("X-Api-Key", out var legacy))
        {
            plaintext = legacy.ToString().Trim();
            return ApiKeyFormat.TryGetLookupPrefix(plaintext, out _);
        }

        if (request.Query.TryGetValue("api_key", out var queryKey))
        {
            plaintext = queryKey.ToString().Trim();
            return ApiKeyFormat.TryGetLookupPrefix(plaintext, out _);
        }

        if (request.Headers.TryGetValue("Authorization", out var auth))
        {
            var value = auth.ToString();
            if (value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                plaintext = value["Bearer ".Length..].Trim();
                if (ApiKeyFormat.TryGetLookupPrefix(plaintext, out _))
                    return true;
            }
        }

        return false;
    }
}
