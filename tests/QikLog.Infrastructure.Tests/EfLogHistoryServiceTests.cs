using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QikLog.Core;
using QikLog.Infrastructure;
using QikLog.Infrastructure.Data;
using QikLog.Infrastructure.Tenants;
using Shouldly;
using Xunit;

namespace QikLog.Infrastructure.Tests;

public sealed class EfLogHistoryServiceTests
{
    private readonly Guid _tenantA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private readonly Guid _tenantB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task GetRecent_returns_oldest_first()
    {
        await using var db = await SeedAsync();
        var service = new EfLogHistoryService(db, new TenantContext { TenantId = _tenantA });

        var entries = await service.GetRecentBySourceAsync("hist", 10, CancellationToken.None);

        entries.Count.ShouldBe(2);
        entries[0].Message.ShouldBe("first");
        entries[1].Message.ShouldBe("second");
    }

    [Fact]
    public async Task GetRecent_clamps_limit_to_at_least_one()
    {
        await using var db = await SeedAsync();
        var service = new EfLogHistoryService(db, new TenantContext { TenantId = _tenantA });
        var entries = await service.GetRecentBySourceAsync("hist", 0, CancellationToken.None);
        entries.Count.ShouldBe(1);
        entries[0].Message.ShouldBe("second");
    }

    [Fact]
    public async Task GetRecent_clamps_limit_to_500()
    {
        await using var db = CreateDb();
        var keyA = await SeedTenantsAndKeyAsync(db);
        var baseTime = DateTimeOffset.UtcNow.AddHours(-1);
        for (var i = 0; i < 505; i++)
        {
            db.LogEntries.Add(new LogEntryEntity
            {
                Source = "flood",
                Level = LogLevel.Info,
                Message = $"m{i}",
                Timestamp = baseTime.AddSeconds(i),
                ReceivedAt = baseTime.AddSeconds(i),
                ApiKeyId = keyA
            });
        }

        await db.SaveChangesAsync();
        var service = new EfLogHistoryService(db, new TenantContext { TenantId = _tenantA });
        var entries = await service.GetRecentBySourceAsync("flood", 10_000, CancellationToken.None);
        entries.Count.ShouldBe(500);
    }

    [Fact]
    public async Task GetRecent_filters_by_source()
    {
        await using var db = await SeedAsync();
        var service = new EfLogHistoryService(db, new TenantContext { TenantId = _tenantA });
        var entries = await service.GetRecentBySourceAsync("other", 10, CancellationToken.None);
        entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetRecent_scopes_to_tenant_via_api_key()
    {
        await using var db = await SeedAsync();
        var serviceB = new EfLogHistoryService(db, new TenantContext { TenantId = _tenantB });
        var entries = await serviceB.GetRecentBySourceAsync("hist", 10, CancellationToken.None);
        entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetRecent_deserializes_properties_json()
    {
        await using var db = CreateDb();
        var keyA = await SeedTenantsAndKeyAsync(db);
        db.LogEntries.Add(new LogEntryEntity
        {
            Source = "props",
            Level = LogLevel.Info,
            Message = "with props",
            Timestamp = DateTimeOffset.UtcNow,
            ReceivedAt = DateTimeOffset.UtcNow,
            ApiKeyId = keyA,
            PropertiesJson = JsonSerializer.Serialize(new Dictionary<string, string> { ["env"] = "prod" })
        });
        await db.SaveChangesAsync();

        var service = new EfLogHistoryService(db, new TenantContext { TenantId = _tenantA });
        var entries = await service.GetRecentBySourceAsync("props", 10, CancellationToken.None);
        entries.Single().Properties!["env"].ShouldBe("prod");
    }

    [Fact]
    public async Task NullLogHistoryService_returns_empty_and_IsEnabled_false()
    {
        var service = new NullLogHistoryService();
        service.IsEnabled.ShouldBeFalse();
        (await service.GetRecentBySourceAsync("any", 10, CancellationToken.None)).ShouldBeEmpty();
    }

    private async Task<QikLogDbContext> SeedAsync()
    {
        var db = CreateDb();
        var keyA = await SeedTenantsAndKeyAsync(db);
        var t0 = DateTimeOffset.UtcNow.AddMinutes(-2);
        var t1 = DateTimeOffset.UtcNow.AddMinutes(-1);
        db.LogEntries.AddRange(
            new LogEntryEntity
            {
                Source = "hist",
                Level = LogLevel.Info,
                Message = "first",
                Timestamp = t0,
                ReceivedAt = t0,
                ApiKeyId = keyA
            },
            new LogEntryEntity
            {
                Source = "hist",
                Level = LogLevel.Info,
                Message = "second",
                Timestamp = t1,
                ReceivedAt = t1,
                ApiKeyId = keyA
            });
        await db.SaveChangesAsync();
        return db;
    }

    private async Task<Guid> SeedTenantsAndKeyAsync(QikLogDbContext db)
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
        return keyA;
    }

    private static QikLogDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<QikLogDbContext>()
            .UseInMemoryDatabase($"log-history-{Guid.NewGuid():N}")
            .Options;
        var db = new QikLogDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}
