using System.Text.Encodings.Web;
using System.Text.Json;
using SecurityWebApp.Helpers;

namespace SecurityWebApp.Services;

// Writes the policy back to Properties/passwordOptions.json. The file is registered
// with reloadOnChange, so the new rules apply from the next request onwards.
public class PasswordPolicyWriter
{
    private const int MinRequiredLength = 6;
    private const int MaxRequiredLength = 128;
    private const int MaxHistoryLimit = 24;
    private const int MaxAttemptLimit = 20;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        // The default encoder escapes '&' and '+' as & and +, which turns
        // the special-character list into something nobody can edit by hand. This
        // file is written to disk and never embedded in a page, and the value is
        // validated below, so the relaxed encoder is the right one here.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly string _path;

    public PasswordPolicyWriter(IWebHostEnvironment environment)
    {
        _path = Path.Combine(environment.ContentRootPath, "Properties", "passwordOptions.json");
    }

    public bool Validate(PasswordRules rules, out string error)
    {
        error = string.Empty;

        if (rules.RequiredLength < MinRequiredLength || rules.RequiredLength > MaxRequiredLength)
        {
            error = $"Minimum length must be between {MinRequiredLength} and {MaxRequiredLength}.";
            return false;
        }

        if (rules.PasswordHistoryLimit < 0 || rules.PasswordHistoryLimit > MaxHistoryLimit)
        {
            error = $"Password history limit must be between 0 and {MaxHistoryLimit}.";
            return false;
        }

        if (rules.LoginAttemptLimit < 1 || rules.LoginAttemptLimit > MaxAttemptLimit)
        {
            error = $"Login attempt limit must be between 1 and {MaxAttemptLimit}.";
            return false;
        }

        rules.AllowedSpecialCharacters = rules.AllowedSpecialCharacters?.Trim() ?? string.Empty;

        if (rules.RequireSpecialCharacter && rules.AllowedSpecialCharacters.Length == 0)
        {
            error = "Allowed special characters cannot be empty while a special character is required.";
            return false;
        }

        if (rules.AllowedSpecialCharacters.Any(char.IsLetterOrDigit))
        {
            error = "Allowed special characters must not contain letters or digits.";
            return false;
        }

        return true;
    }

    public async Task SaveAsync(PasswordRules rules, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(new PasswordOptionsFile { PasswordRules = rules }, SerializerOptions);

        // Write alongside the target and swap it in, so the reload watcher can never
        // pick up a half-written policy file.
        var temporary = _path + ".tmp";
        await File.WriteAllTextAsync(temporary, json, cancellationToken);
        File.Move(temporary, _path, overwrite: true);
    }

    private sealed class PasswordOptionsFile
    {
        public PasswordRules PasswordRules { get; set; } = new();
    }
}
