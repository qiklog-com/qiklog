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

public sealed class UsageLimitServiceTests
{
    [Fact]
    public async Task Pro_plan_uses_higher_limit()
    {
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity
        {
            Id = tenantId,
            Name = "pro-co",
            Plan = "pro",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var options = Options.Create(new UsageLimitOptions { FreeIngestPerMonth = 2, ProIngestPerMonth = 5 });
        var service = new UsageLimitService(
            db,
            new TenantContext { TenantId = tenantId },
            options,
            Options.Create(new AuthEnforcementOptions { Enabled = false }),
            NullLogger<UsageLimitService>.Instance);

        var result = await service.CheckIngestAllowedAsync(CancellationToken.None);
        result.Allowed.ShouldBeTrue();
        result.Limit.ShouldBe(5);
    }

    [Fact]
    public async Task Free_plan_blocks_at_configured_limit()
    {
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var keyId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity
        {
            Id = tenantId,
            Name = "free-co",
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

        var monthStart = new DateTimeOffset(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);
        db.LogEntries.Add(new LogEntryEntity
        {
            Source = "s",
            Level = Core.LogLevel.Info,
            Message = "m",
            Timestamp = monthStart,
            ReceivedAt = monthStart,
            ApiKeyId = keyId
        });
        await db.SaveChangesAsync();

        var options = Options.Create(new UsageLimitOptions { FreeIngestPerMonth = 1, ProIngestPerMonth = 100 });
        var service = new UsageLimitService(
            db,
            new TenantContext { TenantId = tenantId },
            options,
            Options.Create(new AuthEnforcementOptions { Enabled = false }),
            NullLogger<UsageLimitService>.Instance);

        var result = await service.CheckIngestAllowedAsync(CancellationToken.None);
        result.Allowed.ShouldBeFalse();
        result.Count.ShouldBe(1);
        result.Limit.ShouldBe(1);
        result.Reason.ShouldNotBeNull().ShouldContain("limit");
    }

    private static QikLogDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<QikLogDbContext>()
            .UseInMemoryDatabase($"usage-limit-{Guid.NewGuid():N}")
            .Options;
        var db = new QikLogDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}
