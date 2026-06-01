using Shouldly;
using Xunit;

namespace QikLog.Core.Tests;

public class ApiKeyFormatTests
{
    [Fact]
    public void Generate_produces_parseable_key()
    {
        var (plaintext, prefix) = ApiKeyFormat.Generate();
        plaintext.ShouldStartWith("ql_");
        ApiKeyFormat.TryGetLookupPrefix(plaintext, out var parsed).ShouldBeTrue();
        parsed.ShouldBe(prefix);
    }

    [Fact]
    public void TryGetLookupPrefix_rejects_invalid_shapes()
    {
        ApiKeyFormat.TryGetLookupPrefix("not-a-key", out _).ShouldBeFalse();
        ApiKeyFormat.TryGetLookupPrefix("ql_short", out _).ShouldBeFalse();
    }
}
