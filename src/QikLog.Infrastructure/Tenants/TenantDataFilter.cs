using QikLog.Infrastructure.Data;

namespace QikLog.Infrastructure.Tenants;

/// <summary>Applies tenant scoping to EF queries when <see cref="ITenantContext"/> is set.</summary>
internal static class TenantDataFilter
{
    public static IQueryable<ApiKeyEntity> ForTenant(this IQueryable<ApiKeyEntity> query, Guid? tenantId) =>
        tenantId is null ? query : query.Where(k => k.TenantId == tenantId);

    public static IQueryable<LogEntryEntity> ForTenant(
        this IQueryable<LogEntryEntity> query,
        QikLogDbContext db,
        Guid? tenantId)
    {
        if (tenantId is null)
            return query;

        return query.Where(e =>
            e.ApiKeyId != null &&
            db.ApiKeys.Any(k => k.Id == e.ApiKeyId && k.TenantId == tenantId));
    }
}
