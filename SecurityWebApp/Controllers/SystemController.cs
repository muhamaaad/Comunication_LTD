using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecurityWebApp.Data;
using SecurityWebApp.Helpers;
using SecurityWebApp.Models;

namespace SecurityWebApp.Controllers;

public class SystemController : Controller
{
    private const int MinPasswordLength = 10;

    private readonly ApplicationDbContext _db;
    private readonly ILogger<SystemController> _logger;

    public SystemController(ApplicationDbContext db, ILogger<SystemController> logger)
    {
        _db = db;
        _logger = logger;
    }

    public IActionResult Index() => RedirectToAction(nameof(Screen));

    // READ
    public async Task<IActionResult> Screen()
    {
        var users = await _db.Users.OrderBy(u => u.Id).ToListAsync();
        return View(users);
    }

    // CREATE
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string? email, string? password, string? confirmPassword)
    {
        email = email?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email) || !new EmailAddressAttribute().IsValid(email))
        {
            TempData["Error"] = "Please enter a valid email address.";
            return RedirectToAction(nameof(Screen));
        }

        if (string.IsNullOrEmpty(password) || password != confirmPassword)
        {
            TempData["Error"] = "Password and confirmation are required and must match.";
            return RedirectToAction(nameof(Screen));
        }

        if (password.Length < MinPasswordLength)
        {
            TempData["Error"] = $"Password must be at least {MinPasswordLength} characters.";
            return RedirectToAction(nameof(Screen));
        }

        if (await _db.Users.AnyAsync(u => u.Email == email))
        {
            TempData["Error"] = $"A user with email {email} already exists.";
            return RedirectToAction(nameof(Screen));
        }

        var user = new User
        {
            Email = email,
            PasswordHash = PasswordHash.HashPassword(password),
            CreatedAt = DateTime.Now,
            LoginAttempts = 0,
            IsLocked = false
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var emailForLog = (user.Email ?? string.Empty)
            .Replace("\r", string.Empty)
            .Replace("\n", string.Empty);
        _logger.LogInformation("Created user {Email} (id {Id})", emailForLog, user.Id);
        TempData["Success"] = $"User {email} created.";
        return RedirectToAction(nameof(Screen));
    }

    // UPDATE (email)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, string? email)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null)
        {
            TempData["Error"] = "User not found.";
            return RedirectToAction(nameof(Screen));
        }

        email = email?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(email) || !new EmailAddressAttribute().IsValid(email))
        {
            TempData["Error"] = "Please enter a valid email address.";
            return RedirectToAction(nameof(Screen));
        }

        if (await _db.Users.AnyAsync(u => u.Email == email && u.Id != id))
        {
            TempData["Error"] = "Another user already uses that email.";
            return RedirectToAction(nameof(Screen));
        }

        user.Email = email;
        await _db.SaveChangesAsync();

        TempData["Success"] = $"User {id} updated.";
        return RedirectToAction(nameof(Screen));
    }

    // DELETE
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is not null)
        {
            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"User {user.Email} deleted.";
        }
        return RedirectToAction(nameof(Screen));
    }

    // UNLOCK (reset lock state)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is not null)
        {
            user.IsLocked = false;
            user.LoginAttempts = 0;
            user.LockedUntil = null;
            user.LastFailedLoginAttempt = null;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"User {user.Email} unlocked.";
        }
        return RedirectToAction(nameof(Screen));
    }
}