using SecurityWebApp.Data;
using SecurityWebApp.Models;
using Microsoft.EntityFrameworkCore;

public class LoginAttemptManager
{
    private readonly ApplicationDbContext _context;
    private readonly int _attemptLimit = 3;
    private readonly int _lockoutDurationMinutes = 15;

    public LoginAttemptManager(ApplicationDbContext context, int attemptLimit = 3)
    {
        _context = context;
        _attemptLimit = attemptLimit;
    }

    public bool IsAccountLocked(User user)
    {
        if (!user.IsLocked)
            return false;

        if (user.LockedUntil.HasValue && DateTime.Now > user.LockedUntil.Value)
        {
            user.IsLocked = false;
            user.LoginAttempts = 0;
            _context.SaveChanges();
            return false;
        }

        return user.IsLocked;
    }

    public void RecordFailedAttempt(User user)
    {
        user.LoginAttempts++;
        user.LastFailedLoginAttempt = DateTime.Now;

        if (user.LoginAttempts >= _attemptLimit)
        {
            user.IsLocked = true;
            user.LockedUntil = DateTime.Now.AddMinutes(_lockoutDurationMinutes);
        }

        _context.SaveChanges();
    }

    public void ResetAttempts(User user)
    {
        user.LoginAttempts = 0;
        user.LastFailedLoginAttempt = null;
        user.IsLocked = false;
        user.LockedUntil = null;
        _context.SaveChanges();
    }
}
