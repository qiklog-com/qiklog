using Xunit;

namespace QikLog.Smoke.Tests;

/// <summary>
/// A <see cref="FactAttribute"/> that reports as skipped unless <c>QIKLOG_SMOKE=1</c>.
/// Keeps live-deployment checks out of the offline PR gate without silently passing.
/// </summary>
public sealed class SmokeFactAttribute : FactAttribute
{
    /// <summary>Marks the test skipped when smoke runs are not opted in.</summary>
    public SmokeFactAttribute()
    {
        if (!SmokeEnvironment.Enabled)
            Skip = "Set QIKLOG_SMOKE=1 to run smoke tests against a live deployment.";
    }
}
