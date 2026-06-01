using FsCheck;
using FsCheck.Xunit;
using QikLog.Core.Billing;
using Shouldly;
using Xunit;

namespace QikLog.Core.Tests;

public sealed class BillingMathPropertyTests
{
    [Property]
    public Property Annual_cost_never_exceeds_undiscounted_twelve_months(PositiveInt monthlyCents, int discountPercent)
    {
        var monthly = monthlyCents.Item;
        var discount = Math.Clamp(discountPercent, 0, 100);
        var annual = BillingMath.AnnualCostCents(monthly, discount);
        return (annual <= monthly * 12L).ToProperty();
    }

    [Property]
    public Property Annual_cost_matches_discount_formula(PositiveInt monthlyCents, int discountPercent)
    {
        var monthly = monthlyCents.Item;
        var discount = Math.Clamp(discountPercent, 0, 100);
        var expected = monthly * 12L - (monthly * 12L * discount / 100L);
        var annual = BillingMath.AnnualCostCents(monthly, discount);
        return (annual == expected).ToProperty();
    }

    [Property]
    public Property Usage_rollover_never_exceeds_max(PositiveInt limit, PositiveInt used, PositiveInt maxRollover)
    {
        var rollover = BillingMath.UsageRollover(limit.Item, used.Item, maxRollover.Item);
        return (rollover >= 0 && rollover <= maxRollover.Item).ToProperty();
    }

    [Property]
    public Property Effective_limit_includes_rollover(PositiveInt limit, PositiveInt rollover)
    {
        var effective = BillingMath.EffectiveMonthlyLimit(limit.Item, rollover.Item);
        return (effective == limit.Item + rollover.Item).ToProperty();
    }

    [Fact]
    public void Annual_cost_with_20_percent_discount()
    {
        BillingMath.AnnualCostCents(1000, 20).ShouldBe(9600);
    }

    [Fact]
    public void Usage_rollover_caps_unused_allowance()
    {
        BillingMath.UsageRollover(monthlyLimit: 10_000, used: 2_000, maxRollover: 1_000).ShouldBe(1_000);
    }
}
