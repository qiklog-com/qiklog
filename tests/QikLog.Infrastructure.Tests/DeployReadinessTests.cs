using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using QikLog.Infrastructure.Data;
using QikLog.Infrastructure.DataProtection;
using QikLog.Infrastructure.Deployment;
using Shouldly;
using Xunit;

namespace QikLog.Infrastructure.Tests;

/// <summary>
/// Azure Container Apps has no startup ordering: Web can start before the API (or the
/// migrate job) has created data_protection_keys. Readiness and migrate-only cover that.
/// </summary>
public sealed class DeployReadinessTests
{
    // Port 1 refuses connections immediately; nothing is listening.
    private const string UnreachablePostgres = "Host=127.0.0.1;Port=1;Database=qiklog;Username=qiklog;Timeout=2";

    [Fact]
    public async Task Given_keys_table_is_queryable_When_probing_Then_ready()
    {
        // Given: a database with the keys table
        await using var db = InMemoryDb();

        // When: readiness is probed
        var result = await DataProtectionKeyStoreProbe.CheckAsync(db, CancellationToken.None);

        // Then: ready
        result.IsReady.ShouldBeTrue();
        result.Status.ShouldBe("ok");
    }

    [Fact]
    public async Task Given_database_unreachable_When_probing_Then_not_ready()
    {
        // Given: Postgres is configured but cannot be reached (or the table is missing)
        await using var db = NpgsqlDb(UnreachablePostgres);

        // When: readiness is probed
        var result = await DataProtectionKeyStoreProbe.CheckAsync(db, CancellationToken.None);

        // Then: not ready, without throwing
        result.IsReady.ShouldBeFalse();
        result.Status.ShouldBe("unavailable");
    }

    [Fact]
    public async Task Given_no_database_configured_When_probing_Then_ready_with_keys_on_disk()
    {
        // Given: local dev, no Postgres (keys on the file system)
        // When: readiness is probed
        var result = await DataProtectionKeyStoreProbe.CheckAsync(null, CancellationToken.None);

        // Then: ready, reported as skipped
        result.IsReady.ShouldBeTrue();
        result.Status.ShouldBe("skipped");
    }

    [Theory]
    [InlineData(new[] { "--migrate-only" }, null, true)]
    [InlineData(new[] { "--MIGRATE-ONLY" }, null, true)]
    [InlineData(new string[0], "1", true)]
    [InlineData(new string[0], "true", true)]
    [InlineData(new string[0], "TRUE", true)]
    [InlineData(new string[0], null, false)]
    [InlineData(new string[0], "0", false)]
    [InlineData(new string[0], "false", false)]
    [InlineData(new[] { "--urls", "http://localhost:5080" }, null, false)]
    public void Given_args_and_env_When_checking_migrate_only_Then_detected_correctly(
        string[] args, string? envValue, bool expected)
    {
        // Given: command-line args and the QIKLOG_MIGRATE_ONLY value
        // When / Then
        MigrateOnlyMode.IsRequested(args, envValue).ShouldBe(expected);
    }

    [Fact]
    public async Task Given_a_reachable_database_When_running_migrate_only_Then_exit_code_is_0()
    {
        // Given: a database is registered (in-memory has no migrations, which counts as success)
        var services = new ServiceCollection();
        var name = $"QikLogMigrate_{Guid.NewGuid():N}";
        services.AddDbContext<QikLogDbContext>(o => o.UseInMemoryDatabase(name));
        await using var provider = services.BuildServiceProvider();

        // When: migrate-only runs
        var exitCode = await MigrateOnlyMode.RunAsync(provider, NullLogger.Instance, CancellationToken.None);

        // Then: success
        exitCode.ShouldBe(0);
    }

    [Fact]
    public async Task Given_database_unreachable_When_running_migrate_only_Then_exit_code_is_1()
    {
        // Given: Postgres is configured but unreachable
        var services = new ServiceCollection();
        services.AddDbContext<QikLogDbContext>(o => o.UseNpgsql(UnreachablePostgres));
        await using var provider = services.BuildServiceProvider();

        // When: migrate-only runs
        var exitCode = await MigrateOnlyMode.RunAsync(provider, NullLogger.Instance, CancellationToken.None);

        // Then: failure, so the Container Apps Job fails and the rollout stops
        exitCode.ShouldBe(1);
    }

    [Fact]
    public async Task Given_no_connection_string_When_running_migrate_only_Then_exit_code_is_1()
    {
        // Given: no database registered (ConnectionStrings:Postgres missing)
        await using var provider = new ServiceCollection().BuildServiceProvider();

        // When: migrate-only runs
        var exitCode = await MigrateOnlyMode.RunAsync(provider, NullLogger.Instance, CancellationToken.None);

        // Then: misconfiguration is a failure, not a silent no-op
        exitCode.ShouldBe(1);
    }

    [Fact]
    public void Given_database_persistence_When_configured_Then_only_the_database_key_store_is_used()
    {
        // Given: Postgres configured
        var services = new ServiceCollection();
        var name = $"QikLogDp_{Guid.NewGuid():N}";
        services.AddDbContext<QikLogDbContext>(o => o.UseInMemoryDatabase(name));
        services.AddQikLogDataProtection(persistToDatabase: true, fallbackKeysPath: "unused");
        using var provider = services.BuildServiceProvider();

        // When: key management options are resolved
        var repository = provider.GetRequiredService<IOptions<KeyManagementOptions>>().Value.XmlRepository;

        // Then: the EF repository, never the file system
        repository.ShouldBeOfType<EntityFrameworkCoreXmlRepository<QikLogDbContext>>();
    }

    [Fact]
    public void Given_no_database_When_configured_Then_only_the_file_system_key_store_is_used()
    {
        // Given: no Postgres
        var keysPath = Path.Combine(Path.GetTempPath(), $"qiklog-dp-{Guid.NewGuid():N}");
        try
        {
            var services = new ServiceCollection();
            services.AddQikLogDataProtection(persistToDatabase: false, fallbackKeysPath: keysPath);
            using var provider = services.BuildServiceProvider();

            // When: key management options are resolved
            var repository = provider.GetRequiredService<IOptions<KeyManagementOptions>>().Value.XmlRepository;

            // Then: the file system repository only
            repository.ShouldBeOfType<FileSystemXmlRepository>();
        }
        finally
        {
            if (Directory.Exists(keysPath))
                Directory.Delete(keysPath, recursive: true);
        }
    }

    private static QikLogDbContext InMemoryDb() =>
        new(new DbContextOptionsBuilder<QikLogDbContext>()
            .UseInMemoryDatabase($"QikLogProbe_{Guid.NewGuid():N}")
            .Options);

    private static QikLogDbContext NpgsqlDb(string connectionString) =>
        new(new DbContextOptionsBuilder<QikLogDbContext>()
            .UseNpgsql(connectionString)
            .Options);
}
