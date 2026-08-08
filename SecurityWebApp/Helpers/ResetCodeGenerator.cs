using System.Security.Cryptography;
using System.Text;

namespace SecurityWebApp.Helpers;

public static class ResetCodeGenerator
{
    public static string GenerateSha1Code()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(32);
        var timestampBytes = Encoding.UTF8.GetBytes(DateTime.UtcNow.Ticks.ToString());
        var input = randomBytes.Concat(timestampBytes).ToArray();
        var hash = SHA1.HashData(input);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
