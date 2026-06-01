using Microsoft.AspNetCore.OpenApi;
using Scalar.AspNetCore;

namespace QikLog.Api.OpenApi;

internal static class OpenApiServiceExtensions
{
    public static bool IsOpenApiEnabled(this IConfiguration configuration, IHostEnvironment environment) =>
        environment.IsDevelopment() || configuration.GetValue<bool>($"{OpenApiOptions.SectionName}:Enabled");

    public static IServiceCollection AddQikLogOpenApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OpenApiOptions>(configuration.GetSection(OpenApiOptions.SectionName));

        services.AddOpenApi("v1", options =>
        {
            options.AddDocumentTransformer<QikLogOpenApiDocumentTransformer>();
            options.AddOperationTransformer<QikLogOpenApiOperationTransformer>();
        });

        return services;
    }

    public static WebApplication UseQikLogOpenApi(this WebApplication app)
    {
        if (!app.Configuration.IsOpenApiEnabled(app.Environment))
            return app;

        app.MapOpenApi();
        app.MapScalarApiReference("/scalar/v1", options =>
        {
            options.WithTitle("QikLog API");
            options.WithOpenApiRoutePattern("/openapi/{documentName}.json");
        });

        return app;
    }
}
