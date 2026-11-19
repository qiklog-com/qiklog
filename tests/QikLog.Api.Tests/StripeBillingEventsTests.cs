using QikLog.Api.Billing;
using Shouldly;
using Xunit;

namespace QikLog.Api.Tests;

/// <summary>Design contract for which Stripe subscription statuses change the plan.</summary>
public sealed class StripeBillingEventsTests
{
    [Theory]
    [InlineData("active", "pro")]
    [InlineData("trialing", "pro")]
    [InlineData("canceled", "free")]
    [InlineData("unpaid", "free")]
    [InlineData("incomplete_expired", "free")]
    [InlineData("paused", "free")]
    [InlineData("past_due", null)]
    [InlineData("incomplete", null)]
    [InlineData(null, null)]
    [InlineData("something_new", null)]
    public void Subscription_status_maps_to_plan(string? status, string? expectedPlan)
    {
        StripeBillingEvents.PlanForStatus(status).ShouldBe(expectedPlan);
    }
}
