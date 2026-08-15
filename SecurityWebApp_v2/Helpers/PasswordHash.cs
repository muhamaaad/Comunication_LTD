
using System.Security.Cryptography;

namespace SecurityWebApp.Helpers;

// PBKDF2 is HMAC applied over and over with a random salt, so calling it with
// HashAlgorithmName.SHA256 is HMAC-SHA256 based hashing - the requirement is met
// by the same code that was handed out, with only the parameters corrected.
public class PasswordHash
{
    public const int SaltByteSize = 24;
    public const int HashByteSize = 32; // full SHA-256 output, no truncation
    public const int Pbkdf2Iterations = 600000; // OWASP guidance for PBKDF2-HMAC-SHA256
    public const int IterationIndex = 0;
    public const int SaltIndex = 1;
    public const int Pbkdf2Index = 2;

    public static string HashPassword(string password)
    {
        byte[] salt = new byte[SaltByteSize];
        RandomNumberGenerator.Fill(salt);

        var hash = GetPbkdf2Bytes(password, salt, Pbkdf2Iterations, HashByteSize);
        return Pbkdf2Iterations + ":" +
               Convert.ToBase64String(salt) + ":" +
               Convert.ToBase64String(hash);
    }

    public static bool ValidatePassword(string password, string correctHash)
    {
        // A stored value that is missing or malformed is a failed login, not a crash.
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(correctHash))
        {
            return false;
        }

        var split = correctHash.Split(':');
        if (split.Length != 3 || !int.TryParse(split[IterationIndex], out var iterations) || iterations <= 0)
        {
            return false;
        }

        byte[] salt;
        byte[] hash;
        try
        {
            salt = Convert.FromBase64String(split[SaltIndex]);
            hash = Convert.FromBase64String(split[Pbkdf2Index]);
        }
        catch (FormatException)
        {
            return false;
        }

        if (hash.Length == 0)
        {
            return false;
        }

        var testHash = GetPbkdf2Bytes(password, salt, iterations, hash.Length);
        return SlowEquals(hash, testHash);
    }

    private static bool SlowEquals(byte[] a, byte[] b)
    {
        var diff = (uint)a.Length ^ (uint)b.Length;
        for (int i = 0; i < a.Length && i < b.Length; i++)
        {
            diff |= (uint)(a[i] ^ b[i]);
        }
        return diff == 0;
    }

    private static byte[] GetPbkdf2Bytes(string password, byte[] salt, int iterations, int outputBytes)
    {
        using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
        {
            return pbkdf2.GetBytes(outputBytes);
        }
    }
}
