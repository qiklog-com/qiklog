using Microsoft.EntityFrameworkCore;
using QikLog.Infrastructure.Data;

namespace QikLog.Infrastructure.DataProtection;

/// <summary>Readiness check for Web: can the Data Protection key table be queried?</summary>
public static class DataProtectionKeyStoreProbe
{
    /// <param name="db">The key store context, or null when keys live on the file system.</param>
    public static async Task<KeyStoreProbeResult> CheckAsync(QikLogDbContext? db, CancellationToken cancellationToken)
    {
        if (db is null)
            return new KeyStoreProbeResult(IsReady: true, Status: "skipped");

        try
        {
            // Fails if Postgres is unreachable or data_protection_keys hasn't been migrated yet.
            await db.DataProtectionKeys.AsNoTracking().AnyAsync(cancellationToken);
            return new KeyStoreProbeResult(IsReady: true, Status: "ok");
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return new KeyStoreProbeResult(IsReady: false, Status: "unavailable");
        }
    }
}

public sealed record KeyStoreProbeResult(bool IsReady, string Status);
