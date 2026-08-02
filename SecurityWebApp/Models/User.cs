namespace SecurityWebApp.Models;
public class User
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    // Password History
    public virtual ICollection<PasswordHistory> PasswordHistories { get; set; } = new List<PasswordHistory>();
    
    // Login Attempts
    public int LoginAttempts { get; set; } = 0;
    public DateTime? LastFailedLoginAttempt { get; set; }
    public bool IsLocked { get; set; } = false;
    public DateTime? LockedUntil { get; set; }
}

public class PasswordHistory
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string PasswordHash { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.Now;
    
    public virtual User User { get; set; }
}
