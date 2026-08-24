namespace SecurityWebApp.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Regular;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }

    // Login Attempts
    public int LoginAttempts { get; set; } = 0;
    public DateTime? LastFailedLoginAttempt { get; set; }
    public bool IsLocked { get; set; } = false;
    public DateTime? LockedUntil { get; set; }
}
