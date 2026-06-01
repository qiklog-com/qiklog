using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QikLog.Api.Auth.Testing;
using QikLog.Infrastructure.Data;
using QikLog.Infrastructure.Tenants;

namespace QikLog.Api.Tests;

internal static class ApiTestData
{
    public static async Task SeedPrimaryTenantAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QikLogDbContext>();
        if (await db.Tenants.AnyAsync(t => t.Id == TestTenants.Primary, cancellationToken))
            return;

        db.Tenants.Add(new TenantEntity
        {
            Id = TestTenants.Primary,
            Name = "Test Tenant",
            Plan = "free",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public static async Task<string> CreateApiKeyForPrimaryTenantAsync(
        IServiceProvider services,
        string name = "test-key",
        CancellationToken cancellationToken = default)
    {
        await SeedPrimaryTenantAsync(services, cancellationToken);
        await using var scope = services.CreateAsyncScope();
        var tenant = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        tenant.TenantId = TestTenants.Primary;
        var keys = scope.ServiceProvider.GetRequiredService<QikLog.Infrastructure.Auth.IApiKeyService>();
        var created = await keys.CreateAsync(name, cancellationToken);
        return created.Plaintext;
    }
}
