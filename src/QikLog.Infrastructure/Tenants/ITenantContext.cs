namespace QikLog.Infrastructure.Tenants;

public interface ITenantContext
{
    Guid? TenantId { get; set; }

    string? ZitadelOrgId { get; set; }
}

public sealed class TenantContext : ITenantContext
{
    public Guid? TenantId { get; set; }

    public string? ZitadelOrgId { get; set; }
}
