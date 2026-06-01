namespace QikLog.Api.OpenApi;

internal static class OpenApiEndpointExtensions
{
    public static RouteHandlerBuilder WithOpenApiMetadata(
        this RouteHandlerBuilder builder,
        string tag,
        string summary,
        string description) =>
        builder
            .WithTags(tag)
            .WithSummary(summary)
            .WithDescription(description);
}
