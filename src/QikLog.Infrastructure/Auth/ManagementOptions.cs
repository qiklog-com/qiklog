namespace QikLog.Infrastructure.Auth;

/// <summary>
/// Pre-identity management API (#13). Disabled in production until auth (#12) protects these routes.
/// </summary>
public sealed class ManagementOptions
{
    public const string SectionName = "QikLog:Management";

    public bool Enabled { get; set; }
}
