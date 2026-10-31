using NSubstitute;
using QikLog.Core;
using QikLog.Web.Services;
using QikLog.Web.Stage;
using Shouldly;
using Xunit;

namespace QikLog.Web.Tests;

public sealed class StageBufferTests
{
    [Fact]
    public void First_event_into_empty_stream_sets_Opening_and_not_Arrive()
    {
        var buffer = new StageBuffer();

        buffer.AppendLive([Entry("opening")]);

        buffer.Count.ShouldBe(1);
        buffer.Rows[0].Opening.ShouldBeTrue();
        buffer.Rows[0].Arrive.ShouldBeFalse();
    }

    [Fact]
    public void Subsequent_events_set_Arrive_and_not_Opening()
    {
        var buffer = new StageBuffer();

        buffer.AppendLive([Entry("first"), Entry("second"), Entry("third")]);

        buffer.Rows[0].Opening.ShouldBeTrue();
        buffer.Rows[0].Arrive.ShouldBeFalse();
        buffer.Rows[1].Opening.ShouldBeFalse();
        buffer.Rows[1].Arrive.ShouldBeTrue();
        buffer.Rows[2].Opening.ShouldBeFalse();
        buffer.Rows[2].Arrive.ShouldBeTrue();
    }

    [Fact]
    public async Task History_from_mocked_api_client_sets_neither_Arrive_nor_Opening()
    {
        var api = Substitute.For<IQikLogApiClient>();
        IReadOnlyList<LogEntry> returned = [Entry("h1"), Entry("h2"), Entry("h3")];
        api.GetSourceHistoryAsync("demo", Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(returned);

        var history = await api.GetSourceHistoryAsync("demo", StageBuffer.Cap, CancellationToken.None);
        var buffer = new StageBuffer();
        buffer.LoadHistory(history);

        buffer.Count.ShouldBe(3);
        buffer.Rows.ShouldAllBe(row => row.Arrive == false && row.Opening == false);
    }

    [Fact]
    public void Non_empty_stream_never_produces_Opening_row()
    {
        var buffer = new StageBuffer();
        buffer.LoadHistory([Entry("already here")]);

        buffer.AppendLive([Entry("live one"), Entry("live two")]);

        buffer.Rows.ShouldAllBe(row => row.Opening == false);
        buffer.Rows[0].Arrive.ShouldBeFalse();
        buffer.Rows[1].Arrive.ShouldBeTrue();
        buffer.Rows[2].Arrive.ShouldBeTrue();
    }

    [Fact]
    public void Pushing_past_BufferCap_drops_oldest()
    {
        var buffer = new StageBuffer();
        var overflow = 10;
        var total = StageBuffer.Cap + overflow;

        buffer.AppendLive(Enumerable.Range(1, total).Select(i => Entry($"m{i}")));

        buffer.Count.ShouldBe(StageBuffer.Cap);
        buffer.Rows[0].Entry.Message.ShouldBe($"m{overflow + 1}");
        buffer.Rows[^1].Entry.Message.ShouldBe($"m{total}");
    }

    private static LogEntry Entry(string message) =>
        new("demo", LogLevel.Info, message, DateTimeOffset.Parse("2026-03-08T14:05:06.123Z"));
}
