using Microsoft.AspNetCore.SignalR;
using QikLog.Api.Hubs;
using QikLog.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    // Permissive for local dev. Lock down per-environment in Program startup later.
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5081", "https://localhost:5443")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

// POST /v1/logs - ingest endpoint
app.MapPost("/v1/logs", async (
    LogEntryDto dto,
    IHubContext<LogHub> hub,
    CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(dto.Source))
        return Results.BadRequest(new { error = "source is required" });
    if (string.IsNullOrWhiteSpace(dto.Message))
        return Results.BadRequest(new { error = "message is required" });

    var entry = new LogEntry(
        Source: dto.Source.Trim(),
        Level: dto.Level ?? LogLevel.Info,
        Message: dto.Message,
        Timestamp: dto.Timestamp ?? DateTimeOffset.UtcNow,
        Properties: dto.Properties
    );

    // Broadcast to anyone subscribed to this source.
    // Group name convention: "source:{name}". Keep it grep-able.
    await hub.Clients
        .Group($"source:{entry.Source}")
        .SendAsync("LogReceived", entry, ct);

    return Results.Accepted();
})
.WithName("IngestLog")
.WithOpenApi();

// GET /healthz - for container orchestrators
app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }))
   .WithName("Health");

app.MapHub<LogHub>("/hubs/logs");

app.Run();

/// <summary>
/// Wire shape for POST /v1/logs. Looser than <see cref="LogEntry"/> because we
/// accept missing fields and apply defaults server-side.
/// </summary>
public sealed record LogEntryDto(
    string Source,
    string Message,
    LogLevel? Level,
    DateTimeOffset? Timestamp,
    IReadOnlyDictionary<string, string>? Properties
);
