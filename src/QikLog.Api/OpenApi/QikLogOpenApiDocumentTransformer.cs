using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace QikLog.Api.OpenApi;

/// <summary>Adds security schemes and documents endpoints that may be unmapped when features are disabled.</summary>
internal sealed class QikLogOpenApiDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Info = new OpenApiInfo
        {
            Title = "QikLog API",
            Version = "v1",
            Description =
                "Real-time log ingest, source catalog, API key management, and billing hooks for QikLog. " +
                "Tenants are provisioned via OIDC (Zitadel) when auth is enabled; see the Auth and Tenants tags."
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes = new Dictionary<string, OpenApiSecurityScheme>
        {
            ["ApiKeyHeader"] = new()
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = "X-QikLog-API-Key",
                Description = "Ingest and history API key: `X-QikLog-API-Key: ql_{prefix}_{secret}` (also accepts `X-Api-Key` or `Authorization: Bearer`)."
            },
            ["ApiKeyBearer"] = new()
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "API key",
                Description = "Legacy: API key as Bearer token."
            },
            ["OidcBearer"] = new()
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Zitadel access token when `QikLog:Auth:Enabled` is true."
            }
        };

        EnsurePath(
            document,
            "/v1/billing/checkout",
            "post",
            "CreateCheckoutSession",
            "Billing",
            "Create Stripe Checkout session",
            "Creates a Stripe Checkout session for Pro upgrade when `QikLog:Stripe:Enabled` and the caller is authenticated.",
            operation =>
            {
                operation.Tags = [new OpenApiTag { Name = "Billing" }, new OpenApiTag { Name = "Tenants" }];
                operation.Security = [new OpenApiSecurityRequirement { [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "OidcBearer" } }] = [] }];
                OpenApiOperationEnhancer.SetJsonResponseExample(operation, "200", OpenApiExamples.CheckoutResponse);
                OpenApiOperationEnhancer.SetJsonResponseExample(operation, "401", OpenApiExamples.ErrorBody);
            });

        return Task.CompletedTask;
    }

    private static void EnsurePath(
        OpenApiDocument document,
        string path,
        string method,
        string operationId,
        string tag,
        string summary,
        string description,
        Action<OpenApiOperation>? configure = null)
    {
        document.Paths ??= new OpenApiPaths();
        if (document.Paths.ContainsKey(path))
            return;

        var operation = new OpenApiOperation
        {
            OperationId = operationId,
            Summary = summary,
            Description = description,
            Tags = [new OpenApiTag { Name = tag }],
            Responses = new OpenApiResponses
            {
                ["404"] = new OpenApiResponse { Description = "Not available (feature disabled in this environment)." }
            }
        };

        configure?.Invoke(operation);

        document.Paths[path] = new OpenApiPathItem
        {
            Operations = new Dictionary<OperationType, OpenApiOperation>
            {
                [Enum.Parse<OperationType>(method, ignoreCase: true)] = operation
            }
        };
    }
}

internal static class OpenApiOperationEnhancer
{
    public static void SetJsonResponseExample(OpenApiOperation operation, string statusCode, Microsoft.OpenApi.Any.IOpenApiAny example)
    {
        if (!operation.Responses.ContainsKey(statusCode))
            operation.Responses[statusCode] = new OpenApiResponse { Description = statusCode };

        var response = operation.Responses[statusCode];
        response.Content ??= new Dictionary<string, OpenApiMediaType>();
        if (!response.Content.ContainsKey("application/json"))
            response.Content["application/json"] = new OpenApiMediaType();

        response.Content["application/json"].Example = example;
    }
}
