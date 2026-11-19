namespace QikLog.Infrastructure.Data;

/// <summary>QikLog tenant; maps 1:1 to a Zitadel organization when auth is enabled.</summary>
public sealed class TenantEntity
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    /// <summary>Zitadel org ID claim when using OIDC.</summary>
    public string? ZitadelOrgId { get; set; }

    public string Plan { get; set; } = "free";

    /// <summary>Stripe customer id (cus_...) once the tenant has started a checkout.</summary>
    public string? StripeCustomerId { get; set; }

    /// <summary>Current Stripe subscription id (sub_...); null when there is no live subscription.</summary>
    public string? StripeSubscriptionId { get; set; }

    /// <summary>When <see cref="Plan"/> last changed because of a billing event.</summary>
    public DateTimeOffset? PlanChangedAt { get; set; }

    /// <summary>
    /// Creation time of the newest Stripe event applied to this tenant. Stripe retries and
    /// reorders deliveries; anything older than this is ignored.
    /// </summary>
    public DateTimeOffset? BillingEventAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<ApiKeyEntity> ApiKeys { get; set; } = [];
}
