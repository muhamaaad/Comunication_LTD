
namespace SecurityWebApp.Helpers;
public class PasswordRules
{
    public int RequiredLength { get; set; } = 10;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireDigit { get; set; } = true;
    public bool RequireSpecialCharacter { get; set; } = true;
    public string AllowedSpecialCharacters { get; set; } = "!@#$%^&*()_+{}|[].?";
    public int PasswordHistoryLimit { get; set; } = 3;
    public bool PreventDictionary { get; set; } = true;
    public int LoginAttemptLimit { get; set; } = 3;
}
