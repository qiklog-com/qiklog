using Shouldly;
using Xunit;

namespace QikLog.Core.Tests;

public sealed class ApiKeyFormatEdgeTests
{
    [Fact]
    public void TryGetLookupPrefix_accepts_trimmed_key()
    {
        var (plaintext, prefix) = ApiKeyFormat.Generate();
        ApiKeyFormat.TryGetLookupPrefix($"  {plaintext}  ", out var parsed).ShouldBeTrue();
        parsed.ShouldBe(prefix);
    }

    [Fact]
    public void TryGetLookupPrefix_rejects_uppercase_prefix_chars()
    {
        ApiKeyFormat.TryGetLookupPrefix("ql_ABCD1234_secretsecretsecretsecretse", out _).ShouldBeFalse();
    }

    [Theory]
    [InlineData("ql_short_secretsecretsecretsecretse")]
    [InlineData("ql_toolong12_secretsecretsecretsecretse")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("xk_abcdefgh_secretsecretsecretsecretse")]
    public void TryGetLookupPrefix_rejects_wrong_shapes(string plaintext)
    {
        ApiKeyFormat.TryGetLookupPrefix(plaintext, out _).ShouldBeFalse();
    }

    [Fact]
    public void Generate_produces_unique_keys()
    {
        var a = ApiKeyFormat.Generate();
        var b = ApiKeyFormat.Generate();
        a.Plaintext.ShouldNotBe(b.Plaintext);
        a.LookupPrefix.ShouldNotBe(b.LookupPrefix);
    }
}
