using QikLog.Infrastructure.Auth;
using Shouldly;
using Xunit;

namespace QikLog.Infrastructure.Tests;

public sealed class ApiKeyRateLimiterTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void TryAcquire_limit_zero_or_negative_always_allows(int limit)
    {
        var limiter = new ApiKeyRateLimiter();
        var id = Guid.NewGuid();
        for (var i = 0; i < 50; i++)
            limiter.TryAcquire(id, limit).ShouldBeTrue();
    }

    [Fact]
    public void TryAcquire_allows_up_to_limit_then_blocks()
    {
        var limiter = new ApiKeyRateLimiter();
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
        var limiter = new ApiKeyRateLimiter();
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
        var limiter = new ApiKeyRateLimiter();
        var id = Guid.NewGuid();

        limiter.TryAcquire(id, 1).ShouldBeTrue();
        limiter.TryAcquire(id, 1).ShouldBeFalse();
        limiter.TryAcquire(id, 1).ShouldBeFalse();
    }
}
