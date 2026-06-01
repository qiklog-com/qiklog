namespace QikLog.Core.Billing;

/// <summary>Pure billing calculations for plan pricing and monthly usage rollovers.</summary>
public static class BillingMath
{
    /// <summary>Annual price in cents from monthly price and optional annual discount (0–100).</summary>
    public static long AnnualCostCents(long monthlyPriceCents, int annualDiscountPercent)
    {
        if (monthlyPriceCents < 0)
            throw new ArgumentOutOfRangeException(nameof(monthlyPriceCents));

        var discount = Math.Clamp(annualDiscountPercent, 0, 100);
        var yearlyUndiscounted = monthlyPriceCents * 12L;
        return yearlyUndiscounted - (yearlyUndiscounted * discount / 100L);
    }

    /// <summary>Unused allowance carried into the next month, capped at <paramref name="maxRollover"/>.</summary>
    public static long UsageRollover(long monthlyLimit, long used, long maxRollover)
    {
        if (monthlyLimit < 0 || used < 0 || maxRollover < 0)
            throw new ArgumentOutOfRangeException();

        var unused = Math.Max(0, monthlyLimit - used);
        return Math.Min(unused, maxRollover);
    }

    /// <summary>Effective limit for a month including rolled-over allowance from the prior month.</summary>
    public static long EffectiveMonthlyLimit(long monthlyLimit, long rollover)
    {
        if (monthlyLimit < 0 || rollover < 0)
            throw new ArgumentOutOfRangeException();

        return monthlyLimit + rollover;
    }
}
