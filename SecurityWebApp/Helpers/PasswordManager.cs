using Microsoft.Extensions.Options;

namespace SecurityWebApp.Helpers;

public class PasswordManager
{
    private readonly IOptionsMonitor<PasswordRules> _rules;
    private readonly HashSet<string> _commonPasswords = new(StringComparer.OrdinalIgnoreCase);

    // Version 1 built its own ConfigurationBuilder here on every request and resolved
    // the file against the current working directory. The rules now arrive through
    // the options pattern and the word list is read once, at start-up.
    public PasswordManager(IOptionsMonitor<PasswordRules> rules, IWebHostEnvironment environment)
    {
        _rules = rules;

        var path = Path.Combine(environment.ContentRootPath, "Properties", "commonPasswords.txt");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                "The common password list is required by PasswordRules.PreventDictionary.", path);
        }

        _commonPasswords.UnionWith(File.ReadLines(path)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith('#')));
    }

    // Read through the monitor so an edit to passwordOptions.json applies immediately.
    public PasswordRules Rules => _rules.CurrentValue;

    public bool IsPasswordStrong(string password, out string errorMessage)
    {
        var rules = Rules;
        errorMessage = string.Empty;

        if (string.IsNullOrEmpty(password) || password.Length < rules.RequiredLength)
        {
            errorMessage = $"Password must be at least {rules.RequiredLength} characters long.";
            return false;
        }
        if (rules.RequireUppercase && !password.Any(char.IsUpper))
        {
            errorMessage = "Password must contain at least one uppercase letter.";
            return false;
        }
        if (rules.RequireLowercase && !password.Any(char.IsLower))
        {
            errorMessage = "Password must contain at least one lowercase letter.";
            return false;
        }
        if (rules.RequireDigit && !password.Any(char.IsDigit))
        {
            errorMessage = "Password must contain at least one numeric digit.";
            return false;
        }
        if (rules.RequireSpecialCharacter && !password.Any(ch => rules.AllowedSpecialCharacters.Contains(ch)))
        {
            errorMessage = $"Password must contain at least one special character from: {rules.AllowedSpecialCharacters}";
            return false;
        }
        if (rules.PreventDictionary && IsDictionaryWord(password))
        {
            errorMessage = "Password is too common. Please use a stronger password.";
            return false;
        }

        return true;
    }

    // Version 1 rejected any password that *contained* a listed word, so "pass",
    // "test" or "user" anywhere in a long passphrase failed it. Compare the whole
    // password and its alphabetic core instead, which still catches "Password123!"
    // without rejecting "Blue7Horse!Coffee".
    private bool IsDictionaryWord(string password)
    {
        if (_commonPasswords.Contains(password))
            return true;

        var core = new string(password.Where(char.IsLetter).ToArray());
        return core.Length > 0 && _commonPasswords.Contains(core);
    }

    public bool CheckPasswordHistory(List<string> previousPasswordHashes, string newPassword)
    {
        if (previousPasswordHashes == null || previousPasswordHashes.Count == 0)
            return true;

        foreach (var hash in previousPasswordHashes)
        {
            if (PasswordHash.ValidatePassword(newPassword, hash))
                return false; // Password was used before
        }
        return true;
    }
}
