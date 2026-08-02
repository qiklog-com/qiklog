using QikLog.Core.Billing;
using Shouldly;
using Xunit;

namespace QikLog.Core.Tests;

public sealed class BillingMathEdgeTests
{
    [Fact]
    public void AnnualCostCents_rejects_negative_monthly()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => BillingMath.AnnualCostCents(-1, 10));
    }

    [Fact]
    public void AnnualCostCents_clamps_discount_above_100()
    {
        BillingMath.AnnualCostCents(1000, 150).ShouldBe(0);
    }

    [Fact]
    public void AnnualCostCents_clamps_discount_below_zero()
    {
        BillingMath.AnnualCostCents(1000, -20).ShouldBe(12_000);
    }

    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(0, -1, 0)]
    [InlineData(0, 0, -1)]
    public void UsageRollover_rejects_negative_args(long limit, long used, long max)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => BillingMath.UsageRollover(limit, used, max));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    public void EffectiveMonthlyLimit_rejects_negative_args(long limit, long rollover)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => BillingMath.EffectiveMonthlyLimit(limit, rollover));
    }

    [Fact]
    public void UsageRollover_zero_when_fully_used()
    {
        BillingMath.UsageRollover(100, 100, 50).ShouldBe(0);
        BillingMath.UsageRollover(100, 150, 50).ShouldBe(0);
    }
}
