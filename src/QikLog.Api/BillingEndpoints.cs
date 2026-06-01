using Microsoft.Extensions.Options;
using QikLog.Infrastructure.Billing;
using QikLog.Infrastructure.Tenants;
using Stripe;
using Stripe.Checkout;

namespace QikLog.Api;

internal static class BillingEndpoints
{
    public static void MapQikLogBilling(this WebApplication app)
    {
        var stripe = app.Services.GetRequiredService<IOptions<StripeOptions>>().Value;
        if (!stripe.Enabled || string.IsNullOrWhiteSpace(stripe.SecretKey))
            return;

        StripeConfiguration.ApiKey = stripe.SecretKey;

        app.MapPost("/v1/billing/checkout", async (
            ITenantContext tenant,
            IOptions<StripeOptions> options,
            CancellationToken ct) =>
        {
            if (tenant.TenantId is null)
                return Results.Unauthorized();

            var cfg = options.Value;
            if (string.IsNullOrWhiteSpace(cfg.ProPriceId))
                return Results.BadRequest(new { error = "Stripe Pro price is not configured" });

            var service = new SessionService();
            var session = await service.CreateAsync(new SessionCreateOptions
            {
                Mode = "subscription",
                SuccessUrl = cfg.SuccessUrl,
                CancelUrl = cfg.CancelUrl,
                LineItems =
                [
                    new SessionLineItemOptions
                    {
                        Price = cfg.ProPriceId,
                        Quantity = 1
                    }
                ],
                Metadata = new Dictionary<string, string>
                {
                    ["tenant_id"] = tenant.TenantId.Value.ToString()
                }
            }, cancellationToken: ct);

            return Results.Ok(new { url = session.Url });
        })
        .WithName("CreateCheckoutSession");
    }
}
