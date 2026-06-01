using Microsoft.Extensions.Options;
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
        .WithName("ListApiKeys");

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
        .WithName("CreateApiKey");

        group.MapPost("/keys/{id:guid}/revoke", async (Guid id, IApiKeyService keys, CancellationToken ct) =>
        {
            var revoked = await keys.RevokeAsync(id, ct);
            return revoked ? Results.NoContent() : Results.NotFound();
        })
        .WithName("RevokeApiKey");

        group.MapGet("/sources", async (ISourceCatalog sources, CancellationToken ct) =>
        {
            var list = await sources.ListAsync(ct);
            return Results.Ok(list);
        })
        .WithName("ListSources");
    }
}
