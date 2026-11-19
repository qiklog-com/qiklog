using System.Net;
using System.Text;
using QikLog.Web.Services;
using Shouldly;
using Xunit;

namespace QikLog.Web.Tests;

/// <summary>Contract between the Billing page's API client and the billing endpoints.</summary>
public sealed class BillingApiClientTests
{
    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public HttpRequestMessage? Last { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Last = request;
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }

    private static (QikLogApiClient Client, StubHandler Handler) Create(HttpStatusCode status, string body)
    {
        var handler = new StubHandler(status, body);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.example.test/") };
        return (new QikLogApiClient(http), handler);
    }

    [Fact]
    public async Task Status_parses_plan_and_usage()
    {
        var (client, handler) = Create(HttpStatusCode.OK,
            """{"plan":"pro","usedThisMonth":1234,"monthlyLimit":500000,"hasBillingAccount":true}""");

        var status = await client.GetBillingStatusAsync(CancellationToken.None);

        handler.Last!.RequestUri!.AbsolutePath.ShouldBe("/v1/billing/status");
        status.ShouldNotBeNull();
        status.IsPro.ShouldBeTrue();
        status.UsedThisMonth.ShouldBe(1234);
        status.MonthlyLimit.ShouldBe(500000);
        status.HasBillingAccount.ShouldBeTrue();
    }

    [Fact]
    public async Task Status_is_null_when_refused()
    {
        var (client, _) = Create(HttpStatusCode.Unauthorized, "{}");

        (await client.GetBillingStatusAsync(CancellationToken.None)).ShouldBeNull();
    }

    [Fact]
    public async Task Checkout_returns_the_stripe_url()
    {
        var (client, handler) = Create(HttpStatusCode.OK, """{"url":"https://checkout.stripe.com/c/pay/cs_test"}""");

        var url = await client.StartCheckoutAsync(CancellationToken.None);

        handler.Last!.Method.ShouldBe(HttpMethod.Post);
        handler.Last.RequestUri!.AbsolutePath.ShouldBe("/v1/billing/checkout");
        url.ShouldBe("https://checkout.stripe.com/c/pay/cs_test");
    }

    [Fact]
    public async Task Portal_is_null_without_a_billing_account()
    {
        var (client, handler) = Create(HttpStatusCode.NotFound, """{"error":"no billing account yet"}""");

        (await client.OpenBillingPortalAsync(CancellationToken.None)).ShouldBeNull();
        handler.Last!.RequestUri!.AbsolutePath.ShouldBe("/v1/billing/portal");
    }
}
