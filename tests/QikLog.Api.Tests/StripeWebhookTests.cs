using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QikLog.Api.Auth.Testing;
using QikLog.Infrastructure.Data;
using Shouldly;
using Xunit;

namespace QikLog.Api.Tests;

/// <summary>API host with Stripe billing switched on (test-mode values, no network).</summary>
public class StripeApiWebApplicationFactory : QikLogApiWebApplicationFactory
{
    public const string WebhookSecret = "whsec_test_qiklog";

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["QikLog:Stripe:Enabled"] = "true",
                ["QikLog:Stripe:SecretKey"] = "sk_test_not_used_offline",
                ["QikLog:Stripe:WebhookSecret"] = WebhookSecret,
                ["QikLog:Stripe:ProPriceId"] = "price_test_pro",
                ["QikLog:Usage:FreeIngestPerMonth"] = "2"
            });
        });
    }
}

/// <summary>Same host but the webhook signing secret is missing.</summary>
public sealed class StripeNoSecretWebApplicationFactory : QikLogApiWebApplicationFactory
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["QikLog:Stripe:Enabled"] = "true",
                ["QikLog:Stripe:SecretKey"] = "sk_test_not_used_offline"
            });
        });
    }
}

public sealed class StripeWebhookTests : IAsyncLifetime
{
    private const string WebhookUrl = "/v1/billing/webhook";

    private readonly StripeApiWebApplicationFactory _factory = new();
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        // No API key and no JWT: Stripe sends neither.
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    // Given a free tenant, When Stripe reports a paid checkout for it, Then the tenant is Pro
    // and remembers its Stripe customer and subscription.
    [Fact]
    public async Task Paid_checkout_upgrades_the_tenant_to_pro()
    {
        var response = await SendAsync(CheckoutCompleted("evt_checkout_1", "cus_1", "sub_1"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var tenant = await PrimaryTenantAsync();
        tenant.Plan.ShouldBe("pro");
        tenant.StripeCustomerId.ShouldBe("cus_1");
        tenant.StripeSubscriptionId.ShouldBe("sub_1");
        tenant.PlanChangedAt.ShouldNotBeNull();
    }

    // Given the tenant id only in client_reference_id, When checkout completes, Then it still upgrades.
    [Fact]
    public async Task Checkout_falls_back_to_client_reference_id()
    {
        var response = await SendAsync(CheckoutCompleted("evt_ref", "cus_ref", "sub_ref", useMetadata: false));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await PrimaryTenantAsync()).Plan.ShouldBe("pro");
    }

    // Given an async payment still pending, When checkout completes, Then the subscription is
    // recorded but Pro waits for the subscription to become active.
    [Fact]
    public async Task Unpaid_checkout_records_subscription_without_upgrading()
    {
        await SendAsync(CheckoutCompleted("evt_unpaid", "cus_2", "sub_2", paymentStatus: "unpaid"));

        var tenant = await PrimaryTenantAsync();
        tenant.Plan.ShouldBe("free");
        tenant.StripeSubscriptionId.ShouldBe("sub_2");

        await SendAsync(SubscriptionEvent("evt_active", "customer.subscription.updated", "sub_2", "cus_2", "active"));
        (await PrimaryTenantAsync()).Plan.ShouldBe("pro");
    }

    // Given an event already processed, When Stripe redelivers it, Then nothing changes twice.
    [Fact]
    public async Task Duplicate_event_is_a_no_op()
    {
        await SendAsync(CheckoutCompleted("evt_dup", "cus_3", "sub_3"));
        await SetPrimaryPlanAsync("free"); // prove the redelivery does not re-apply

        var again = await SendAsync(CheckoutCompleted("evt_dup", "cus_3", "sub_3"));

        again.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ReadOutcomeAsync(again)).ShouldBe("Duplicate");
        (await PrimaryTenantAsync()).Plan.ShouldBe("free");
    }

    // Given a forged or tampered request, When the signature does not verify, Then 400 and no change.
    [Fact]
    public async Task Bad_signature_is_rejected_and_changes_nothing()
    {
        var payload = CheckoutCompleted("evt_forged", "cus_x", "sub_x");
        using var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", Sign(payload, "whsec_wrong_secret"));

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await PrimaryTenantAsync()).Plan.ShouldBe("free");
    }

    // Given a correctly signed body that is not a Stripe event, Then 400 rather than a 500.
    [Fact]
    public async Task Signed_but_unreadable_payload_is_rejected()
    {
        var response = await SendAsync("""{"id":"evt_weird","object":"event"}""");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Missing_signature_is_rejected()
    {
        var response = await _client.PostAsync(
            WebhookUrl,
            new StringContent(CheckoutCompleted("evt_nosig", "cus", "sub"), Encoding.UTF8, "application/json"));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // Given a Pro tenant, When the subscription is deleted, Then the tenant drops to Free.
    [Fact]
    public async Task Deleted_subscription_downgrades_to_free()
    {
        await SendAsync(CheckoutCompleted("evt_up", "cus_4", "sub_4"));

        await SendAsync(SubscriptionEvent("evt_del", "customer.subscription.deleted", "sub_4", "cus_4", "canceled"));

        var tenant = await PrimaryTenantAsync();
        tenant.Plan.ShouldBe("free");
        tenant.StripeSubscriptionId.ShouldBeNull();
        tenant.StripeCustomerId.ShouldBe("cus_4");
    }

    // Given a Pro tenant, When a renewal fails and Stripe is retrying (past_due), Then the
    // tenant keeps Pro.
    [Fact]
    public async Task Past_due_does_not_downgrade()
    {
        await SendAsync(CheckoutCompleted("evt_up5", "cus_5", "sub_5"));

        await SendAsync(SubscriptionEvent("evt_pd", "customer.subscription.updated", "sub_5", "cus_5", "past_due"));

        (await PrimaryTenantAsync()).Plan.ShouldBe("pro");
    }

    // Given a Pro tenant, When Stripe gives up on payment (unpaid), Then the tenant drops to Free.
    [Fact]
    public async Task Unpaid_downgrades()
    {
        await SendAsync(CheckoutCompleted("evt_up6", "cus_6", "sub_6"));

        await SendAsync(SubscriptionEvent("evt_unp", "customer.subscription.updated", "sub_6", "cus_6", "unpaid"));

        (await PrimaryTenantAsync()).Plan.ShouldBe("free");
    }

    // Given a tenant that resubscribed, When a late event arrives for the old subscription,
    // Then the new subscription keeps Pro.
    [Fact]
    public async Task Late_event_for_an_old_subscription_is_ignored()
    {
        await SendAsync(CheckoutCompleted("evt_new", "cus_7", "sub_new"));

        var late = await SendAsync(SubscriptionEvent("evt_old_del", "customer.subscription.deleted", "sub_old", "cus_7", "canceled"));

        (await ReadOutcomeAsync(late)).ShouldBe("StaleSubscription");
        (await PrimaryTenantAsync()).Plan.ShouldBe("pro");
    }

    // Given a subscription that was cancelled, When Stripe retries an older "active" event for it,
    // Then the tenant stays Free (no free Pro from a retried event).
    [Fact]
    public async Task Retried_older_active_event_after_cancel_is_ignored()
    {
        await SendAsync(CheckoutCompleted("evt_o1", "cus_o", "sub_o", created: 1_800_000_100));
        await SendAsync(SubscriptionEvent("evt_o3", "customer.subscription.deleted", "sub_o", "cus_o", "canceled", created: 1_800_000_300));

        var late = await SendAsync(SubscriptionEvent("evt_o2", "customer.subscription.updated", "sub_o", "cus_o", "active", created: 1_800_000_200));

        (await ReadOutcomeAsync(late)).ShouldBe("OutOfOrder");
        (await PrimaryTenantAsync()).Plan.ShouldBe("free");
    }

    // Given a paying tenant, When an older "unpaid" event is retried after a newer "active",
    // Then the tenant keeps Pro.
    [Fact]
    public async Task Retried_older_unpaid_event_does_not_downgrade()
    {
        await SendAsync(CheckoutCompleted("evt_u1", "cus_u", "sub_u", created: 1_800_001_000));
        await SendAsync(SubscriptionEvent("evt_u3", "customer.subscription.updated", "sub_u", "cus_u", "active", created: 1_800_001_300));

        await SendAsync(SubscriptionEvent("evt_u2", "customer.subscription.updated", "sub_u", "cus_u", "unpaid", created: 1_800_001_200));

        (await PrimaryTenantAsync()).Plan.ShouldBe("pro");
    }

    // Given subscription events without our metadata, Then the tenant is found by customer id.
    [Fact]
    public async Task Subscription_events_match_by_customer_id()
    {
        await SendAsync(CheckoutCompleted("evt_c1", "cus_match", "sub_match"));
        var noMeta = Envelope("evt_c2", "customer.subscription.deleted",
            """{"id":"sub_match","object":"subscription","status":"canceled","customer":"cus_match","metadata":{} }""");

        await SendAsync(noMeta);

        (await PrimaryTenantAsync()).Plan.ShouldBe("free");
    }

    // Given a tenant with a live subscription, When it starts checkout again, Then 409 before Stripe is called.
    [Fact]
    public async Task Second_checkout_is_refused()
    {
        await SendAsync(CheckoutCompleted("evt_twice", "cus_t", "sub_t"));
        using var client = _factory.CreateClient();
        ApiTestAuth.SetValidJwt(client);

        var response = await client.PostAsync("/v1/billing/checkout", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Oversized_webhook_body_is_refused()
    {
        var payload = new string('x', 300 * 1024);
        using var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", Sign(payload, StripeApiWebApplicationFactory.WebhookSecret));

        (await _client.SendAsync(request)).StatusCode.ShouldBe(HttpStatusCode.RequestEntityTooLarge);
    }

    // Given an event for a tenant QikLog does not know, Then 200 so Stripe stops retrying.
    [Fact]
    public async Task Unknown_tenant_returns_ok()
    {
        var response = await SendAsync(SubscriptionEvent(
            "evt_ghost", "customer.subscription.updated", "sub_ghost", "cus_ghost", "active", tenantId: Guid.NewGuid()));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ReadOutcomeAsync(response)).ShouldBe("TenantNotFound");
    }

    [Fact]
    public async Task Unhandled_event_type_is_recorded_and_ok()
    {
        var payload = Envelope("evt_other", "invoice.created", """{"id":"in_1","object":"invoice"}""");

        var response = await SendAsync(payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ReadOutcomeAsync(response)).ShouldBe("NoChange");
    }

    // Given a Free tenant over its cap, Then ingest is blocked but history stays readable.
    [Fact]
    public async Task Over_cap_blocks_ingest_but_never_reads()
    {
        var apiKey = await ApiTestData.CreateApiKeyForPrimaryTenantAsync(_factory.Services);
        using var client = _factory.CreateClient();
        ApiTestAuth.SetApiKey(client, apiKey);

        for (var i = 0; i < 2; i++)
            (await client.PostAsync("/v1/logs", Json("""{"source":"cap","message":"ok"}"""))).StatusCode
                .ShouldBe(HttpStatusCode.Accepted);

        (await client.PostAsync("/v1/logs", Json("""{"source":"cap","message":"blocked"}"""))).StatusCode
            .ShouldBe(HttpStatusCode.PaymentRequired);

        var history = await client.GetAsync("/v1/sources/cap/logs?limit=10");
        history.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // Given a Free tenant over its cap, When it upgrades, Then ingest works again.
    [Fact]
    public async Task Upgrading_lifts_the_cap()
    {
        var apiKey = await ApiTestData.CreateApiKeyForPrimaryTenantAsync(_factory.Services);
        using var client = _factory.CreateClient();
        ApiTestAuth.SetApiKey(client, apiKey);
        for (var i = 0; i < 2; i++)
            await client.PostAsync("/v1/logs", Json("""{"source":"cap2","message":"ok"}"""));

        await SendAsync(CheckoutCompleted("evt_lift", "cus_8", "sub_8"));

        (await client.PostAsync("/v1/logs", Json("""{"source":"cap2","message":"now pro"}"""))).StatusCode
            .ShouldBe(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task Status_reports_plan_and_usage_for_the_signed_in_tenant()
    {
        using var client = _factory.CreateClient();
        ApiTestAuth.SetValidJwt(client);

        var status = await client.GetFromJsonAsync<JsonElement>("/v1/billing/status");

        status.GetProperty("plan").GetString().ShouldBe("free");
        status.GetProperty("monthlyLimit").GetInt64().ShouldBe(2);
        status.GetProperty("hasBillingAccount").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public async Task Status_requires_sign_in()
    {
        (await _client.GetAsync("/v1/billing/status")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // Given a tenant that never checked out, When it opens the portal, Then 404 (no Stripe call).
    [Fact]
    public async Task Portal_without_billing_account_is_not_found()
    {
        using var client = _factory.CreateClient();
        ApiTestAuth.SetValidJwt(client);

        var response = await client.PostAsync("/v1/billing/portal", null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Portal_requires_sign_in()
    {
        (await _client.PostAsync("/v1/billing/portal", null)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Missing_webhook_secret_returns_service_unavailable()
    {
        await using var factory = new StripeNoSecretWebApplicationFactory();
        using var client = factory.CreateClient();
        var payload = CheckoutCompleted("evt_cfg", "cus", "sub");
        using var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", Sign(payload, StripeApiWebApplicationFactory.WebhookSecret));

        (await client.SendAsync(request)).StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }

    // ---- helpers ----

    private async Task<HttpResponseMessage> SendAsync(string payload)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", Sign(payload, StripeApiWebApplicationFactory.WebhookSecret));
        return await _client.SendAsync(request);
    }

    /// <summary>Builds a Stripe-Signature header the same way Stripe does (HMAC-SHA256 of "t.payload").</summary>
    internal static string Sign(string payload, string secret)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{timestamp}.{payload}"));
        return $"t={timestamp},v1={Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    private static string CheckoutCompleted(
        string eventId,
        string customerId,
        string subscriptionId,
        string paymentStatus = "paid",
        bool useMetadata = true,
        long? created = null)
    {
        var tenant = TestTenants.Primary.ToString();
        var metadata = useMetadata ? $$"""{"tenant_id":"{{tenant}}"}""" : "{}";
        var session = $$"""
            {"id":"cs_test_{{eventId}}","object":"checkout.session","mode":"subscription",
             "payment_status":"{{paymentStatus}}","client_reference_id":"{{tenant}}",
             "customer":"{{customerId}}","subscription":"{{subscriptionId}}","metadata":{{metadata}} }
            """;
        return Envelope(eventId, "checkout.session.completed", session, created);
    }

    private static string SubscriptionEvent(
        string eventId,
        string type,
        string subscriptionId,
        string customerId,
        string status,
        Guid? tenantId = null,
        long? created = null)
    {
        var tenant = (tenantId ?? TestTenants.Primary).ToString();
        var subscription = $$"""
            {"id":"{{subscriptionId}}","object":"subscription","status":"{{status}}",
             "customer":"{{customerId}}","metadata":{"tenant_id":"{{tenant}}"} }
            """;
        return Envelope(eventId, type, subscription, created);
    }

    private static long _clock = 1763500000;

    private static string Envelope(string eventId, string type, string dataObject, long? created = null) =>
        $$"""
        {"id":"{{eventId}}","object":"event","api_version":"2024-12-18.acacia","created":{{created ?? Interlocked.Increment(ref _clock)}},
         "livemode":false,"pending_webhooks":1,"request":{"id":null,"idempotency_key":null},
         "type":"{{type}}","data":{"object":{{dataObject}},"previous_attributes":null } }
        """;

    private static StringContent Json(string body) => new(body, Encoding.UTF8, "application/json");

    private static async Task<string?> ReadOutcomeAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("outcome").GetString();
    }

    private async Task<TenantEntity> PrimaryTenantAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QikLogDbContext>();
        return await db.Tenants.AsNoTracking().SingleAsync(t => t.Id == TestTenants.Primary);
    }

    private async Task SetPrimaryPlanAsync(string plan)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QikLogDbContext>();
        var tenant = await db.Tenants.SingleAsync(t => t.Id == TestTenants.Primary);
        tenant.Plan = plan;
        await db.SaveChangesAsync();
    }
}
