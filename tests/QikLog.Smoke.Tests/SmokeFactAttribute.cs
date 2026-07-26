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

/// <summary>
/// A smoke test that additionally needs a real API key in <c>QIKLOG_SMOKE_API_KEY</c>.
/// </summary>
public sealed class AuthenticatedSmokeFactAttribute : FactAttribute
{
    /// <summary>Marks the test skipped when smoke runs or a key are unavailable.</summary>
    public AuthenticatedSmokeFactAttribute()
    {
        if (!SmokeEnvironment.Enabled)
            Skip = "Set QIKLOG_SMOKE=1 to run smoke tests against a live deployment.";
        else if (SmokeEnvironment.ApiKey is null)
            Skip = "Set QIKLOG_SMOKE_API_KEY to run authenticated round-trip checks.";
    }
}
