using Microsoft.EntityFrameworkCore;
using SecurityWebApp.Data;
using SecurityWebApp.Helpers;
using SecurityWebApp.Models;

namespace SecurityWebApp.Services;

// Version 1 had a PasswordHistories table and a CheckPasswordHistory method that
// nothing ever called. Every place that sets a password goes through here instead.
public class PasswordHistoryService
{
    private readonly ApplicationDbContext _context;
    private readonly PasswordManager _passwordManager;

    public PasswordHistoryService(ApplicationDbContext context, PasswordManager passwordManager)
    {
        _context = context;
        _passwordManager = passwordManager;
    }

    public int HistoryLimit => _passwordManager.Rules.PasswordHistoryLimit;

    public async Task<bool> IsPasswordUnusedAsync(int userId, string newPassword)
    {
        if (HistoryLimit <= 0)
        {
            return true;
        }

        var recent = await RecentHashesQuery(userId)
            .Take(HistoryLimit)
            .Select(h => h.PasswordHash)
            .ToListAsync();

        return _passwordManager.CheckPasswordHistory(recent, newPassword);
    }

    public async Task ApplyNewPasswordAsync(User user, string newPassword)
    {
        user.PasswordHash = PasswordHash.HashPassword(newPassword);
        await RecordAsync(user);
    }

    // Used right after a user row is created, so the first password counts as history too.
    public async Task RecordAsync(User user)
    {
        _context.PasswordHistories.Add(new PasswordHistory
        {
            UserId = user.Id,
            PasswordHash = user.PasswordHash,
            ChangedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        if (HistoryLimit <= 0)
        {
            return;
        }

        var expired = await RecentHashesQuery(user.Id)
            .Skip(HistoryLimit)
            .ToListAsync();

        if (expired.Count > 0)
        {
            _context.PasswordHistories.RemoveRange(expired);
            await _context.SaveChangesAsync();
        }
    }

    private IOrderedQueryable<PasswordHistory> RecentHashesQuery(int userId)
    {
        return _context.PasswordHistories
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.ChangedAt)
            .ThenByDescending(h => h.Id);
    }
}
