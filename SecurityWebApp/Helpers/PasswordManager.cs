namespace SecurityWebApp.Helpers;

using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

public class PasswordManager
{
    private readonly PasswordRules _rules;
    private readonly HashSet<string> _commonPasswords = new(StringComparer.OrdinalIgnoreCase);

    public PasswordManager()
    {
        // Build the configuration pipeline
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("Properties/passwordOptions.json", optional: false, reloadOnChange: true)
            .Build();

        // Bind the JSON section directly to the C# object
        _rules = configuration.GetSection("PasswordRules").Get<PasswordRules>() ?? new PasswordRules();
        
        // Load common passwords for dictionary prevention
        LoadCommonPasswords();
    }

    private void LoadCommonPasswords()
    {
        _commonPasswords.Clear();
        _commonPasswords.UnionWith(new[]
        {
            "password", "123456", "password123", "admin", "letmein", "welcome",
            "monkey", "dragon", "master", "sunshine", "princess", "qwerty",
            "abc123", "pass123", "12345678", "111111", "1234567", "123123",
            "test", "user", "root", "toor", "demo", "guest", "test123",
            "asdfgh", "zxcvbn", "qazwsx", "pass", "login", "hello"
        });
    }

    public bool IsPasswordStrong(string password, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (string.IsNullOrEmpty(password) || password.Length < _rules.RequiredLength)
        {
            errorMessage = $"Password must be at least {_rules.RequiredLength} characters long.";
            return false;
        }
        if (_rules.RequireUppercase && !password.Any(char.IsUpper))
        {
            errorMessage = "Password must contain at least one uppercase letter.";
            return false;
        }
        if (_rules.RequireLowercase && !password.Any(char.IsLower))
        {
            errorMessage = "Password must contain at least one lowercase letter.";
            return false;
        }
        if (_rules.RequireDigit && !password.Any(char.IsDigit))
        {
            errorMessage = "Password must contain at least one numeric digit.";
            return false;
        }
        if (_rules.RequireSpecialCharacter && !password.Any(ch => _rules.AllowedSpecialCharacters.Contains(ch)))
        {
            errorMessage = $"Password must contain at least one special character from: {_rules.AllowedSpecialCharacters}";
            return false;
        }
        if (_rules.PreventDictionary && IsDictionaryWord(password))
        {
            errorMessage = "Password is too common. Please use a stronger password.";
            return false;
        }

        return true;
    }

    private bool IsDictionaryWord(string password)
    {
        return _commonPasswords.Contains(password) || 
               _commonPasswords.Any(word => password.Contains(word, StringComparison.OrdinalIgnoreCase));
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
