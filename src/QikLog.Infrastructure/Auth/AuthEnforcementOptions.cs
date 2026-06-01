namespace QikLog.Infrastructure.Auth;

/// <summary>When enabled, protected API routes require tenant-scoped authentication (no global fallback).</summary>
public sealed class AuthEnforcementOptions
{
    public const string SectionName = "QikLog:AuthEnforcement";

    public bool Enabled { get; set; } = true;
}
