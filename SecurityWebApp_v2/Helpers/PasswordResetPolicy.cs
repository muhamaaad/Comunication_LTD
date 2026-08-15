namespace SecurityWebApp.Helpers;

public class PasswordResetPolicy
{
    public int TokenExpirationMinutes { get; set; } = 15;
}
