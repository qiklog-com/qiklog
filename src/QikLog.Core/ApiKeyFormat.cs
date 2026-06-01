using System.Security.Cryptography;

namespace QikLog.Core;

/// <summary>
/// Wire format for ingest API keys: <c>ql_{prefix}_{secret}</c> (prefix is 8 chars, grep-able).
/// </summary>
public static class ApiKeyFormat
{
    public const string Prefix = "ql_";
    public const int PrefixLength = 8;
    public const int SecretLength = 32;

    /// <summary>Creates a new key and its lookup prefix. Plaintext is returned once at creation.</summary>
    public static (string Plaintext, string LookupPrefix) Generate()
    {
        var prefix = GenerateSegment(PrefixLength);
        var secret = GenerateSegment(SecretLength);
        var plaintext = $"{Prefix}{prefix}_{secret}";
        return (plaintext, prefix);
    }

    /// <summary>Extracts the 8-char lookup prefix from a plaintext key.</summary>
    public static bool TryGetLookupPrefix(string plaintext, out string lookupPrefix)
    {
        lookupPrefix = "";
        if (string.IsNullOrWhiteSpace(plaintext))
            return false;

        var trimmed = plaintext.Trim();
        if (!trimmed.StartsWith(Prefix, StringComparison.Ordinal))
            return false;

        var rest = trimmed[Prefix.Length..];
        var underscore = rest.IndexOf('_');
        if (underscore != PrefixLength)
            return false;

        lookupPrefix = rest[..PrefixLength];
        return lookupPrefix.Length == PrefixLength
               && lookupPrefix.All(IsPrefixChar);
    }

    private static string GenerateSegment(int length)
    {
        const string alphabet = "abcdefghijklmnopqrstuvwxyz0123456789";
        Span<char> chars = stackalloc char[length];
        Span<byte> bytes = stackalloc byte[length];
        RandomNumberGenerator.Fill(bytes);
        for (var i = 0; i < length; i++)
            chars[i] = alphabet[bytes[i] % alphabet.Length];
        return new string(chars);
    }

    private static bool IsPrefixChar(char c) =>
        char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c);
}
