using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using QikLog.Core;
using QikLog.Infrastructure.Auth;
using QikLog.Infrastructure.Billing;
using QikLog.Infrastructure.Data;
using QikLog.Infrastructure.Tenants;
using Shouldly;
using Xunit;

namespace QikLog.Infrastructure.Tests;

public sealed class UsageLimitServiceEdgeTests
{
    [Fact]
    public async Task Check_rejects_when_enforcement_enabled_and_tenant_null()
    {
        await using var db = CreateDb();
        var service = new UsageLimitService(
            db,
            new TenantContext(),
            Options.Create(new UsageLimitOptions { FreeIngestPerMonth = 100 }),
            Options.Create(new AuthEnforcementOptions { Enabled = true }),
            NullLogger<UsageLimitService>.Instance);

        var result = await service.CheckIngestAllowedAsync(CancellationToken.None);
        result.Allowed.ShouldBeFalse();
        result.Reason.ShouldNotBeNull().ShouldContain("tenant");
    }

    [Fact]
    public async Task Check_ignores_entries_from_previous_month()
    {
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var keyId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity
        {
            Id = tenantId,
            Name = "free",
            Plan = "free",
            CreatedAt = DateTimeOffset.UtcNow
        });
        db.ApiKeys.Add(new ApiKeyEntity
        {
            Id = keyId,
            Name = "k",
            LookupPrefix = "abcd1234",
            SecretHash = "x",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            RateLimitPerMinute = 60,
            TenantId = tenantId
        });

        var lastMonth = new DateTimeOffset(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero)
            .AddDays(-1);
        db.LogEntries.Add(new LogEntryEntity
        {
            Source = "s",
            Level = LogLevel.Info,
            Message = "old",
            Timestamp = lastMonth,
            ReceivedAt = lastMonth,
            ApiKeyId = keyId
        });
        await db.SaveChangesAsync();

        var service = new UsageLimitService(
            db,
            new TenantContext { TenantId = tenantId },
            Options.Create(new UsageLimitOptions { FreeIngestPerMonth = 1, ProIngestPerMonth = 100 }),
            Options.Create(new AuthEnforcementOptions { Enabled = false }),
            NullLogger<UsageLimitService>.Instance);

        var result = await service.CheckIngestAllowedAsync(CancellationToken.None);
        result.Allowed.ShouldBeTrue();
        result.Count.ShouldBe(0);
    }

    [Fact]
    public async Task Check_allows_when_count_below_limit()
    {
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity
        {
            Id = tenantId,
            Name = "free",
            Plan = "free",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new UsageLimitService(
            db,
            new TenantContext { TenantId = tenantId },
            Options.Create(new UsageLimitOptions { FreeIngestPerMonth = 10 }),
            Options.Create(new AuthEnforcementOptions { Enabled = false }),
            NullLogger<UsageLimitService>.Instance);

        var result = await service.CheckIngestAllowedAsync(CancellationToken.None);
        result.Allowed.ShouldBeTrue();
        result.Limit.ShouldBe(10);
    }

    [Fact]
    public async Task NullUsageLimitService_always_allows()
    {
        var result = await new NullUsageLimitService().CheckIngestAllowedAsync(CancellationToken.None);
        result.Allowed.ShouldBeTrue();
        result.Limit.ShouldBe(long.MaxValue);
    }

    private static QikLogDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<QikLogDbContext>()
            .UseInMemoryDatabase($"usage-edge-{Guid.NewGuid():N}")
            .Options;
        var db = new QikLogDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}
