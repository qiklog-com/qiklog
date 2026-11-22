using System.Security.Cryptography;

namespace QikLog.Core.Links;

/// <summary>
/// Identifier for a log link: the short random part of <c>https://qiklog.com/l/{id}</c>.
/// The id is the only secret an anonymous link has, so it is long, random, and never logged.
/// </summary>
public static class LogLinkId
{
    /// <summary>Characters in an id. With <see cref="Alphabet"/> this is about 59 bits.</summary>
    public const int Length = 12;

    /// <summary>Lowercase letters and digits with the look-alikes (0, 1, i, l, o) removed.</summary>
    public const string Alphabet = "23456789abcdefghjkmnpqrstuvwxyz";

    /// <summary>Prefix of the source name that link lines are stored under.</summary>
    public const string SourcePrefix = "link-";

    public static string New() => RandomNumberGenerator.GetString(Alphabet, Length);

    public static bool IsValid(string? value)
    {
        if (value is null || value.Length != Length)
            return false;

        foreach (var ch in value)
        {
            if (!Alphabet.Contains(ch))
                return false;
        }

        return true;
    }

    /// <summary>Source name for a link's lines. Tenants cannot read it: link rows have no API key.</summary>
    public static string SourceFor(string id) => SourcePrefix + id;
}
