using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using QikLog.Api.Hubs;
using QikLog.Api.Observability;
using QikLog.Api.OpenApi;
using QikLog.Core;
using QikLog.Core.Links;
using QikLog.Infrastructure.Links;
using CoreLogLevel = QikLog.Core.LogLevel;

namespace QikLog.Api.Links;

/// <summary>
/// Anonymous log links: <c>POST /l</c> makes one, <c>POST /l/{id}</c> appends a line,
/// <c>GET /l/{id}</c> reads it back. No account or key: the id is the secret.
/// See docs/specs/log-link.md.
/// </summary>
internal static class LinkEndpoints
{
    /// <summary>Any origin, no credentials: links are meant to be called from anywhere.</summary>
    public const string CorsPolicy = "QikLogLinks";

    private const int DefaultReadLimit = 100;
    private const int SweepBatch = 10;

    public static IEndpointRouteBuilder MapQikLogLinks(this IEndpointRouteBuilder app)
    {
        app.MapPost("/l", CreateAsync)
            .WithName("CreateLogLink")
            .RequireCors(CorsPolicy)
            .WithOpenApiMetadata(
                OpenApiTags.Links,
                "Create a log link",
                "Makes an anonymous log link. No account or API key. The link lasts 24 hours and holds 500 lines.")
            .Produces<LogLinkCreated>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapPost("/l/{id}", AppendAsync)
            .WithName("AppendLogLinkLine")
            .RequireCors(CorsPolicy)
            .WithOpenApiMetadata(
                OpenApiTags.Links,
                "Send a line to a log link",
                "The request body is the line, as plain text. JSON with a `message` field also works. " +
                "A multi-line body becomes one line each.")
            .Produces<LogLinkAccepted>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status402PaymentRequired)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status410Gone)
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge);

        app.MapGet("/l/{id}", ReadOrRedirectAsync)
            .WithName("OpenLogLink")
            .RequireCors(CorsPolicy)
            .WithOpenApiMetadata(
                OpenApiTags.Links,
                "Open a log link",
                "A browser is redirected to the live tail. Anything else gets the lines as plain text.");

        app.MapGet("/l/{id}/raw", RawAsync)
            .WithName("ReadLogLinkText")
            .RequireCors(CorsPolicy)
            .WithOpenApiMetadata(
                OpenApiTags.Links,
                "Read a log link as text",
                "The newest lines, oldest first, one per line. `last` sets how many (default 100, max 500).");

        app.MapGet("/l/{id}/lines", LinesAsync)
            .WithName("ReadLogLinkLines")
            .RequireCors(CorsPolicy)
            .WithOpenApiMetadata(
                OpenApiTags.Links,
                "Read a log link as JSON",
                "The newest lines, oldest first, in the same shape as source history.")
            .Produces<IReadOnlyList<LogEntry>>(StatusCodes.Status200OK);

        return app;
    }

    private static async Task<IResult> CreateAsync(
        HttpContext http,
        ILogLinkService links,
        LinkCreateLimiter limiter,
        IOptions<LogLinkOptions> options,
        CancellationToken ct)
    {
        if (!links.IsEnabled || !options.Value.Enabled)
            return Error(StatusCodes.Status503ServiceUnavailable, "log links are not available on this server");

        var client = http.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (!limiter.TryAcquire(client, options.Value.CreatesPerIpPerHour))
            return Error(StatusCodes.Status429TooManyRequests, "too many new links from this address; try again next hour");

        await links.PurgeExpiredAsync(SweepBatch, ct);
        var link = await links.CreateAsync(ct);
        var url = $"{PublicBase(http, options.Value)}/l/{link.Id}";

        return Results.Json(
            new LogLinkCreated(
                link.Id,
                url,
                $"{url}/raw",
                link.ExpiresAt,
                options.Value.MaxLines,
                $"curl {url} -d \"it broke\""),
            statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> AppendAsync(
        string id,
        string? level,
        HttpContext http,
        ILogLinkService links,
        IHubContext<LogHub> hub,
        IOptions<LogLinkOptions> options,
        CancellationToken ct)
    {
        if (!links.IsEnabled || !options.Value.Enabled)
            return Error(StatusCodes.Status503ServiceUnavailable, "log links are not available on this server");

        if (!LogLinkId.IsValid(id))
            return NotFound();

        var limits = options.Value;
        var maxChars = (limits.MaxLineLength + 2) * limits.MaxLinesPerRequest;
        var body = await ReadBodyAsync(http.Request, maxChars, ct);
        if (body is null)
            return Error(StatusCodes.Status413PayloadTooLarge, $"too much in one request; a line is at most {limits.MaxLineLength} characters");

        var parsed = LinkLineParser.Parse(
            body,
            http.Request.ContentType,
            level,
            limits.MaxLineLength,
            limits.MaxLinesPerRequest);

        switch (parsed.Error)
        {
            case LinkLineError.Empty:
                return Error(StatusCodes.Status400BadRequest, "put the line in the request body, e.g. -d \"it broke\"");
            case LinkLineError.TooLong:
                return Error(StatusCodes.Status413PayloadTooLarge, $"a line is at most {limits.MaxLineLength} characters");
            case LinkLineError.TooManyLines:
                return Error(StatusCodes.Status413PayloadTooLarge, $"at most {limits.MaxLinesPerRequest} lines in one request");
        }

        var result = await links.AppendAsync(id, parsed.Lines, ct);
        switch (result.Status)
        {
            case LogLinkStatus.NotFound:
                return NotFound();
            case LogLinkStatus.Expired:
                return Expired();
            case LogLinkStatus.Full:
                return Error(
                    StatusCodes.Status402PaymentRequired,
                    $"this link is full ({limits.MaxLines} lines). Reading still works. Create a free account to keep logging.");
        }

        var group = LogHubGroups.ForLink(id);
        foreach (var entry in result.Entries)
        {
            QikLogMetrics.LogsIngested.Inc();
            await hub.Clients.Group(group).SendAsync("LogReceived", entry, ct);
        }

        return Results.Json(
            new LogLinkAccepted(result.Entries.Count, result.Remaining),
            statusCode: StatusCodes.Status202Accepted);
    }

    private static async Task<IResult> ReadOrRedirectAsync(
        string id,
        int? last,
        HttpContext http,
        ILogLinkService links,
        IOptions<LogLinkOptions> options,
        CancellationToken ct)
    {
        var webBase = options.Value.WebBaseUrl?.Trim().TrimEnd('/');
        if (!string.IsNullOrEmpty(webBase) && LogLinkId.IsValid(id) && WantsHtml(http.Request))
            return Results.Redirect($"{webBase}/l/{id}");

        return await RawAsync(id, last, links, ct);
    }

    private static async Task<IResult> RawAsync(
        string id,
        int? last,
        ILogLinkService links,
        CancellationToken ct)
    {
        var result = await links.ReadAsync(id, last ?? DefaultReadLimit, ct);
        if (result.Status == LogLinkStatus.NotFound)
            return NotFound();
        if (result.Status == LogLinkStatus.Expired)
            return Expired();

        var text = new StringBuilder();
        foreach (var entry in result.Entries)
        {
            text.Append(entry.Timestamp.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture))
                .Append(' ')
                .Append(LevelLabel(entry.Level).PadRight(5))
                .Append(' ')
                .Append(entry.Message)
                .Append('\n');
        }

        return Results.Text(text.ToString(), "text/plain", Encoding.UTF8);
    }

    private static async Task<IResult> LinesAsync(
        string id,
        int? limit,
        ILogLinkService links,
        CancellationToken ct)
    {
        var result = await links.ReadAsync(id, limit ?? DefaultReadLimit, ct);
        return result.Status switch
        {
            LogLinkStatus.NotFound => NotFound(),
            LogLinkStatus.Expired => Expired(),
            _ => Results.Ok(result.Entries)
        };
    }

    /// <summary>Reads the body as text, or null when it is longer than <paramref name="maxChars"/>.</summary>
    private static async Task<string?> ReadBodyAsync(HttpRequest request, int maxChars, CancellationToken ct)
    {
        using var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var buffer = new char[4096];
        var text = new StringBuilder();
        while (true)
        {
            var read = await reader.ReadAsync(buffer.AsMemory(), ct);
            if (read == 0)
                return text.ToString();

            text.Append(buffer, 0, read);
            if (text.Length > maxChars)
                return null;
        }
    }

    private static string PublicBase(HttpContext http, LogLinkOptions options)
    {
        var configured = options.PublicBaseUrl?.Trim().TrimEnd('/');
        return string.IsNullOrEmpty(configured)
            ? $"{http.Request.Scheme}://{http.Request.Host}"
            : configured;
    }

    private static bool WantsHtml(HttpRequest request) =>
        request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase);

    private static string LevelLabel(CoreLogLevel level) => level switch
    {
        CoreLogLevel.Trace => "TRACE",
        CoreLogLevel.Debug => "DEBUG",
        CoreLogLevel.Info => "INFO",
        CoreLogLevel.Warning => "WARN",
        CoreLogLevel.Error => "ERROR",
        CoreLogLevel.Critical => "CRIT",
        _ => level.ToString().ToUpperInvariant()
    };

    private static IResult NotFound() =>
        Error(StatusCodes.Status404NotFound, "no such log link; get a new one at https://qiklog.com");

    private static IResult Expired() =>
        Error(StatusCodes.Status410Gone, "this log link has expired; get a new one at https://qiklog.com");

    private static IResult Error(int statusCode, string message) =>
        Results.Json(new { error = message }, statusCode: statusCode);
}

/// <summary>Response body of <c>POST /l</c>.</summary>
/// <param name="Url">Send to it and open it: the same address does both.</param>
/// <param name="Raw">Plain-text read address, for terminals and coding agents.</param>
/// <param name="Send">A ready-to-paste example.</param>
public sealed record LogLinkCreated(
    string Id,
    string Url,
    string Raw,
    DateTimeOffset ExpiresAt,
    int MaxLines,
    string Send);

/// <summary>Response body of <c>POST /l/{id}</c>.</summary>
public sealed record LogLinkAccepted(int Accepted, int Remaining);
