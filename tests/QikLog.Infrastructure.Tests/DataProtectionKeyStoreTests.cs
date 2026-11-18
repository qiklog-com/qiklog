using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using QikLog.Infrastructure.Data;
using QikLog.Infrastructure.DataProtection;
using Shouldly;
using Xunit;

namespace QikLog.Infrastructure.Tests;

/// <summary>
/// Web's Data Protection key ring must survive container restarts and be shared by every
/// Web instance (Azure Container Apps wipes local disk on each revision).
/// </summary>
public sealed class DataProtectionKeyStoreTests
{
    private const string Purpose = "QikLog.Tests.Antiforgery";

    [Fact]
    public void Given_two_web_instances_sharing_a_database_When_first_protects_a_payload_Then_second_can_unprotect_it()
    {
        // Given: two independent app instances (separate DI containers) on the same database
        var database = new SharedDatabase();
        using var instanceA = BuildDatabaseBackedInstance(database);
        using var instanceB = BuildDatabaseBackedInstance(database);

        // When: instance A protects a payload (as it would an auth or antiforgery cookie)
        var protectedPayload = Protector(instanceA).Protect("signed-in-user");

        // Then: instance B reads the key from the database and unprotects it
        Protector(instanceB).Unprotect(protectedPayload).ShouldBe("signed-in-user");
    }

    [Fact]
    public void Given_a_restarted_instance_When_it_reads_an_old_cookie_Then_the_key_comes_from_the_database()
    {
        // Given: an instance protected a payload, then was disposed (container restart / new revision)
        var database = new SharedDatabase();
        string protectedPayload;
        using (var before = BuildDatabaseBackedInstance(database))
        {
            protectedPayload = Protector(before).Protect("still-signed-in");
        }

        // When: a fresh instance starts against the same database
        using var after = BuildDatabaseBackedInstance(database);

        // Then: the old payload is still readable, and the key is stored as a row
        Protector(after).Unprotect(protectedPayload).ShouldBe("still-signed-in");
        using var scope = after.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QikLogDbContext>();
        db.DataProtectionKeys.Count().ShouldBe(1);
        db.DataProtectionKeys.Single().Xml.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Given_database_persistence_When_configured_Then_application_name_is_fixed()
    {
        // Given: Web's Data Protection setup
        using var instance = BuildDatabaseBackedInstance(new SharedDatabase());

        // When: the options are resolved
        var options = instance.GetRequiredService<IOptions<DataProtectionOptions>>().Value;

        // Then: the discriminator is the stable name, not something derived from the content root
        options.ApplicationDiscriminator.ShouldBe(QikLogDataProtection.ApplicationName);
        QikLogDataProtection.ApplicationName.ShouldBe("QikLog.Web");
    }

    [Fact]
    public void Given_an_app_with_a_different_name_When_it_reads_web_payloads_Then_unprotect_fails()
    {
        // Given: Web protected a payload; another app shares the database but uses its own name
        var database = new SharedDatabase();
        using var web = BuildDatabaseBackedInstance(database);
        var protectedPayload = Protector(web).Protect("web-only");

        var services = new ServiceCollection();
        database.Register(services);
        services.AddDataProtection()
            .SetApplicationName("Some.Other.App")
            .PersistKeysToDbContext<QikLogDbContext>();
        using var other = services.BuildServiceProvider();

        // When / Then: the payload is isolated by application name
        Should.Throw<System.Security.Cryptography.CryptographicException>(
            () => Protector(other).Unprotect(protectedPayload));
    }

    [Fact]
    public void Given_no_database_When_configured_Then_keys_are_written_to_the_fallback_directory()
    {
        // Given: local dev with no Postgres connection string
        var keysPath = Path.Combine(Path.GetTempPath(), $"qiklog-dp-{Guid.NewGuid():N}");
        try
        {
            var services = new ServiceCollection();
            services.AddQikLogDataProtection(persistToDatabase: false, fallbackKeysPath: keysPath);
            using var instance = services.BuildServiceProvider();

            // When: a payload is protected
            Protector(instance).Protect("local-dev");

            // Then: the key ring lives on disk at the fallback path
            Directory.GetFiles(keysPath, "key-*.xml").Length.ShouldBe(1);
        }
        finally
        {
            if (Directory.Exists(keysPath))
                Directory.Delete(keysPath, recursive: true);
        }
    }

    [Fact]
    public void Given_the_model_When_mapped_to_postgres_Then_keys_table_is_data_protection_keys()
    {
        // Given: the Postgres-backed context (no connection is opened)
        using var db = CreateNpgsqlContext();

        // When: the key entity is inspected
        var entity = db.Model.FindEntityType(typeof(Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey));

        // Then: it maps to the snake_case table used by the other QikLog tables
        entity.ShouldNotBeNull();
        entity.GetTableName().ShouldBe("data_protection_keys");
    }

    [Fact]
    public void Given_the_model_When_compared_to_the_migrations_snapshot_Then_there_are_no_pending_changes()
    {
        // Given: the Postgres-backed context (no connection is opened)
        using var db = CreateNpgsqlContext();

        // When / Then: every model change, including the keys table, has a migration
        db.Database.HasPendingModelChanges().ShouldBeFalse();
    }

    private static ServiceProvider BuildDatabaseBackedInstance(SharedDatabase database)
    {
        var services = new ServiceCollection();
        database.Register(services);
        services.AddQikLogDataProtection(persistToDatabase: true, fallbackKeysPath: "unused");
        return services.BuildServiceProvider();
    }

    private static IDataProtector Protector(IServiceProvider services) =>
        services.GetRequiredService<IDataProtectionProvider>().CreateProtector(Purpose);

    private static QikLogDbContext CreateNpgsqlContext() =>
        new(new DbContextOptionsBuilder<QikLogDbContext>()
            .UseNpgsql("Host=localhost;Database=unused")
            .Options);

    /// <summary>One in-memory database that several DI containers can open, like a shared Postgres.</summary>
    private sealed class SharedDatabase
    {
        private readonly InMemoryDatabaseRoot _root = new();
        private readonly string _name = $"QikLogDataProtection_{Guid.NewGuid():N}";

        public void Register(IServiceCollection services) =>
            services.AddDbContext<QikLogDbContext>(options => options.UseInMemoryDatabase(_name, _root));
    }
}
