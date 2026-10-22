using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QikLog.Infrastructure.Data;

namespace QikLog.Infrastructure.Deployment;

/// <summary>
/// Runs database migrations and exits, without starting the API. Used by a one-off
/// Container Apps Job before the API and Web revisions roll out.
/// </summary>
public static class MigrateOnlyMode
{
    public const string Argument = "--migrate-only";
    public const string EnvironmentVariable = "QIKLOG_MIGRATE_ONLY";

    public static bool IsRequested(IReadOnlyList<string> args, IConfiguration configuration) =>
        IsRequested(args, configuration[EnvironmentVariable]);

    /// <param name="migrateOnlySetting">Value of <see cref="EnvironmentVariable"/>: "1" or "true" enables it.</param>
    public static bool IsRequested(IReadOnlyList<string> args, string? migrateOnlySetting) =>
        args.Any(a => string.Equals(a, Argument, StringComparison.OrdinalIgnoreCase))
        || migrateOnlySetting == "1"
        || string.Equals(migrateOnlySetting, "true", StringComparison.OrdinalIgnoreCase);

    /// <returns>Process exit code: 0 when migrations are applied, 1 otherwise.</returns>
    public static async Task<int> RunAsync(IServiceProvider services, ILogger logger, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetService<QikLogDbContext>();
        if (db is null)
        {
            logger.LogError("Migrate-only mode needs ConnectionStrings:Postgres; no database is configured");
            return 1;
        }

        if (!db.Database.IsRelational())
        {
            logger.LogInformation("Migrate-only mode: {Provider} has no migrations, nothing to do", db.Database.ProviderName);
            return 0;
        }

        try
        {
            var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
            logger.LogInformation("Migrate-only mode: applying {Count} pending migration(s): {Migrations}",
                pending.Count, string.Join(", ", pending));
            await db.Database.MigrateAsync(cancellationToken);
            logger.LogInformation("Migrate-only mode: database is up to date");
            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Migrate-only mode: migration failed");
            return 1;
        }
    }
}
