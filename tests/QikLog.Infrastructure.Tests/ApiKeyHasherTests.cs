using QikLog.Infrastructure.Auth;
using Shouldly;
using Xunit;

namespace QikLog.Infrastructure.Tests;

public sealed class ApiKeyHasherTests
{
    private readonly ApiKeyHasher _hasher = new();

    [Fact]
    public void Hash_and_Verify_roundtrip_succeeds()
    {
        var stored = _hasher.Hash("ql_abcdefgh_secretsecretsecretsecretse");
        _hasher.Verify("ql_abcdefgh_secretsecretsecretsecretse", stored).ShouldBeTrue();
    }

    [Fact]
    public void Verify_wrong_plaintext_returns_false()
    {
        var stored = _hasher.Hash("ql_abcdefgh_secretsecretsecretsecretse");
        _hasher.Verify("ql_abcdefgh_differentsecretsecretsecre", stored).ShouldBeFalse();
    }

    [Fact]
    public void Verify_malformed_stored_no_dot_returns_false()
    {
        _hasher.Verify("anything", "nodothere").ShouldBeFalse();
    }

    [Fact]
    public void Verify_invalid_base64_returns_false()
    {
        _hasher.Verify("anything", "!!!notbase64!!.!!!notbase64!!").ShouldBeFalse();
    }

    [Fact]
    public void Hash_produces_distinct_salts_for_same_input()
    {
        const string plaintext = "ql_abcdefgh_secretsecretsecretsecretse";
        var a = _hasher.Hash(plaintext);
        var b = _hasher.Hash(plaintext);
        a.ShouldNotBe(b);
        _hasher.Verify(plaintext, a).ShouldBeTrue();
        _hasher.Verify(plaintext, b).ShouldBeTrue();
    }

    [Fact]
    public void Verify_different_length_plaintext_returns_false()
    {
        var stored = _hasher.Hash("ql_abcdefgh_secretsecretsecretsecretse");
        _hasher.Verify("ql_abcdefgh_short", stored).ShouldBeFalse();
    }

    [Fact]
    public void Verify_empty_plaintext_returns_false_without_throwing()
    {
        var stored = _hasher.Hash("ql_abcdefgh_secretsecretsecretsecretse");
        _hasher.Verify("", stored).ShouldBeFalse();
    }
}
