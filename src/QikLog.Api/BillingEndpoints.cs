using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using QikLog.Api.Billing;
using QikLog.Api.OpenApi;
using QikLog.Infrastructure.Billing;
using QikLog.Infrastructure.Tenants;
using Stripe;
using Stripe.Checkout;
using PortalSessionCreateOptions = Stripe.BillingPortal.SessionCreateOptions;
using PortalSessionService = Stripe.BillingPortal.SessionService;

namespace QikLog.Api;

internal static class BillingEndpoints
{
    public const string WebhookPath = "/v1/billing/webhook";

    /// <summary>Stripe events are a few KB; this leaves wide headroom.</summary>
    private const long MaxWebhookBytes = 256 * 1024;

    public static void MapQikLogBilling(this WebApplication app)
    {
        // Plan and usage are shown on the Billing page whether or not Stripe is on.
        app.MapGet("/v1/billing/status", async (
            ITenantContext tenant,
            [FromServices] ITenantBillingService billing,
            CancellationToken ct) =>
        {
            if (tenant.TenantId is not Guid tenantId)
                return Results.Unauthorized();

            var status = await billing.GetStatusAsync(tenantId, ct);
            return status is null
                ? Results.NotFound(new { error = "tenant not found" })
                : Results.Ok(new BillingStatusResponse(
                    status.Plan,
                    status.UsedThisMonth,
                    status.MonthlyLimit,
                    status.HasBillingAccount));
        })
        .WithName("GetBillingStatus")
        .WithOpenApiMetadata(
            OpenApiTags.Billing,
            "Get plan and usage",
            "Current plan, log lines received this calendar month (UTC), the monthly limit, and whether a Stripe billing account exists.")
        .Produces<BillingStatusResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        var stripe = app.Services.GetRequiredService<IOptions<StripeOptions>>().Value;
        if (!stripe.Enabled || string.IsNullOrWhiteSpace(stripe.SecretKey))
            return;

        StripeConfiguration.ApiKey = stripe.SecretKey;

        app.MapPost("/v1/billing/checkout", async (
            ITenantContext tenant,
            [FromServices] ITenantBillingService billing,
            IOptions<StripeOptions> options,
            ILogger<BillingLog> log,
            CancellationToken ct) =>
        {
            if (tenant.TenantId is not Guid tenantId)
            {
                log.LogWarning("Checkout rejected: missing tenant context");
                return Results.Unauthorized();
            }

            var cfg = options.Value;
            if (string.IsNullOrWhiteSpace(cfg.ProPriceId))
            {
                log.LogWarning("Checkout rejected: Stripe Pro price not configured");
                return Results.BadRequest(new { error = "Stripe Pro price is not configured" });
            }

            // One subscription per tenant. A second checkout (two tabs, a replayed request)
            // would bill twice and make cancelling one of them drop a paying customer.
            if (await billing.HasSubscriptionAsync(tenantId, ct))
                return Results.Conflict(new { error = "already subscribed; use Manage billing to change or cancel" });

            var tenantIdText = tenantId.ToString();
            var customerId = await billing.GetCustomerIdAsync(tenantId, ct);
            var service = new SessionService();
            var session = await service.CreateAsync(new SessionCreateOptions
            {
                Mode = "subscription",
                SuccessUrl = cfg.SuccessUrl,
                CancelUrl = cfg.CancelUrl,
                Customer = string.IsNullOrWhiteSpace(customerId) ? null : customerId,
                ClientReferenceId = tenantIdText,
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
                    [StripeBillingEvents.TenantIdMetadataKey] = tenantIdText
                },
                // Subscription events do not carry the checkout session's metadata, so the
                // tenant id rides on the subscription too.
                SubscriptionData = new SessionSubscriptionDataOptions
                {
                    Metadata = new Dictionary<string, string>
                    {
                        [StripeBillingEvents.TenantIdMetadataKey] = tenantIdText
                    }
                }
            }, cancellationToken: ct);

            log.LogInformation("Created Stripe checkout session for tenant {TenantId}", tenantId);
            return Results.Ok(new CheckoutSessionResponse(session.Url));
        })
        .WithName("CreateCheckoutSession")
        .WithOpenApiMetadata(
            OpenApiTags.Billing,
            "Create Stripe Checkout session",
            "Starts a Stripe Checkout session for the Pro plan. Requires `QikLog:Stripe:Enabled` and an authenticated tenant (OIDC JWT).")
        .WithTags(OpenApiTags.Tenants)
        .Produces<CheckoutSessionResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        app.MapPost("/v1/billing/portal", async (
            ITenantContext tenant,
            [FromServices] ITenantBillingService billing,
            IOptions<StripeOptions> options,
            ILogger<BillingLog> log,
            CancellationToken ct) =>
        {
            if (tenant.TenantId is not Guid tenantId)
                return Results.Unauthorized();

            var customerId = await billing.GetCustomerIdAsync(tenantId, ct);
            if (string.IsNullOrWhiteSpace(customerId))
                return Results.NotFound(new { error = "no billing account yet; upgrade to Pro first" });

            var portal = await new PortalSessionService().CreateAsync(new PortalSessionCreateOptions
            {
                Customer = customerId,
                ReturnUrl = options.Value.PortalReturnUrl
            }, cancellationToken: ct);

            log.LogInformation("Created Stripe portal session for tenant {TenantId}", tenantId);
            return Results.Ok(new PortalSessionResponse(portal.Url));
        })
        .WithName("CreatePortalSession")
        .WithOpenApiMetadata(
            OpenApiTags.Billing,
            "Open the Stripe Customer Portal",
            "Returns a one-time Stripe Customer Portal URL where the tenant can update payment details or cancel. 404 until the tenant has a billing account.")
        .Produces<PortalSessionResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound);

        app.MapPost(WebhookPath, async (
            HttpRequest request,
            [FromServices] ITenantBillingService billing,
            IOptions<StripeOptions> options,
            ILogger<BillingLog> log,
            CancellationToken ct) =>
        {
            var secret = options.Value.WebhookSecret;
            if (string.IsNullOrWhiteSpace(secret))
            {
                log.LogError("Stripe webhook received but QikLog:Stripe:WebhookSecret is not set");
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }

            // Unauthenticated until the signature checks out: refuse oversized bodies.
            var sizeLimit = request.HttpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature>();
            if (sizeLimit is { IsReadOnly: false })
                sizeLimit.MaxRequestBodySize = MaxWebhookBytes;
            if (request.ContentLength > MaxWebhookBytes)
                return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);

            using var reader = new StreamReader(request.Body);
            var payload = await reader.ReadToEndAsync(ct);
            var signature = request.Headers["Stripe-Signature"].ToString();

            Event stripeEvent;
            try
            {
                // Do not throw on API version mismatch: the dashboard endpoint's API version
                // can differ from the one Stripe.net is pinned to, and the fields we read
                // (ids, status, metadata) are stable across versions.
                stripeEvent = EventUtility.ConstructEvent(
                    payload,
                    signature,
                    secret,
                    throwOnApiVersionMismatch: false);
            }
            catch (StripeException ex)
            {
                log.LogWarning("Stripe webhook rejected: {Reason}", ex.Message);
                return Results.BadRequest(new { error = "invalid signature" });
            }
            catch (Exception ex) when (ex is Newtonsoft.Json.JsonException or NullReferenceException or ArgumentException)
            {
                // Signed by Stripe but not an event shape Stripe.net can read. 400 makes Stripe
                // retry and surfaces the delivery as failed in the dashboard instead of a 500.
                log.LogError("Stripe webhook payload could not be parsed: {Error}", ex.GetType().Name);
                return Results.BadRequest(new { error = "unreadable event" });
            }

            var change = StripeBillingEvents.ToChange(stripeEvent)
                ?? new BillingChange(stripeEvent.Id, stripeEvent.Type, null, null, null, null);
            var outcome = await billing.ApplyAsync(change, ct);

            log.LogInformation(
                "Stripe event {EventId} ({EventType}): {Outcome}",
                stripeEvent.Id,
                stripeEvent.Type,
                outcome);

            // 200 for every verified event, including unknown tenants, so Stripe stops retrying.
            return Results.Ok(new WebhookResponse(outcome.ToString()));
        })
        .WithName("StripeWebhook")
        .WithOpenApiMetadata(
            OpenApiTags.Billing,
            "Stripe webhook",
            "Receives Stripe events. Authenticated by the Stripe-Signature header, not by QikLog credentials.")
        .Produces<WebhookResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }
}

/// <summary>Stripe Checkout redirect URL.</summary>
internal sealed record CheckoutSessionResponse(string? Url);

/// <summary>Stripe Customer Portal URL.</summary>
internal sealed record PortalSessionResponse(string? Url);

/// <summary>Current plan and this month's usage.</summary>
internal sealed record BillingStatusResponse(string Plan, long UsedThisMonth, long MonthlyLimit, bool HasBillingAccount);

/// <summary>How the webhook handled the event.</summary>
internal sealed record WebhookResponse(string Outcome);

internal sealed class BillingLog;
