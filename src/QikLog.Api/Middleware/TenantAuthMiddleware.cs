using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using QikLog.Api.Auth;
using QikLog.Api.Auth.Testing;
using QikLog.Core;
using QikLog.Infrastructure;
using QikLog.Infrastructure.Auth;
using QikLog.Infrastructure.Tenants;

namespace QikLog.Api.Middleware;

/// <summary>Enforces JWT and/or API-key authentication and sets <see cref="ITenantContext"/>.</summary>
public sealed class TenantAuthMiddleware(
    RequestDelegate next,
    IOptions<AuthEnforcementOptions> enforcementOptions,
    IOptions<ManagementOptions> managementOptions,
    ILogger<TenantAuthMiddleware> log)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (ProtectedApiRoutes.IsPublic(context.Request.Path))
        {
            await next(context);
            return;
        }

        var store = context.RequestServices.GetRequiredService<ILogEntryStore>();
        if (!store.IsEnabled || !enforcementOptions.Value.Enabled)
        {
            await next(context);
            return;
        }

        if (!ProtectedApiRoutes.TryGetAuthMode(
                context.Request.Path,
                context.Request.Method,
                out var authMode))
        {
            await next(context);
            return;
        }

        if (authMode == AuthMode.Jwt && !managementOptions.Value.Enabled
            && context.Request.Path.StartsWithSegments("/v1/keys", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        if (authMode == AuthMode.Jwt && !managementOptions.Value.Enabled
            && context.Request.Path.StartsWithSegments("/v1/sources", StringComparison.OrdinalIgnoreCase)
            && context.Request.Path.Value?.EndsWith("/logs", StringComparison.OrdinalIgnoreCase) != true)
        {
            await next(context);
            return;
        }

        if (authMode.HasFlag(AuthMode.Jwt))
        {
            var jwtResult = await TryResolveJwtTenantAsync(context);
            if (jwtResult == JwtTenantResult.Success)
            {
                await next(context);
                return;
            }

            if (authMode == AuthMode.Jwt)
            {
                await WriteAuthErrorAsync(context, jwtResult);
                return;
            }
        }

        if (authMode.HasFlag(AuthMode.ApiKey))
        {
            var apiKeyResult = await TryResolveApiKeyTenantAsync(context);
            if (apiKeyResult == ApiKeyTenantResult.Success)
            {
                await next(context);
                return;
            }

            if (apiKeyResult == ApiKeyTenantResult.RateLimited)
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsJsonAsync(new { error = "rate limit exceeded" });
                return;
            }

            await WriteApiKeyErrorAsync(context, apiKeyResult);
            return;
        }

        await next(context);
    }

    private enum JwtTenantResult
    {
        Success,
        Missing,
        Invalid,
        TenantNotFound
    }

    private enum ApiKeyTenantResult
    {
        Success,
        Missing,
        Invalid,
        TenantNotFound,
        RateLimited
    }

    private static async Task<JwtTenantResult> TryResolveJwtTenantAsync(HttpContext context)
    {
        var authenticate = await context.AuthenticateAsync();
        if (!authenticate.Succeeded || authenticate.Principal is null)
            return JwtTenantResult.Missing;

        var resolver = context.RequestServices.GetRequiredService<TenantResolver>();
        var resolution = await resolver.ResolveFromPrincipalAsync(
            authenticate.Principal,
            context.RequestAborted);

        if (resolution.Status == TenantResolutionStatus.Unauthenticated)
            return JwtTenantResult.Missing;
        if (resolution.Status == TenantResolutionStatus.TenantNotFound)
            return JwtTenantResult.TenantNotFound;

        context.RequestServices.GetRequiredService<ITenantContext>().TenantId = resolution.TenantId;
        return JwtTenantResult.Success;
    }

    private static async Task<ApiKeyTenantResult> TryResolveApiKeyTenantAsync(HttpContext context)
    {
        if (!TryGetApiKeyFromRequest(context.Request, out var plaintext))
            return ApiKeyTenantResult.Missing;

        var apiKeys = context.RequestServices.GetRequiredService<IApiKeyService>();
        var validation = await apiKeys.ValidateAsync(plaintext, context.RequestAborted);
        if (validation is null)
            return ApiKeyTenantResult.Invalid;

        if (validation.TenantId is null)
            return ApiKeyTenantResult.TenantNotFound;

        var rateLimiter = context.RequestServices.GetRequiredService<ApiKeyRateLimiter>();
        if (context.Request.Path.StartsWithSegments("/v1/logs", StringComparison.OrdinalIgnoreCase)
            && !rateLimiter.TryAcquire(validation.Id, validation.RateLimitPerMinute))
            return ApiKeyTenantResult.RateLimited;

        var ingestContext = context.RequestServices.GetRequiredService<IIngestContext>();
        ingestContext.ApiKeyId = validation.Id;
        context.RequestServices.GetRequiredService<ITenantContext>().TenantId = validation.TenantId;
        return ApiKeyTenantResult.Success;
    }

    private static bool TryGetApiKeyFromRequest(HttpRequest request, out string plaintext)
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

    private async Task WriteAuthErrorAsync(HttpContext context, JwtTenantResult result)
    {
        switch (result)
        {
            case JwtTenantResult.Missing:
            case JwtTenantResult.Invalid:
                log.LogWarning("JWT auth rejected for {Path}: {Reason}", context.Request.Path, result);
                await WriteErrorAsync(context, StatusCodes.Status401Unauthorized, "authentication required");
                break;
            case JwtTenantResult.TenantNotFound:
                log.LogWarning("JWT auth tenant not found for {Path}", context.Request.Path);
                await WriteErrorAsync(context, StatusCodes.Status403Forbidden, "tenant not found or not provisioned");
                break;
        }
    }

    private async Task WriteApiKeyErrorAsync(HttpContext context, ApiKeyTenantResult result)
    {
        switch (result)
        {
            case ApiKeyTenantResult.Missing:
                log.LogWarning("API key missing for {Path}", context.Request.Path);
                await WriteErrorAsync(
                    context,
                    StatusCodes.Status401Unauthorized,
                    "missing API key (header X-QikLog-API-Key)");
                break;
            case ApiKeyTenantResult.Invalid:
                log.LogWarning("API key invalid for {Path}", context.Request.Path);
                await WriteErrorAsync(context, StatusCodes.Status403Forbidden, "invalid or revoked API key");
                break;
            case ApiKeyTenantResult.TenantNotFound:
                log.LogWarning("API key has no tenant for {Path}", context.Request.Path);
                await WriteErrorAsync(context, StatusCodes.Status403Forbidden, "API key is not associated with a tenant");
                break;
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, int statusCode, string message)
    {
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(new { error = message });
    }
}
