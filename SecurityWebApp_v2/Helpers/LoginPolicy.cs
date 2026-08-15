namespace SecurityWebApp.Helpers;

public class LoginPolicy
{
    public int LockoutMinutes { get; set; } = 15;
    public int SessionTimeoutMinutes { get; set; } = 20;

    // Accounts that get the Administrator role claim when they sign in.
    public List<string> AdminEmails { get; set; } = new();
}