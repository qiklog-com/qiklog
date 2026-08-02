using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using QikLog.Core;
using QikLog.Infrastructure;
using QikLog.Infrastructure.Auth;
using QikLog.Infrastructure.Data;
using Shouldly;
using Xunit;

namespace QikLog.Infrastructure.Tests;

public sealed class EfLogEntryStoreTests
{
    [Fact]
    public async Task SaveAsync_persists_core_fields_and_ApiKeyId()
    {
        await using var db = CreateDb();
        var apiKeyId = Guid.NewGuid();
        var store = new EfLogEntryStore(
            db,
            new IngestContext { ApiKeyId = apiKeyId },
            NullLogger<EfLogEntryStore>.Instance);

        var ts = DateTimeOffset.Parse("2026-08-01T12:00:00Z");
        await store.SaveAsync(
            new LogEntry("api", LogLevel.Info, "hello", ts, new Dictionary<string, string> { ["k"] = "v" }),
            CancellationToken.None);

        var row = await db.LogEntries.SingleAsync();
        row.Source.ShouldBe("api");
        row.Level.ShouldBe(LogLevel.Info);
        row.Message.ShouldBe("hello");
        row.Timestamp.ShouldBe(ts);
        row.ApiKeyId.ShouldBe(apiKeyId);
        row.PropertiesJson.ShouldNotBeNull().ShouldContain("\"k\"");
        row.ReceivedAt.ShouldBeInRange(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(1));
    }

    [Theory]
    [InlineData(null)]
    public async Task SaveAsync_null_properties_stores_null_json(IReadOnlyDictionary<string, string>? properties)
    {
        await using var db = CreateDb();
        var store = new EfLogEntryStore(db, new IngestContext(), NullLogger<EfLogEntryStore>.Instance);
        await store.SaveAsync(
            new LogEntry("s", LogLevel.Debug, "m", DateTimeOffset.UtcNow, properties),
            CancellationToken.None);
        (await db.LogEntries.SingleAsync()).PropertiesJson.ShouldBeNull();
    }

    [Fact]
    public async Task SaveAsync_empty_properties_stores_null_json()
    {
        await using var db = CreateDb();
        var store = new EfLogEntryStore(db, new IngestContext(), NullLogger<EfLogEntryStore>.Instance);
        await store.SaveAsync(
            new LogEntry("s", LogLevel.Debug, "m", DateTimeOffset.UtcNow, new Dictionary<string, string>()),
            CancellationToken.None);
        (await db.LogEntries.SingleAsync()).PropertiesJson.ShouldBeNull();
    }

    [Fact]
    public void NullLogEntryStore_IsEnabled_false_and_Save_is_noop()
    {
        var store = new NullLogEntryStore();
        store.IsEnabled.ShouldBeFalse();
        Should.NotThrow(() =>
            store.SaveAsync(
                new LogEntry("s", LogLevel.Info, "m", DateTimeOffset.UtcNow),
                CancellationToken.None).GetAwaiter().GetResult());
    }

    private static QikLogDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<QikLogDbContext>()
            .UseInMemoryDatabase($"log-store-{Guid.NewGuid():N}")
            .Options;
        var db = new QikLogDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}
