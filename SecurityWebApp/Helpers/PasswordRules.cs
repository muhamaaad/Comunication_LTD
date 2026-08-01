
namespace SecurityWebApp.Helpers;
public class PasswordRules
{
    public int RequiredLength { get; set; } = 12;
    public bool RequireUppercase { get; set; }
    public bool RequireLowercase { get; set; }
    public bool RequireDigit { get; set; }
    public bool RequireSpecialCharacter { get; set; }
    public string AllowedSpecialCharacters { get; set; } = string.Empty;
}
