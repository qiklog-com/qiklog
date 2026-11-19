using QikLog.Infrastructure.Billing;
using Stripe;
using CheckoutSession = Stripe.Checkout.Session;

namespace QikLog.Api.Billing;

/// <summary>
/// Turns a verified Stripe event into a <see cref="BillingChange"/>. Pure mapping:
/// no I/O, so every plan rule is unit tested here.
/// </summary>
internal static class StripeBillingEvents
{
    public const string TenantIdMetadataKey = "tenant_id";

    public const string CheckoutSessionCompleted = "checkout.session.completed";
    public const string SubscriptionCreated = "customer.subscription.created";
    public const string SubscriptionUpdated = "customer.subscription.updated";
    public const string SubscriptionDeleted = "customer.subscription.deleted";

    /// <summary>Statuses where the customer is paid up and gets Pro.</summary>
    private static readonly HashSet<string> ProStatuses = new(StringComparer.Ordinal) { "active", "trialing" };

    /// <summary>Statuses where Stripe has given up on payment; the tenant drops to Free.</summary>
    private static readonly HashSet<string> FreeStatuses = new(StringComparer.Ordinal)
    {
        "canceled", "unpaid", "incomplete_expired", "paused"
    };

    // past_due and incomplete map to "no change": Stripe is still retrying the card or
    // waiting on payment, and a customer must not lose Pro over a retry.

    /// <summary>Returns the change for a handled event type, or null for any other type.</summary>
    public static BillingChange? ToChange(Event stripeEvent)
    {
        switch (stripeEvent.Type)
        {
            case CheckoutSessionCompleted when stripeEvent.Data.Object is CheckoutSession session:
                return FromCheckout(stripeEvent, session);

            case SubscriptionCreated or SubscriptionUpdated when stripeEvent.Data.Object is Subscription subscription:
                return new BillingChange(
                    stripeEvent.Id,
                    stripeEvent.Type,
                    TenantIdFrom(subscription.Metadata),
                    subscription.CustomerId,
                    subscription.Id,
                    PlanForStatus(subscription.Status),
                    EndsSubscription: FreeStatuses.Contains(subscription.Status ?? ""),
                    OccurredAt: OccurredAt(stripeEvent));

            case SubscriptionDeleted when stripeEvent.Data.Object is Subscription deleted:
                return new BillingChange(
                    stripeEvent.Id,
                    stripeEvent.Type,
                    TenantIdFrom(deleted.Metadata),
                    deleted.CustomerId,
                    deleted.Id,
                    BillingPlans.Free,
                    EndsSubscription: true,
                    OccurredAt: OccurredAt(stripeEvent));

            default:
                return null;
        }
    }

    public static string? PlanForStatus(string? status)
    {
        if (status is null)
            return null;
        if (ProStatuses.Contains(status))
            return BillingPlans.Pro;
        if (FreeStatuses.Contains(status))
            return BillingPlans.Free;
        return null;
    }

    private static BillingChange? FromCheckout(Event stripeEvent, CheckoutSession session)
    {
        if (!string.Equals(session.Mode, "subscription", StringComparison.Ordinal))
            return null;

        // An async payment method can complete checkout before money moves. Record the
        // subscription now; customer.subscription.updated grants Pro once it is active.
        var paid = session.PaymentStatus is "paid" or "no_payment_required";

        var tenantId = TenantIdFrom(session.Metadata)
            ?? (Guid.TryParse(session.ClientReferenceId, out var fromReference) ? fromReference : null);

        return new BillingChange(
            stripeEvent.Id,
            stripeEvent.Type,
            tenantId,
            session.CustomerId,
            session.SubscriptionId,
            paid ? BillingPlans.Pro : null,
            StartsSubscription: true,
            OccurredAt: OccurredAt(stripeEvent));
    }

    private static DateTimeOffset? OccurredAt(Event stripeEvent) =>
        stripeEvent.Created == default ? null : new DateTimeOffset(DateTime.SpecifyKind(stripeEvent.Created, DateTimeKind.Utc));

    private static Guid? TenantIdFrom(IDictionary<string, string>? metadata) =>
        metadata is not null
        && metadata.TryGetValue(TenantIdMetadataKey, out var raw)
        && Guid.TryParse(raw, out var id)
            ? id
            : null;
}
