using Microsoft.Extensions.Options;
using QikLog.Api.OpenApi;
using QikLog.Core.Management;
using QikLog.Infrastructure.Auth;
using QikLog.Infrastructure.Sources;

namespace QikLog.Api;

internal static class ManagementEndpoints
{
    public static void MapQikLogManagement(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<ManagementOptions>>().Value;
        if (!options.Enabled)
            return;

        var group = app.MapGroup("/v1");

        group.MapGet("/keys", async (IApiKeyService keys, CancellationToken ct) =>
        {
            var list = await keys.ListAsync(ct);
            return Results.Ok(list);
        })
        .WithName("ListApiKeys")
        .WithOpenApiMetadata(
            OpenApiTags.Auth,
            "List API keys",
            "Returns API key metadata (never includes secrets). Requires `QikLog:Management:Enabled`.")
        .Produces<IReadOnlyList<ApiKeySummary>>(StatusCodes.Status200OK);

        group.MapPost("/keys", async (CreateApiKeyRequest request, IApiKeyService keys, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return Results.BadRequest(new { error = "name is required" });

            var created = await keys.CreateAsync(request.Name, ct);
            return Results.Created($"/v1/keys/{created.Id}", new
            {
                id = created.Id,
                name = created.Name,
                key = created.Plaintext,
                hint = "Save this key now. It will not be shown again. Use: Authorization: Bearer <key>"
            });
        })
        .WithName("CreateApiKey")
        .WithOpenApiMetadata(
            OpenApiTags.Auth,
            "Create API key",
            "Creates a new ingest API key. The plaintext `key` is returned once in the response body.")
        .Accepts<CreateApiKeyRequest>("application/json")
        .Produces<CreateApiKeyResponse>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/keys/{id:guid}/revoke", async (Guid id, IApiKeyService keys, CancellationToken ct) =>
        {
            var revoked = await keys.RevokeAsync(id, ct);
            return revoked ? Results.NoContent() : Results.NotFound();
        })
        .WithName("RevokeApiKey")
        .WithOpenApiMetadata(
            OpenApiTags.Auth,
            "Revoke API key",
            "Deactivates an API key. Ingest with that key returns 401 afterward.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/sources", async (ISourceCatalog sources, CancellationToken ct) =>
        {
            var list = await sources.ListAsync(ct);
            return Results.Ok(list);
        })
        .WithName("ListSources")
        .WithOpenApiMetadata(
            OpenApiTags.Sources,
            "List sources",
            "Lists distinct source names seen in persisted `log_entries` with counts and last-seen timestamps.")
        .Produces<IReadOnlyList<SourceSummary>>(StatusCodes.Status200OK);
    }
}
