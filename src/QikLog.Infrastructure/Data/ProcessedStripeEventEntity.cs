namespace QikLog.Infrastructure.Data;

/// <summary>
/// A Stripe webhook event that has already been handled. Stripe retries deliveries,
/// so the event id is the idempotency key.
/// </summary>
public sealed class ProcessedStripeEventEntity
{
    public required string EventId { get; set; }

    public required string Type { get; set; }

    public DateTimeOffset ProcessedAt { get; set; }
}
