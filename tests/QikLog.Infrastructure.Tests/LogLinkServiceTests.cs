using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using QikLog.Core.Links;
using QikLog.Infrastructure.Data;
using QikLog.Infrastructure.Links;
using Shouldly;
using Xunit;
using LogLevel = QikLog.Core.LogLevel;

namespace QikLog.Infrastructure.Tests;

public sealed class LogLinkServiceTests
{
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 11, 21, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static QikLogDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<QikLogDbContext>()
            .UseInMemoryDatabase($"links_{Guid.NewGuid():N}")
            .Options);

    private static EfLogLinkService CreateService(QikLogDbContext db, Clock clock, LogLinkOptions? options = null) =>
        new(db, Options.Create(options ?? new LogLinkOptions()), clock, NullLogger<EfLogLinkService>.Instance);

    private static LinkLine[] Lines(params string[] messages) =>
        messages.Select(m => new LinkLine(LogLevel.Info, m)).ToArray();

    [Fact]
    public async Task Given_defaults_When_a_link_is_created_Then_it_lasts_24_hours_and_is_empty()
    {
        // Given
        await using var db = CreateDb();
        var clock = new Clock();
        var service = CreateService(db, clock);

        // When
        var link = await service.CreateAsync(CancellationToken.None);

        // Then
        LogLinkId.IsValid(link.Id).ShouldBeTrue();
        link.ExpiresAt.ShouldBe(clock.Now.AddHours(24));
        link.LineCount.ShouldBe(0);
        (await db.LogLinks.SingleAsync()).Id.ShouldBe(link.Id);
    }

    [Fact]
    public async Task Given_a_link_When_lines_are_appended_Then_they_are_stored_without_an_api_key_and_read_back_in_order()
    {
        // Given
        await using var db = CreateDb();
        var service = CreateService(db, new Clock());
        var link = await service.CreateAsync(CancellationToken.None);

        // When
        var first = await service.AppendAsync(link.Id, Lines("one", "two"), CancellationToken.None);
        var second = await service.AppendAsync(link.Id, [new LinkLine(LogLevel.Error, "three")], CancellationToken.None);
        var read = await service.ReadAsync(link.Id, 100, CancellationToken.None);

        // Then
        first.Status.ShouldBe(LogLinkStatus.Ok);
        first.Remaining.ShouldBe(498);
        second.Remaining.ShouldBe(497);
        read.Entries.Select(e => e.Message).ShouldBe(["one", "two", "three"]);
        read.Entries[2].Level.ShouldBe(LogLevel.Error);

        var rows = await db.LogEntries.ToListAsync();
        rows.ShouldAllBe(r => r.ApiKeyId == null && r.Source == LogLinkId.SourceFor(link.Id));
    }

    [Fact]
    public async Task Given_a_link_near_its_cap_When_more_lines_arrive_Then_only_the_room_left_is_used_and_then_it_is_full()
    {
        // Given: room for three lines
        await using var db = CreateDb();
        var service = CreateService(db, new Clock(), new LogLinkOptions { MaxLines = 3 });
        var link = await service.CreateAsync(CancellationToken.None);

        // When
        var partial = await service.AppendAsync(link.Id, Lines("a", "b", "c", "d"), CancellationToken.None);
        var full = await service.AppendAsync(link.Id, Lines("e"), CancellationToken.None);
        var read = await service.ReadAsync(link.Id, 100, CancellationToken.None);

        // Then: three stored, the fourth dropped, later sends refused, reading still works
        partial.Entries.Count.ShouldBe(3);
        partial.Remaining.ShouldBe(0);
        full.Status.ShouldBe(LogLinkStatus.Full);
        read.Status.ShouldBe(LogLinkStatus.Ok);
        read.Entries.Count.ShouldBe(3);
    }

    [Fact]
    public async Task Given_an_expired_link_When_used_Then_append_and_read_report_expired_and_nothing_is_stored()
    {
        // Given
        await using var db = CreateDb();
        var clock = new Clock();
        var service = CreateService(db, clock);
        var link = await service.CreateAsync(CancellationToken.None);
        clock.Now = clock.Now.AddHours(24).AddSeconds(1);

        // When
        var append = await service.AppendAsync(link.Id, Lines("late"), CancellationToken.None);
        var read = await service.ReadAsync(link.Id, 100, CancellationToken.None);

        // Then
        append.Status.ShouldBe(LogLinkStatus.Expired);
        read.Status.ShouldBe(LogLinkStatus.Expired);
        (await db.LogEntries.CountAsync()).ShouldBe(0);
    }

    [Theory]
    [InlineData("nope")]
    [InlineData("zzzzzzzzzzzz")]
    public async Task Given_an_unknown_or_malformed_id_When_used_Then_it_is_not_found(string id)
    {
        await using var db = CreateDb();
        var service = CreateService(db, new Clock());

        (await service.AppendAsync(id, Lines("x"), CancellationToken.None)).Status.ShouldBe(LogLinkStatus.NotFound);
        (await service.ReadAsync(id, 10, CancellationToken.None)).Status.ShouldBe(LogLinkStatus.NotFound);
    }

    [Fact]
    public async Task Given_a_tenant_row_with_the_same_source_name_When_the_link_is_read_Then_it_is_not_included()
    {
        // Given: a link, and a keyed (tenant) row that happens to use the link's source name
        await using var db = CreateDb();
        var service = CreateService(db, new Clock());
        var link = await service.CreateAsync(CancellationToken.None);
        await service.AppendAsync(link.Id, Lines("mine"), CancellationToken.None);
        db.LogEntries.Add(new LogEntryEntity
        {
            Source = LogLinkId.SourceFor(link.Id),
            Message = "tenant row",
            Level = LogLevel.Info,
            Timestamp = DateTimeOffset.UtcNow,
            ReceivedAt = DateTimeOffset.UtcNow,
            ApiKeyId = Guid.NewGuid()
        });
        await db.SaveChangesAsync();

        // When
        var read = await service.ReadAsync(link.Id, 100, CancellationToken.None);

        // Then
        read.Entries.ShouldHaveSingleItem().Message.ShouldBe("mine");
    }

    [Fact]
    public async Task Given_expired_and_live_links_When_purged_Then_only_expired_links_and_their_lines_go()
    {
        // Given
        await using var db = CreateDb();
        var clock = new Clock();
        var service = CreateService(db, clock);
        var old = await service.CreateAsync(CancellationToken.None);
        await service.AppendAsync(old.Id, Lines("old 1", "old 2"), CancellationToken.None);
        clock.Now = clock.Now.AddHours(23);
        var fresh = await service.CreateAsync(CancellationToken.None);
        await service.AppendAsync(fresh.Id, Lines("fresh"), CancellationToken.None);
        clock.Now = clock.Now.AddHours(2);

        // When
        var purged = await service.PurgeExpiredAsync(10, CancellationToken.None);

        // Then
        purged.ShouldBe(1);
        (await db.LogLinks.SingleAsync()).Id.ShouldBe(fresh.Id);
        (await db.LogEntries.SingleAsync()).Message.ShouldBe("fresh");
    }
}
