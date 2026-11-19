using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QikLog.Infrastructure.Data;
using QikLog.Infrastructure.Tenants;

namespace QikLog.Infrastructure.Billing;

/// <summary>Plan names stored on <see cref="TenantEntity.Plan"/>.</summary>
public static class BillingPlans
{
    public const string Free = "free";
    public const string Pro = "pro";
}

/// <summary>
/// A billing change derived from one Stripe webhook event. Free of Stripe SDK types so
/// the plan rules can be tested without Stripe.
/// </summary>
/// <param name="EventId">Stripe event id (evt_...). Idempotency key.</param>
/// <param name="EventType">Stripe event type, for the processed-events record and logs.</param>
/// <param name="TenantIdHint">Tenant id carried in Stripe metadata or client_reference_id.</param>
/// <param name="CustomerId">Stripe customer id, when the event has one.</param>
/// <param name="SubscriptionId">Stripe subscription id, when the event has one.</param>
/// <param name="NewPlan">Plan to set, or null to record the event without a plan change.</param>
/// <param name="StartsSubscription">True for a completed checkout: this subscription becomes the tenant's current one.</param>
/// <param name="EndsSubscription">True when the subscription is gone; clears the stored subscription id.</param>
/// <param name="OccurredAt">When Stripe created the event. Older events than the last applied one are ignored.</param>
public sealed record BillingChange(
    string EventId,
    string EventType,
    Guid? TenantIdHint,
    string? CustomerId,
    string? SubscriptionId,
    string? NewPlan,
    bool StartsSubscription = false,
    bool EndsSubscription = false,
    DateTimeOffset? OccurredAt = null);

public enum BillingChangeOutcome
{
    /// <summary>The event was recorded and the tenant updated (or confirmed) as asked.</summary>
    Applied,
    /// <summary>This event id was already processed; nothing changed.</summary>
    Duplicate,
    /// <summary>No tenant matched; the event was recorded so Stripe stops retrying.</summary>
    TenantNotFound,
    /// <summary>The event is about an older subscription than the tenant's current one.</summary>
    StaleSubscription,
    /// <summary>The event carries no plan change; recorded only.</summary>
    NoChange,
    /// <summary>The event is older than one already applied to this tenant (Stripe retried or reordered it).</summary>
    OutOfOrder
}

public sealed record BillingStatus(string Plan, long UsedThisMonth, long MonthlyLimit, bool HasBillingAccount);

public interface ITenantBillingService
{
    Task<BillingChangeOutcome> ApplyAsync(BillingChange change, CancellationToken cancellationToken);

    Task<BillingStatus?> GetStatusAsync(Guid tenantId, CancellationToken cancellationToken);

    Task<string?> GetCustomerIdAsync(Guid tenantId, CancellationToken cancellationToken);

    Task SetCustomerIdAsync(Guid tenantId, string customerId, CancellationToken cancellationToken);

    /// <summary>True when the tenant already has a live Stripe subscription.</summary>
    Task<bool> HasSubscriptionAsync(Guid tenantId, CancellationToken cancellationToken);
}

public sealed class TenantBillingService(
    QikLogDbContext db,
    IOptions<UsageLimitOptions> usageOptions,
    TimeProvider clock,
    ILogger<TenantBillingService> log) : ITenantBillingService
{
    public async Task<BillingChangeOutcome> ApplyAsync(BillingChange change, CancellationToken cancellationToken)
    {
        if (await db.ProcessedStripeEvents.AnyAsync(e => e.EventId == change.EventId, cancellationToken))
        {
            log.LogInformation("Stripe event {EventId} already processed; skipping", change.EventId);
            return BillingChangeOutcome.Duplicate;
        }

        var now = clock.GetUtcNow();
        db.ProcessedStripeEvents.Add(new ProcessedStripeEventEntity
        {
            EventId = change.EventId,
            Type = change.EventType,
            ProcessedAt = now
        });

        var outcome = await ApplyToTenantAsync(change, now, cancellationToken);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsDuplicateEventKey(ex))
        {
            // Two deliveries of the same event raced; the other one won. Any other save
            // failure propagates so the webhook fails and Stripe retries.
            log.LogInformation("Stripe event {EventId} was processed concurrently; skipping", change.EventId);
            return BillingChangeOutcome.Duplicate;
        }

        return outcome;
    }

    private static bool IsDuplicateEventKey(DbUpdateException ex) =>
        ex.InnerException is Npgsql.PostgresException { SqlState: Npgsql.PostgresErrorCodes.UniqueViolation } pg
        && string.Equals(pg.TableName, "processed_stripe_events", StringComparison.Ordinal);

    private async Task<BillingChangeOutcome> ApplyToTenantAsync(
        BillingChange change,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (change.NewPlan is null && !change.StartsSubscription)
            return BillingChangeOutcome.NoChange;

        var tenant = await FindTenantAsync(change, cancellationToken);
        if (tenant is null)
        {
            log.LogWarning(
                "Stripe event {EventId} ({EventType}) matched no tenant",
                change.EventId,
                change.EventType);
            return BillingChangeOutcome.TenantNotFound;
        }

        if (change.OccurredAt is DateTimeOffset occurred
            && tenant.BillingEventAt is DateTimeOffset latest
            && occurred < latest)
        {
            log.LogInformation(
                "Stripe event {EventId} ({EventType}) is older than the last applied event for tenant {TenantId}; ignoring",
                change.EventId,
                change.EventType,
                tenant.Id);
            return BillingChangeOutcome.OutOfOrder;
        }

        // A late event about a subscription the tenant has since replaced must not
        // downgrade (or upgrade) the current one.
        if (!change.StartsSubscription
            && change.SubscriptionId is not null
            && tenant.StripeSubscriptionId is not null
            && !string.Equals(tenant.StripeSubscriptionId, change.SubscriptionId, StringComparison.Ordinal))
        {
            log.LogInformation(
                "Stripe event {EventId} is for subscription {Old}, tenant {TenantId} is on {Current}; ignoring",
                change.EventId,
                change.SubscriptionId,
                tenant.Id,
                tenant.StripeSubscriptionId);
            return BillingChangeOutcome.StaleSubscription;
        }

        if (!string.IsNullOrWhiteSpace(change.CustomerId))
            tenant.StripeCustomerId = change.CustomerId;

        if (change.OccurredAt is DateTimeOffset applied)
            tenant.BillingEventAt = applied;

        if (change.EndsSubscription)
            tenant.StripeSubscriptionId = null;
        else if (!string.IsNullOrWhiteSpace(change.SubscriptionId))
            tenant.StripeSubscriptionId = change.SubscriptionId;

        if (change.NewPlan is not null && !string.Equals(tenant.Plan, change.NewPlan, StringComparison.OrdinalIgnoreCase))
        {
            log.LogInformation(
                "Tenant {TenantId} plan {OldPlan} -> {NewPlan} (Stripe event {EventId}, {EventType})",
                tenant.Id,
                tenant.Plan,
                change.NewPlan,
                change.EventId,
                change.EventType);
            tenant.Plan = change.NewPlan;
            tenant.PlanChangedAt = now;
        }

        return BillingChangeOutcome.Applied;
    }

    private async Task<TenantEntity?> FindTenantAsync(BillingChange change, CancellationToken cancellationToken)
    {
        if (change.TenantIdHint is Guid hinted)
        {
            var byId = await db.Tenants.FirstOrDefaultAsync(t => t.Id == hinted, cancellationToken);
            if (byId is not null)
                return byId;
        }

        if (!string.IsNullOrWhiteSpace(change.CustomerId))
        {
            var byCustomer = await db.Tenants
                .FirstOrDefaultAsync(t => t.StripeCustomerId == change.CustomerId, cancellationToken);
            if (byCustomer is not null)
                return byCustomer;
        }

        if (!string.IsNullOrWhiteSpace(change.SubscriptionId))
        {
            return await db.Tenants
                .FirstOrDefaultAsync(t => t.StripeSubscriptionId == change.SubscriptionId, cancellationToken);
        }

        return null;
    }

    public async Task<BillingStatus?> GetStatusAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var tenant = await db.Tenants
            .AsNoTracking()
            .Where(t => t.Id == tenantId)
            .Select(t => new { t.Plan, t.StripeCustomerId })
            .FirstOrDefaultAsync(cancellationToken);
        if (tenant is null)
            return null;

        var limit = string.Equals(tenant.Plan, BillingPlans.Pro, StringComparison.OrdinalIgnoreCase)
            ? usageOptions.Value.ProIngestPerMonth
            : usageOptions.Value.FreeIngestPerMonth;

        var now = clock.GetUtcNow();
        var monthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var used = await db.LogEntries
            .Where(e => e.ReceivedAt >= monthStart)
            .ForTenant(db, tenantId)
            .LongCountAsync(cancellationToken);

        return new BillingStatus(tenant.Plan, used, limit, !string.IsNullOrWhiteSpace(tenant.StripeCustomerId));
    }

    public async Task<string?> GetCustomerIdAsync(Guid tenantId, CancellationToken cancellationToken) =>
        await db.Tenants
            .AsNoTracking()
            .Where(t => t.Id == tenantId)
            .Select(t => t.StripeCustomerId)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<bool> HasSubscriptionAsync(Guid tenantId, CancellationToken cancellationToken) =>
        await db.Tenants
            .AsNoTracking()
            .AnyAsync(t => t.Id == tenantId && t.StripeSubscriptionId != null, cancellationToken);

    public async Task SetCustomerIdAsync(Guid tenantId, string customerId, CancellationToken cancellationToken)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
        if (tenant is null || string.Equals(tenant.StripeCustomerId, customerId, StringComparison.Ordinal))
            return;

        tenant.StripeCustomerId = customerId;
        await db.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Used when no database is configured: billing has nothing to read or change.</summary>
public sealed class NullTenantBillingService : ITenantBillingService
{
    public Task<BillingChangeOutcome> ApplyAsync(BillingChange change, CancellationToken cancellationToken) =>
        Task.FromResult(BillingChangeOutcome.TenantNotFound);

    public Task<BillingStatus?> GetStatusAsync(Guid tenantId, CancellationToken cancellationToken) =>
        Task.FromResult<BillingStatus?>(null);

    public Task<string?> GetCustomerIdAsync(Guid tenantId, CancellationToken cancellationToken) =>
        Task.FromResult<string?>(null);

    public Task SetCustomerIdAsync(Guid tenantId, string customerId, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task<bool> HasSubscriptionAsync(Guid tenantId, CancellationToken cancellationToken) =>
        Task.FromResult(false);
}
