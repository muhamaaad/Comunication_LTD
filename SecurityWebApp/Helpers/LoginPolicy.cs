namespace SecurityWebApp.Helpers;

public class LoginPolicy
{
    public int LockoutMinutes { get; set; } = 60;
    public int SessionTimeoutMinutes { get; set; } = 20;
}
