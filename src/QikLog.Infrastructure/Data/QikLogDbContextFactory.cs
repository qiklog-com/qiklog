using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace QikLog.Infrastructure.Data;

/// <summary>Design-time factory for <c>dotnet ef</c> migrations.</summary>
public sealed class QikLogDbContextFactory : IDesignTimeDbContextFactory<QikLogDbContext>
{
    public QikLogDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? "Host=localhost;Database=qiklog;Username=qiklog;Password=qiklog_dev";

        var options = new DbContextOptionsBuilder<QikLogDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new QikLogDbContext(options);
    }
}
