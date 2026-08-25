namespace SecurityWebApp.Helpers;

// Which characters a username may contain. The list is deliberately wide: the
// real defence against XSS is encoding the value when it is printed, not
// refusing it on the way in. Narrow the list here to tighten it.
public class UsernameRules
{
    // The Username column is 50 characters, so MaxLength cannot usefully go above it.
    public const int MaxSupportedLength = 50;

    public int MinLength { get; set; } = 3;
    public int MaxLength { get; set; } = 50;
    public bool AllowLetters { get; set; } = true;
    public bool AllowDigits { get; set; } = true;
    public string AllowedSpecialCharacters { get; set; } = "._-";

    public bool IsValid(string? username, out string errorMessage)
    {
        errorMessage = string.Empty;
        var maxLength = Math.Min(MaxLength, MaxSupportedLength);

        if (string.IsNullOrWhiteSpace(username) ||
            username.Length < MinLength ||
            username.Length > maxLength)
        {
            errorMessage = $"Username must be between {MinLength} and {maxLength} characters.";
            return false;
        }

        foreach (var character in username)
        {
            var allowed =
                (AllowLetters && char.IsLetter(character)) ||
                (AllowDigits && char.IsDigit(character)) ||
                AllowedSpecialCharacters.Contains(character);

            if (!allowed)
            {
                errorMessage = $"Username contains a character that is not allowed: {character}";
                return false;
            }
        }

        return true;
    }
}
