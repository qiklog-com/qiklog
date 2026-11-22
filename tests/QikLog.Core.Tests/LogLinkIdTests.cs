using QikLog.Core.Links;
using Shouldly;
using Xunit;

namespace QikLog.Core.Tests;

public sealed class LogLinkIdTests
{
    [Fact]
    public void Given_new_ids_When_generated_Then_they_are_valid_and_distinct()
    {
        // Given / When: a batch of fresh ids
        var ids = Enumerable.Range(0, 200).Select(_ => LogLinkId.New()).ToList();

        // Then: every one is well formed and none repeat
        ids.ShouldAllBe(id => LogLinkId.IsValid(id));
        ids.Distinct().Count().ShouldBe(ids.Count);
    }

    [Fact]
    public void Given_the_alphabet_When_inspected_Then_it_is_long_enough_and_has_no_lookalikes()
    {
        // Then: 12 characters from 31 symbols is about 59 bits, and 0/1/i/l/o are absent
        LogLinkId.Length.ShouldBeGreaterThanOrEqualTo(12);
        LogLinkId.Alphabet.Length.ShouldBe(31);
        LogLinkId.Alphabet.Distinct().Count().ShouldBe(31);
        foreach (var ch in "01ilo")
            LogLinkId.Alphabet.ShouldNotContain(ch);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("k7mq")]
    [InlineData("k7mq2x9d4fab3")]
    [InlineData("K7MQ2X9D4FAB")]
    [InlineData("k7mq2x9d4fa0")]
    [InlineData("k7mq2x9d4f/b")]
    [InlineData("../../../etc")]
    public void Given_a_malformed_id_When_validated_Then_it_is_rejected(string? value)
    {
        LogLinkId.IsValid(value).ShouldBeFalse();
    }

    [Fact]
    public void Given_an_id_When_source_is_built_Then_it_carries_the_link_prefix()
    {
        LogLinkId.SourceFor("k7mq2x9d4fab").ShouldBe("link-k7mq2x9d4fab");
    }
}
