using QikLog.Api;
using QikLog.Api.Observability;
using QikLog.Api.OpenApi;
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
builder.Services.AddQikLogOpenApi(builder.Configuration);
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

app.UseQikLogOpenApi();
app.UseQikLogObservability();

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
    IUsageLimitService usage,
    ILogger<IngestEndpoint> log,
    CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(dto.Source))
    {
        log.LogWarning("Ingest rejected: missing source");
        return Results.BadRequest(new { error = "source is required" });
    }

    if (string.IsNullOrWhiteSpace(dto.Message))
    {
        log.LogWarning("Ingest rejected: missing message for source {Source}", dto.Source);
        return Results.BadRequest(new { error = "message is required" });
    }

    var usageCheck = await usage.CheckIngestAllowedAsync(ct);
    if (!usageCheck.Allowed)
    {
        QikLogMetrics.UsageLimitChecks.WithLabels("denied").Inc();
        log.LogWarning(
            "Ingest blocked for source {Source}: {Reason} ({Count}/{Limit})",
            dto.Source,
            usageCheck.Reason,
            usageCheck.Count,
            usageCheck.Limit);
        return Results.Json(
            new { error = usageCheck.Reason, usage = usageCheck.Count, limit = usageCheck.Limit },
            statusCode: 402);
    }

    QikLogMetrics.UsageLimitChecks.WithLabels("allowed").Inc();

    var entry = new LogEntry(
        Source: dto.Source.Trim(),
        Level: dto.Level ?? CoreLogLevel.Info,
        Message: dto.Message,
        Timestamp: dto.Timestamp ?? DateTimeOffset.UtcNow,
        Properties: dto.Properties
    );

    await store.SaveAsync(entry, ct);
    QikLogMetrics.LogsIngested.Inc();

    await hub.Clients
        .Group($"source:{entry.Source}")
        .SendAsync("LogReceived", entry, ct);

    log.LogInformation("Ingested log for source {Source} level {Level}", entry.Source, entry.Level);
    return Results.Accepted();
})
.WithName("IngestLog")
.WithOpenApiMetadata(
    OpenApiTags.Logs,
    "Ingest a log entry",
    "Accepts a JSON log line, persists it when Postgres is configured, and broadcasts to live tail subscribers via SignalR. " +
    "Requires an API key when `QikLog:Ingest:RequireApiKey` is true.")
.Accepts<LogEntryDto>("application/json")
.Produces(StatusCodes.Status202Accepted)
.ProducesProblem(StatusCodes.Status400BadRequest)
.ProducesProblem(StatusCodes.Status401Unauthorized)
.ProducesProblem(StatusCodes.Status402PaymentRequired)
.ProducesProblem(StatusCodes.Status429TooManyRequests);

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
    .WithName("CreateDevApiKey")
    .WithOpenApiMetadata(
        OpenApiTags.Auth,
        "Create API key (development alias)",
        "Development-only alias for `POST /v1/keys`. Returns the plaintext key once.")
    .Accepts<CreateApiKeyRequest>("application/json")
    .Produces<CreateApiKeyResponse>(StatusCodes.Status201Created)
    .ProducesProblem(StatusCodes.Status400BadRequest);
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
.WithName("Health")
.WithSummary("Health check")
.WithDescription("Container orchestrator probe. Reports Postgres connectivity when persistence is enabled.");

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
.WithName("GetSourceLogs")
.WithOpenApiMetadata(
    OpenApiTags.Logs,
    "Get recent logs for a source",
    "Returns persisted log entries for the source, oldest-first, up to `limit` (max 500). Empty array when persistence is disabled.")
.Produces<IReadOnlyList<LogEntry>>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status400BadRequest);

app.MapHub<LogHub>("/hubs/logs");
app.MapQikLogObservability();

app.Run();

/// <summary>Logger category for POST /v1/logs.</summary>
internal sealed class IngestEndpoint;

public sealed record CreateApiKeyRequest(string Name);

/// <summary>Response body when an API key is created (plaintext shown once).</summary>
public sealed record CreateApiKeyResponse(
    Guid Id,
    string Name,
    string Key,
    string Hint);

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
