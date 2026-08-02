using Microsoft.EntityFrameworkCore;
using QikLog.Core;
using QikLog.Infrastructure.Data;
using QikLog.Infrastructure.Sources;
using QikLog.Infrastructure.Tenants;
using Shouldly;
using Xunit;

namespace QikLog.Infrastructure.Tests;

public sealed class SourceCatalogServiceTests
{
    private readonly Guid _tenantA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private readonly Guid _tenantB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task ListAsync_groups_by_source_with_counts()
    {
        await using var db = CreateDb();
        var keyA = await SeedAsync(db);
        var t0 = DateTimeOffset.UtcNow.AddMinutes(-3);
        var t1 = DateTimeOffset.UtcNow.AddMinutes(-1);
        db.LogEntries.AddRange(
            new LogEntryEntity
            {
                Source = "api",
                Level = LogLevel.Info,
                Message = "1",
                Timestamp = t0,
                ReceivedAt = t0,
                ApiKeyId = keyA
            },
            new LogEntryEntity
            {
                Source = "api",
                Level = LogLevel.Info,
                Message = "2",
                Timestamp = t1,
                ReceivedAt = t1,
                ApiKeyId = keyA
            },
            new LogEntryEntity
            {
                Source = "worker",
                Level = LogLevel.Info,
                Message = "w",
                Timestamp = t0,
                ReceivedAt = t0,
                ApiKeyId = keyA
            });
        await db.SaveChangesAsync();

        var catalog = new SourceCatalogService(db, new TenantContext { TenantId = _tenantA });
        var list = await catalog.ListAsync(CancellationToken.None);

        list.Count.ShouldBe(2);
        list[0].Name.ShouldBe("api");
        list[0].EntryCount.ShouldBe(2);
        list[0].LastReceivedAt.ShouldBe(t1);
        list[1].Name.ShouldBe("worker");
        list[1].EntryCount.ShouldBe(1);
    }

    [Fact]
    public async Task ListAsync_excludes_other_tenant_entries()
    {
        await using var db = CreateDb();
        var (_, keyB) = await SeedBothKeysAsync(db);
        db.LogEntries.Add(new LogEntryEntity
        {
            Source = "secret-b",
            Level = LogLevel.Info,
            Message = "nope",
            Timestamp = DateTimeOffset.UtcNow,
            ReceivedAt = DateTimeOffset.UtcNow,
            ApiKeyId = keyB
        });
        await db.SaveChangesAsync();

        var catalog = new SourceCatalogService(db, new TenantContext { TenantId = _tenantA });
        (await catalog.ListAsync(CancellationToken.None)).ShouldBeEmpty();
    }

    [Fact]
    public async Task ListAsync_empty_when_no_entries()
    {
        await using var db = CreateDb();
        await SeedAsync(db);
        var catalog = new SourceCatalogService(db, new TenantContext { TenantId = _tenantA });
        (await catalog.ListAsync(CancellationToken.None)).ShouldBeEmpty();
    }

    [Fact]
    public async Task NullSourceCatalog_returns_empty()
    {
        var catalog = new NullSourceCatalog();
        (await catalog.ListAsync(CancellationToken.None)).ShouldBeEmpty();
    }

    private async Task<Guid> SeedAsync(QikLogDbContext db)
    {
        var (keyA, _) = await SeedBothKeysAsync(db);
        return keyA;
    }

    private async Task<(Guid KeyA, Guid KeyB)> SeedBothKeysAsync(QikLogDbContext db)
    {
        db.Tenants.AddRange(
            new TenantEntity { Id = _tenantA, Name = "A", Plan = "free", CreatedAt = DateTimeOffset.UtcNow },
            new TenantEntity { Id = _tenantB, Name = "B", Plan = "free", CreatedAt = DateTimeOffset.UtcNow });
        var keyA = Guid.NewGuid();
        var keyB = Guid.NewGuid();
        db.ApiKeys.AddRange(
            new ApiKeyEntity
            {
                Id = keyA,
                Name = "a",
                LookupPrefix = "aaaa1111",
                SecretHash = "h",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                RateLimitPerMinute = 60,
                TenantId = _tenantA
            },
            new ApiKeyEntity
            {
                Id = keyB,
                Name = "b",
                LookupPrefix = "bbbb2222",
                SecretHash = "h",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                RateLimitPerMinute = 60,
                TenantId = _tenantB
            });
        await db.SaveChangesAsync();
        return (keyA, keyB);
    }

    private static QikLogDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<QikLogDbContext>()
            .UseInMemoryDatabase($"source-catalog-{Guid.NewGuid():N}")
            .Options;
        var db = new QikLogDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}
