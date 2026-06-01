using QikLog.Api;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using QikLog.Api.Hubs;
using QikLog.Api.Middleware;
using QikLog.Core;
using QikLog.Infrastructure;
using Microsoft.Extensions.Options;
using QikLog.Infrastructure.Auth;
using QikLog.Infrastructure.Billing;
using QikLog.Infrastructure.Data;
using CoreLogLevel = QikLog.Core.LogLevel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddQikLogPersistence(builder.Configuration, builder.Environment);
builder.Services.AddQikLogJwtAuth(builder.Configuration);
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new LogLevelJsonConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? ["http://localhost:5081", "https://localhost:5443"];

    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

var app = builder.Build();

await app.Services.MigrateQikLogDatabaseAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
var authOptions = app.Services.GetRequiredService<IOptions<QikLogAuthOptions>>().Value;
if (authOptions.Enabled && !string.IsNullOrWhiteSpace(authOptions.Authority))
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.UseMiddleware<IngestApiKeyMiddleware>();

// POST /v1/logs - ingest endpoint
app.MapPost("/v1/logs", async (
    LogEntryDto dto,
    IHubContext<LogHub> hub,
    ILogEntryStore store,
    IServiceProvider sp,
    CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(dto.Source))
        return Results.BadRequest(new { error = "source is required" });
    if (string.IsNullOrWhiteSpace(dto.Message))
        return Results.BadRequest(new { error = "message is required" });

    var usage = sp.GetRequiredService<IUsageLimitService>();
    var usageCheck = await usage.CheckIngestAllowedAsync(ct);
    if (!usageCheck.Allowed)
        return Results.Json(new { error = usageCheck.Reason, usage = usageCheck.Count, limit = usageCheck.Limit }, statusCode: 402);

    var entry = new LogEntry(
        Source: dto.Source.Trim(),
        Level: dto.Level ?? CoreLogLevel.Info,
        Message: dto.Message,
        Timestamp: dto.Timestamp ?? DateTimeOffset.UtcNow,
        Properties: dto.Properties
    );

    await store.SaveAsync(entry, ct);

    await hub.Clients
        .Group($"source:{entry.Source}")
        .SendAsync("LogReceived", entry, ct);

    return Results.Accepted();
})
.WithName("IngestLog");

app.MapQikLogManagement();
app.MapQikLogBilling();

// Back-compat alias for CLI/scripts when management API is enabled.
if (app.Environment.IsDevelopment())
{
    app.MapPost("/v1/dev/keys", async (CreateApiKeyRequest request, IApiKeyService keys, CancellationToken ct) =>
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
    .WithName("CreateDevApiKey");
}

// GET /healthz - for container orchestrators
app.MapGet("/healthz", async (IServiceProvider sp, CancellationToken ct) =>
{
    var store = sp.GetRequiredService<ILogEntryStore>();
    if (!store.IsEnabled)
        return Results.Ok(new { status = "ok", postgres = "skipped" });

    try
    {
        await using var scope = sp.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QikLogDbContext>();
        var canConnect = await db.Database.CanConnectAsync(ct);
        return canConnect
            ? Results.Ok(new { status = "ok", postgres = "ok" })
            : Results.Json(new { status = "degraded", postgres = "unreachable" }, statusCode: 503);
    }
    catch
    {
        return Results.Json(new { status = "degraded", postgres = "error" }, statusCode: 503);
    }
})
.WithName("Health");

app.MapGet("/v1/sources/{source}/logs", async (
    string source,
    int? limit,
    ILogHistoryService history,
    CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(source))
        return Results.BadRequest(new { error = "source is required" });

    if (!history.IsEnabled)
        return Results.Ok(Array.Empty<LogEntry>());

    var entries = await history.GetRecentBySourceAsync(source.Trim(), limit ?? 100, ct);
    return Results.Ok(entries);
})
.WithName("GetSourceLogs");

app.MapHub<LogHub>("/hubs/logs");

app.Run();

public sealed record CreateApiKeyRequest(string Name);

/// <summary>
/// Wire shape for POST /v1/logs. Looser than <see cref="LogEntry"/> because we
/// accept missing fields and apply defaults server-side.
/// </summary>
public sealed record LogEntryDto(
    string Source,
    string Message,
    CoreLogLevel? Level,
    DateTimeOffset? Timestamp,
    IReadOnlyDictionary<string, string>? Properties
);

/// <summary>Entry point type for integration tests.</summary>
public partial class Program;
