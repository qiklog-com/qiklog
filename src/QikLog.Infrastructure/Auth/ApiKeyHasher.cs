using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace QikLog.Infrastructure.Auth;

/// <summary>Argon2id hashing for API key secrets at rest.</summary>
public sealed class ApiKeyHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int DegreeOfParallelism = 4;
    private const int MemorySize = 65536;
    private const int Iterations = 3;

    public string Hash(string plaintext)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = HashWithSalt(plaintext, salt);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string plaintext, string stored)
    {
        if (string.IsNullOrEmpty(plaintext) || string.IsNullOrEmpty(stored))
            return false;

        var parts = stored.Split('.', 2);
        if (parts.Length != 2)
            return false;

        byte[] salt;
        byte[] expected;
        try
        {
            salt = Convert.FromBase64String(parts[0]);
            expected = Convert.FromBase64String(parts[1]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actual = HashWithSalt(plaintext, salt);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static byte[] HashWithSalt(string plaintext, byte[] salt)
    {
        var argon2 = new Argon2id(Encoding.UTF8.GetBytes(plaintext))
        {
            Salt = salt,
            DegreeOfParallelism = DegreeOfParallelism,
            MemorySize = MemorySize,
            Iterations = Iterations
        };
        return argon2.GetBytes(HashSize);
    }
}
