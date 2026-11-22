using QikLog.Api.Links;
using Shouldly;
using Xunit;

namespace QikLog.Api.Tests;

public sealed class LinkCreateLimiterTests
{
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 11, 21, 12, 10, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => Now;
    }

    [Fact]
    public void Given_a_limit_When_one_address_passes_it_Then_only_that_address_is_refused_until_the_next_hour()
    {
        // Given
        var clock = new Clock();
        var limiter = new LinkCreateLimiter(clock);

        // When / Then: two allowed, the third refused
        limiter.TryAcquire("203.0.113.1", 2).ShouldBeTrue();
        limiter.TryAcquire("203.0.113.1", 2).ShouldBeTrue();
        limiter.TryAcquire("203.0.113.1", 2).ShouldBeFalse();

        // Then: another address is unaffected
        limiter.TryAcquire("203.0.113.2", 2).ShouldBeTrue();

        // Then: the next clock hour starts a new window
        clock.Now = clock.Now.AddHours(1);
        limiter.TryAcquire("203.0.113.1", 2).ShouldBeTrue();
    }

    [Fact]
    public void Given_a_zero_limit_When_acquiring_Then_the_limiter_is_off()
    {
        var limiter = new LinkCreateLimiter(new Clock());

        for (var i = 0; i < 100; i++)
            limiter.TryAcquire("203.0.113.1", 0).ShouldBeTrue();
    }
}
