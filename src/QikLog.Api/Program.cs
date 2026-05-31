using Microsoft.AspNetCore.SignalR;
using QikLog.Api.Hubs;
using QikLog.Core;
using CoreLogLevel = QikLog.Core.LogLevel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
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
        Level: dto.Level ?? CoreLogLevel.Info,
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
.WithName("IngestLog");

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
    CoreLogLevel? Level,
    DateTimeOffset? Timestamp,
    IReadOnlyDictionary<string, string>? Properties
);

/// <summary>Entry point type for integration tests.</summary>
public partial class Program;
