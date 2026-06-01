using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QikLog.Infrastructure.Auth;
using QikLog.Infrastructure.Billing;
using QikLog.Infrastructure.Data;
using QikLog.Infrastructure.Sources;
using QikLog.Infrastructure.Tenants;

namespace QikLog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddQikLogPersistence(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.Configure<IngestAuthOptions>(configuration.GetSection(IngestAuthOptions.SectionName));
        services.Configure<ManagementOptions>(configuration.GetSection(ManagementOptions.SectionName));
        services.Configure<QikLogAuthOptions>(configuration.GetSection(QikLogAuthOptions.SectionName));
        services.Configure<UsageLimitOptions>(configuration.GetSection(UsageLimitOptions.SectionName));
        services.Configure<StripeOptions>(configuration.GetSection(StripeOptions.SectionName));
        services.AddScoped<IIngestContext, IngestContext>();
        services.AddSingleton<ApiKeyHasher>();
        services.AddSingleton<ApiKeyRateLimiter>();

        if (environment.IsEnvironment("Testing"))
        {
            // One in-memory store per test host (do not call Guid inside the options lambda).
            var testDbName = $"QikLogTests_{Guid.NewGuid():N}";
            services.AddDbContext<QikLogDbContext>(options =>
                options.UseInMemoryDatabase(testDbName));

            services.AddScoped<ILogEntryStore, EfLogEntryStore>();
            services.AddScoped<ILogHistoryService, EfLogHistoryService>();
            services.AddScoped<IApiKeyService, ApiKeyService>();
            services.AddScoped<ISourceCatalog, SourceCatalogService>();
            services.AddScoped<IUsageLimitService, UsageLimitService>();
            RegisterTenantServices(services);
            return services;
        }

        var connectionString = configuration.GetConnectionString("Postgres");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddSingleton<ILogEntryStore, NullLogEntryStore>();
            services.AddSingleton<ILogHistoryService, NullLogHistoryService>();
            services.AddSingleton<IApiKeyService, NullApiKeyService>();
            services.AddSingleton<ISourceCatalog, NullSourceCatalog>();
            services.AddSingleton<IUsageLimitService, NullUsageLimitService>();
            return services;
        }

        services.AddDbContext<QikLogDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<ILogEntryStore, EfLogEntryStore>();
        services.AddScoped<ILogHistoryService, EfLogHistoryService>();
        services.AddScoped<IApiKeyService, ApiKeyService>();
        services.AddScoped<ISourceCatalog, SourceCatalogService>();
        services.AddScoped<IUsageLimitService, UsageLimitService>();
        RegisterTenantServices(services);
        return services;
    }

    private static void RegisterTenantServices(IServiceCollection services)
    {
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<TenantProvisioner>();
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
