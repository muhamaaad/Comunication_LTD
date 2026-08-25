using SecurityWebApp.Data;
using SecurityWebApp.Models;
using Microsoft.Extensions.Options;

namespace SecurityWebApp.Helpers;

public class LoginAttemptManager
{
    private readonly ApplicationDbContext _context;
    private readonly int _attemptLimit;
    private readonly int _lockoutDurationMinutes;

    public LoginAttemptManager(
        ApplicationDbContext context,
        IOptionsSnapshot<PasswordRules> rules,
        IOptionsSnapshot<LoginPolicy> loginPolicy)
    {
        _context = context;
        _attemptLimit = rules.Value.LoginAttemptLimit;
        _lockoutDurationMinutes = loginPolicy.Value.LockoutMinutes;
    }

    public bool IsAccountLocked(User user)
    {
        if (!user.IsLocked)
            return false;

        if (user.LockedUntil.HasValue && DateTime.UtcNow > user.LockedUntil.Value)
        {
            user.IsLocked = false;
            user.LoginAttempts = 0;
            user.LockedUntil = null;
            _context.SaveChanges();
            return false;
        }

        return user.IsLocked;
    }

    public void RecordFailedAttempt(User user)
    {
        user.LoginAttempts++;
        user.LastFailedLoginAttempt = DateTime.UtcNow;

        if (user.LoginAttempts >= _attemptLimit)
        {
            user.IsLocked = true;
            user.LockedUntil = DateTime.UtcNow.AddMinutes(_lockoutDurationMinutes);
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
