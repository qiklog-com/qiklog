using System.Security.Cryptography;

namespace QikLog.Web.Demo;

/// <summary>Per-circuit source names for the try-it-now tail.</summary>
public static class DemoSourceNames
{
    private const string Alphabet = "abcdefghijklmnopqrstuvwxyz0123456789";

    /// <summary><c>demo-</c> plus 4 to 6 lowercase letters or digits.</summary>
    public static string Create()
    {
        var length = RandomNumberGenerator.GetInt32(4, 7);
        return Create(length, RandomNumberGenerator.GetInt32);
    }

    /// <summary>Builds a name with a caller-supplied die, for tests.</summary>
    public static string Create(int length, Func<int, int> next)
    {
        if (length is < 4 or > 6)
            throw new ArgumentOutOfRangeException(nameof(length));

        Span<char> chars = stackalloc char[length];
        for (var i = 0; i < length; i++)
            chars[i] = Alphabet[next(Alphabet.Length)];

        return "demo-" + new string(chars);
    }
}
