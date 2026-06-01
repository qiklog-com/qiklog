using Microsoft.Extensions.Options;
using QikLog.Core;
using QikLog.Infrastructure.Auth;

namespace QikLog.Api.Middleware;

/// <summary>Validates API keys and applies per-key rate limits for <c>POST /v1/logs</c>.</summary>
public sealed class IngestApiKeyMiddleware(
    RequestDelegate next,
    IOptions<IngestAuthOptions> options,
    IApiKeyService apiKeys,
    ApiKeyRateLimiter rateLimiter,
    IIngestContext ingestContext,
    ILogger<IngestApiKeyMiddleware> log)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsIngestPost(context))
        {
            await next(context);
            return;
        }

        if (!options.Value.RequireApiKey)
        {
            await next(context);
            return;
        }

        if (!TryGetApiKeyFromRequest(context.Request, out var plaintext))
        {
            await WriteErrorAsync(context, StatusCodes.Status401Unauthorized, "missing or invalid Authorization header (Bearer ql_...)");
            return;
        }

        var validation = await apiKeys.ValidateAsync(plaintext, context.RequestAborted);
        if (validation is null)
        {
            await WriteErrorAsync(context, StatusCodes.Status401Unauthorized, "invalid API key");
            return;
        }

        if (!rateLimiter.TryAcquire(validation.Id, validation.RateLimitPerMinute))
        {
            await WriteErrorAsync(context, StatusCodes.Status429TooManyRequests, "rate limit exceeded");
            return;
        }

        ingestContext.ApiKeyId = validation.Id;
        log.LogDebug("Ingest authorized for API key {ApiKeyId}", validation.Id);
        await next(context);
    }

    private static bool IsIngestPost(HttpContext context) =>
        HttpMethods.IsPost(context.Request.Method)
        && context.Request.Path.StartsWithSegments("/v1/logs", StringComparison.OrdinalIgnoreCase);

    private static bool TryGetApiKeyFromRequest(HttpRequest request, out string plaintext)
    {
        plaintext = "";

        if (request.Headers.TryGetValue("Authorization", out var auth))
        {
            var value = auth.ToString();
            if (value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                plaintext = value["Bearer ".Length..].Trim();
                return ApiKeyFormat.TryGetLookupPrefix(plaintext, out _);
            }
        }

        if (request.Headers.TryGetValue("X-Api-Key", out var header))
        {
            plaintext = header.ToString().Trim();
            return ApiKeyFormat.TryGetLookupPrefix(plaintext, out _);
        }

        return false;
    }

    private static async Task WriteErrorAsync(HttpContext context, int statusCode, string message)
    {
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(new { error = message });
    }
}
