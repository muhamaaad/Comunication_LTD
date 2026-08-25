namespace SecurityWebApp.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    // Admins reach the System screen. Everyone else is a regular user.
    public bool IsAdmin { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }

    // Login Attempts
    public int LoginAttempts { get; set; } = 0;
    public DateTime? LastFailedLoginAttempt { get; set; }
    public bool IsLocked { get; set; } = false;
    public DateTime? LockedUntil { get; set; }
}
