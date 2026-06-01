using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QikLog.Infrastructure.Auth;
using QikLog.Infrastructure.Data;

namespace QikLog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddQikLogPersistence(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.Configure<IngestAuthOptions>(configuration.GetSection(IngestAuthOptions.SectionName));
        services.AddScoped<IIngestContext, IngestContext>();
        services.AddSingleton<ApiKeyHasher>();
        services.AddSingleton<ApiKeyRateLimiter>();

        if (environment.IsEnvironment("Testing"))
        {
            services.AddDbContext<QikLogDbContext>(options =>
                options.UseInMemoryDatabase("QikLogTests"));

            services.AddScoped<ILogEntryStore, EfLogEntryStore>();
            services.AddScoped<IApiKeyService, ApiKeyService>();
            return services;
        }

        var connectionString = configuration.GetConnectionString("Postgres");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddSingleton<ILogEntryStore, NullLogEntryStore>();
            services.AddSingleton<IApiKeyService, NullApiKeyService>();
            return services;
        }

        services.AddDbContext<QikLogDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<ILogEntryStore, EfLogEntryStore>();
        services.AddScoped<IApiKeyService, ApiKeyService>();
        return services;
    }

    public static async Task MigrateQikLogDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetService<QikLogDbContext>();
        if (db is null)
            return;

        // In-memory (tests) has no migrations.
        if (db.Database.IsRelational())
            await db.Database.MigrateAsync(cancellationToken);
    }
}
