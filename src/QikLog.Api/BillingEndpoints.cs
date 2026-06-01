using Microsoft.Extensions.Options;
using QikLog.Api.OpenApi;
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

            return Results.Ok(new CheckoutSessionResponse(session.Url));
        })
        .WithName("CreateCheckoutSession")
        .WithOpenApiMetadata(
            OpenApiTags.Billing,
            "Create Stripe Checkout session",
            "Starts a Stripe Checkout session for the Pro plan. Requires `QikLog:Stripe:Enabled` and an authenticated tenant (OIDC JWT).")
        .WithTags(OpenApiTags.Tenants)
        .Produces<CheckoutSessionResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}

/// <summary>Stripe Checkout redirect URL.</summary>
internal sealed record CheckoutSessionResponse(string? Url);
