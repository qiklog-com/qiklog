using QikLog.Infrastructure.Auth;
using Shouldly;
using Xunit;

namespace QikLog.Infrastructure.Tests;

public sealed class ApiKeyRateLimiterTests
{
    // Mid-minute, so a slow test run can't cross into the next window.
    private static readonly DateTimeOffset MidMinute = new(2026, 1, 1, 12, 0, 30, TimeSpan.Zero);

    private static ApiKeyRateLimiter NewLimiter() => new(new ManualTimeProvider(MidMinute));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void TryAcquire_limit_zero_or_negative_always_allows(int limit)
    {
        var limiter = NewLimiter();
        var id = Guid.NewGuid();
        for (var i = 0; i < 50; i++)
            limiter.TryAcquire(id, limit).ShouldBeTrue();
    }

    [Fact]
    public void TryAcquire_allows_up_to_limit_then_blocks()
    {
        var limiter = NewLimiter();
        var id = Guid.NewGuid();
        const int limit = 3;

        limiter.TryAcquire(id, limit).ShouldBeTrue();
        limiter.TryAcquire(id, limit).ShouldBeTrue();
        limiter.TryAcquire(id, limit).ShouldBeTrue();
        limiter.TryAcquire(id, limit).ShouldBeFalse();
    }

    [Fact]
    public void TryAcquire_is_isolated_per_api_key_id()
    {
        var limiter = NewLimiter();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        const int limit = 1;

        limiter.TryAcquire(a, limit).ShouldBeTrue();
        limiter.TryAcquire(a, limit).ShouldBeFalse();
        limiter.TryAcquire(b, limit).ShouldBeTrue();
    }

    [Fact]
    public void TryAcquire_at_exact_limit_returns_true_then_false()
    {
        var limiter = NewLimiter();
        var id = Guid.NewGuid();

        limiter.TryAcquire(id, 1).ShouldBeTrue();
        limiter.TryAcquire(id, 1).ShouldBeFalse();
        limiter.TryAcquire(id, 1).ShouldBeFalse();
    }

    [Fact]
    public void Given_limit_reached_When_next_minute_starts_Then_requests_are_allowed_again()
    {
        // Given: a key that used its whole limit this minute
        var clock = new ManualTimeProvider(MidMinute);
        var limiter = new ApiKeyRateLimiter(clock);
        var id = Guid.NewGuid();
        limiter.TryAcquire(id, 1).ShouldBeTrue();
        limiter.TryAcquire(id, 1).ShouldBeFalse();

        // When: the clock moves into the next calendar minute
        clock.Now = MidMinute.AddSeconds(30);

        // Then: the count starts over
        limiter.TryAcquire(id, 1).ShouldBeTrue();
        limiter.TryAcquire(id, 1).ShouldBeFalse();
    }

    [Fact]
    public void Given_limit_reached_When_time_moves_within_the_same_minute_Then_requests_stay_blocked()
    {
        // Given: a key that used its whole limit this minute
        var clock = new ManualTimeProvider(MidMinute);
        var limiter = new ApiKeyRateLimiter(clock);
        var id = Guid.NewGuid();
        limiter.TryAcquire(id, 1).ShouldBeTrue();

        // When: time moves forward but stays in the same minute
        clock.Now = MidMinute.AddSeconds(29);

        // Then: the key is still blocked
        limiter.TryAcquire(id, 1).ShouldBeFalse();
    }

    [Fact]
    public void Given_no_time_provider_When_created_Then_uses_the_system_clock()
    {
        // Given / When: the production constructor
        var limiter = new ApiKeyRateLimiter();

        // Then: it still enforces the limit with the real clock
        var id = Guid.NewGuid();
        limiter.TryAcquire(id, 1000).ShouldBeTrue();
    }
}
