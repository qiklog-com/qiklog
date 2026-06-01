using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace QikLog.Api.OpenApi;

internal sealed class QikLogOpenApiOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var name = context.Description.ActionDescriptor.EndpointMetadata
            .OfType<Microsoft.AspNetCore.Routing.IEndpointNameMetadata>()
            .LastOrDefault()
            ?.EndpointName;

        if (name is null)
            return Task.CompletedTask;

        ApplyExamples(name, operation);
        ApplySecurity(name, operation);
        return Task.CompletedTask;
    }

    private static void ApplySecurity(string operationName, OpenApiOperation operation)
    {
        if (operationName is "IngestLog")
        {
            operation.Security =
            [
                new OpenApiSecurityRequirement
                {
                    [new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKeyBearer" }
                    }] = []
                }
            ];
        }

        if (operationName is "CreateCheckoutSession")
        {
            operation.Security =
            [
                new OpenApiSecurityRequirement
                {
                    [new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "OidcBearer" }
                    }] = []
                }
            ];
        }
    }

    private static void ApplyExamples(string operationName, OpenApiOperation operation)
    {
        switch (operationName)
        {
            case "IngestLog":
                SetJsonRequestExample(operation, OpenApiExamples.IngestRequest);
                SetJsonResponseExample(operation, "202", OpenApiExamples.IngestAccepted);
                SetJsonResponseExample(operation, "400", OpenApiExamples.ErrorBody);
                SetJsonResponseExample(operation, "402", OpenApiExamples.UsageLimitBody);
                break;
            case "GetSourceLogs":
                SetJsonResponseExample(operation, "200", OpenApiExamples.LogHistory);
                SetJsonResponseExample(operation, "400", OpenApiExamples.ErrorBody);
                break;
            case "ListSources":
                SetJsonResponseExample(operation, "200", OpenApiExamples.SourceList);
                break;
            case "ListApiKeys":
                SetJsonResponseExample(operation, "200", OpenApiExamples.ApiKeyList);
                break;
            case "CreateApiKey":
            case "CreateDevApiKey":
                SetJsonRequestExample(operation, OpenApiExamples.CreateKeyRequest);
                SetJsonResponseExample(operation, "201", OpenApiExamples.CreateKeyResponse);
                SetJsonResponseExample(operation, "400", OpenApiExamples.ErrorBody);
                break;
            case "CreateCheckoutSession":
                SetJsonResponseExample(operation, "200", OpenApiExamples.CheckoutResponse);
                SetJsonResponseExample(operation, "400", OpenApiExamples.ErrorBody);
                SetJsonResponseExample(operation, "401", OpenApiExamples.ErrorBody);
                break;
            case "RevokeApiKey":
                if (!operation.Responses.ContainsKey("204"))
                    operation.Responses["204"] = new OpenApiResponse { Description = "API key revoked." };
                SetJsonResponseExample(operation, "404", OpenApiExamples.ErrorBody);
                break;
            case "Health":
                SetJsonResponseExample(operation, "200", OpenApiExamples.HealthOk);
                SetJsonResponseExample(operation, "503", OpenApiExamples.HealthDegraded);
                break;
        }
    }

    private static void SetJsonRequestExample(OpenApiOperation operation, Microsoft.OpenApi.Any.IOpenApiAny example)
    {
        operation.RequestBody ??= new OpenApiRequestBody
        {
            Required = true,
            Content = new Dictionary<string, OpenApiMediaType>()
        };

        if (!operation.RequestBody.Content.ContainsKey("application/json"))
            operation.RequestBody.Content["application/json"] = new OpenApiMediaType();

        operation.RequestBody.Content["application/json"].Example = example;
    }

    private static void SetJsonResponseExample(OpenApiOperation operation, string statusCode, Microsoft.OpenApi.Any.IOpenApiAny example)
    {
        if (!operation.Responses.TryGetValue(statusCode, out var response))
            return;

        response.Content ??= new Dictionary<string, OpenApiMediaType>();
        if (!response.Content.ContainsKey("application/json"))
            response.Content["application/json"] = new OpenApiMediaType();

        response.Content["application/json"].Example = example;
    }
}
