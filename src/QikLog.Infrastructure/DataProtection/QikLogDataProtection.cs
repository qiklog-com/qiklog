using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using QikLog.Infrastructure.Data;

namespace QikLog.Infrastructure.DataProtection;

public static class QikLogDataProtection
{
    /// <summary>
    /// Fixed key-ring discriminator. Every Web instance and revision must use the same value,
    /// or cookies and antiforgery tokens from one revision can't be read by the next.
    /// </summary>
    public const string ApplicationName = "QikLog.Web";

    /// <summary>
    /// Registers Data Protection for Web. With a database, keys live in the
    /// <c>data_protection_keys</c> table so they survive restarts and are shared across
    /// replicas. Without one (local dev), keys go to <paramref name="fallbackKeysPath"/>.
    /// The caller registers <see cref="QikLogDbContext"/> when <paramref name="persistToDatabase"/> is true.
    /// </summary>
    public static IDataProtectionBuilder AddQikLogDataProtection(
        this IServiceCollection services,
        bool persistToDatabase,
        string fallbackKeysPath)
    {
        var builder = services.AddDataProtection().SetApplicationName(ApplicationName);

        if (persistToDatabase)
            return builder.PersistKeysToDbContext<QikLogDbContext>();

        Directory.CreateDirectory(fallbackKeysPath);
        return builder.PersistKeysToFileSystem(new DirectoryInfo(fallbackKeysPath));
    }
}
