using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using QikLog.Infrastructure.Auth;
using QikLog.Infrastructure.Billing;
using QikLog.Infrastructure.Data;
using QikLog.Infrastructure.Tenants;
using Shouldly;
using Xunit;

namespace QikLog.Infrastructure.Tests;

public sealed class TenantIsolationTests
{
    private readonly Guid _tenantA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private readonly Guid _tenantB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task List_api_keys_returns_only_current_tenant_keys()
    {
        await using var db = CreateDb();
        await SeedTenantsAndKeysAsync(db);

        var serviceA = CreateApiKeyService(db, _tenantA);
        var keysA = await serviceA.ListAsync(CancellationToken.None);
        keysA.ShouldHaveSingleItem();
        keysA[0].Name.ShouldBe("tenant-a-key");

        var serviceB = CreateApiKeyService(db, _tenantB);
        var keysB = await serviceB.ListAsync(CancellationToken.None);
        keysB.ShouldHaveSingleItem();
        keysB[0].Name.ShouldBe("tenant-b-key");
    }

    [Fact]
    public async Task Revoke_other_tenant_key_returns_false()
    {
        await using var db = CreateDb();
        var keyBId = await SeedTenantsAndKeysAsync(db);

        var serviceA = CreateApiKeyService(db, _tenantA);
        var revoked = await serviceA.RevokeAsync(keyBId, CancellationToken.None);
        revoked.ShouldBeFalse();

        var keyB = await db.ApiKeys.AsNoTracking().SingleAsync(k => k.Id == keyBId);
        keyB.IsActive.ShouldBeTrue();
        keyB.RevokedAt.ShouldBeNull();
    }

    [Fact]
    public async Task Usage_limit_is_enforced_per_tenant_not_globally()
    {
        await using var db = CreateDb();
        await SeedTenantsAndKeysAsync(db);

        var keyA = await db.ApiKeys.SingleAsync(k => k.TenantId == _tenantA);
        var monthStart = new DateTimeOffset(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);

        db.LogEntries.AddRange(
            new LogEntryEntity
            {
                Source = "a",
                Level = Core.LogLevel.Info,
                Message = "one",
                Timestamp = monthStart,
                ReceivedAt = monthStart,
                ApiKeyId = keyA.Id
            },
            new LogEntryEntity
            {
                Source = "a",
                Level = Core.LogLevel.Info,
                Message = "two",
                Timestamp = monthStart,
                ReceivedAt = monthStart,
                ApiKeyId = keyA.Id
            });
        await db.SaveChangesAsync();

        var options = Options.Create(new UsageLimitOptions { FreeIngestPerMonth = 2, ProIngestPerMonth = 100 });
        var serviceA = new UsageLimitService(db, new TenantContext { TenantId = _tenantA }, options, NullLogger<UsageLimitService>.Instance);
        var blocked = await serviceA.CheckIngestAllowedAsync(CancellationToken.None);
        blocked.Allowed.ShouldBeFalse();

        var serviceB = new UsageLimitService(db, new TenantContext { TenantId = _tenantB }, options, NullLogger<UsageLimitService>.Instance);
        var allowed = await serviceB.CheckIngestAllowedAsync(CancellationToken.None);
        allowed.Allowed.ShouldBeTrue();
        allowed.Count.ShouldBe(0);
    }

    private static QikLogDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<QikLogDbContext>()
            .UseInMemoryDatabase($"tenant-isolation-{Guid.NewGuid():N}")
            .Options;
        var db = new QikLogDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private async Task<Guid> SeedTenantsAndKeysAsync(QikLogDbContext db)
    {
        db.Tenants.AddRange(
            new TenantEntity { Id = _tenantA, Name = "A", Plan = "free", CreatedAt = DateTimeOffset.UtcNow },
            new TenantEntity { Id = _tenantB, Name = "B", Plan = "free", CreatedAt = DateTimeOffset.UtcNow });

        var keyA = new ApiKeyEntity
        {
            Id = Guid.NewGuid(),
            Name = "tenant-a-key",
            LookupPrefix = "aaaa1111",
            SecretHash = "hash",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            RateLimitPerMinute = 120,
            TenantId = _tenantA
        };
        var keyB = new ApiKeyEntity
        {
            Id = Guid.NewGuid(),
            Name = "tenant-b-key",
            LookupPrefix = "bbbb2222",
            SecretHash = "hash",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            RateLimitPerMinute = 120,
            TenantId = _tenantB
        };

        db.ApiKeys.AddRange(keyA, keyB);
        await db.SaveChangesAsync();
        return keyB.Id;
    }

    private static ApiKeyService CreateApiKeyService(QikLogDbContext db, Guid tenantId) =>
        new(
            db,
            new ApiKeyHasher(),
            Options.Create(new IngestAuthOptions()),
            new TenantContext { TenantId = tenantId },
            NullLogger<ApiKeyService>.Instance);
}
