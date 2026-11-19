namespace QikLog.Infrastructure.Billing;

public sealed class StripeOptions
{
    public const string SectionName = "QikLog:Stripe";

    public bool Enabled { get; set; }

    public string? SecretKey { get; set; }

    public string? WebhookSecret { get; set; }

    public string ProPriceId { get; set; } = "";

    public string SuccessUrl { get; set; } = "http://localhost:5081/billing?success=1";

    public string CancelUrl { get; set; } = "http://localhost:5081/billing?canceled=1";

    /// <summary>Where the Stripe Customer Portal sends people back to.</summary>
    public string PortalReturnUrl { get; set; } = "http://localhost:5081/billing";

    /// <summary>
    /// Customer Portal configuration id (bpc_...). Optional: when empty Stripe uses the
    /// account's default portal configuration, which only exists after it has been saved
    /// once in the dashboard. scripts/stripe-setup.sh creates one and sets this.
    /// </summary>
    public string? PortalConfigurationId { get; set; }
}
