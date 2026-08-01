namespace SecurityWebApp.Helpers;

using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;

public class PasswordManager
{
    private readonly PasswordRules _rules;

    public PasswordManager()
    {
        // Build the configuration pipeline
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("Properties/passwordOptions.json", optional: false, reloadOnChange: true)
            .Build();

        // Bind the JSON section directly to the C# object
        _rules = configuration.GetSection("PasswordRules").Get<PasswordRules>() ?? new PasswordRules();
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

        return true;
    }
}
