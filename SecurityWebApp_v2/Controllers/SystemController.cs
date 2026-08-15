using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecurityWebApp.Data;
using SecurityWebApp.Helpers;
using SecurityWebApp.Models;
using SecurityWebApp.Services;

namespace SecurityWebApp.Controllers;

// Version 1 left this screen open to anonymous visitors, so anyone who guessed
// the URL could list, edit or delete every account.
[Authorize(Roles = AccountController.AdministratorRole)]
public class SystemController : Controller
{
    private const int PageSize = 10;

    private readonly ApplicationDbContext _db;
    private readonly PasswordManager _passwordManager;
    private readonly PasswordHistoryService _passwordHistory;
    private readonly PasswordPolicyWriter _policyWriter;
    private readonly ILogger<SystemController> _logger;

    public SystemController(
        ApplicationDbContext db,
        PasswordManager passwordManager,
        PasswordHistoryService passwordHistory,
        PasswordPolicyWriter policyWriter,
        ILogger<SystemController> logger)
    {
        _db = db;
        _passwordManager = passwordManager;
        _passwordHistory = passwordHistory;
        _policyWriter = policyWriter;
        _logger = logger;
    }

    public IActionResult Index() => RedirectToAction(nameof(Screen));

    // READ
    public async Task<IActionResult> Screen(string? q, int page = 1)
    {
        var query = _db.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(u => EF.Functions.Like(u.Email, $"%{term}%"));
        }

        var matching = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(matching / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);

        var users = await query
            .OrderBy(u => u.Id)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        // The summary counts describe the whole table, not the current page.
        ViewBag.TotalUsers = await _db.Users.CountAsync();
        ViewBag.LockedUsers = await _db.Users.CountAsync(u => u.IsLocked);
        ViewBag.NewThisWeek = await _db.Users.CountAsync(u => u.CreatedAt >= DateTime.UtcNow.AddDays(-7));

        ViewBag.Query = q;
        ViewBag.Page = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.Matching = matching;
        ViewBag.AdministratorCount = await _db.Users.CountAsync(u => u.Role == UserRole.Administrator);
        ViewBag.CurrentUserId = GetCurrentUserId();

        return View(users);
    }

    // CREATE
    [HttpPost]
    public async Task<IActionResult> Create(string? email, string? password, string? confirmPassword, UserRole role = UserRole.User)
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

        // Version 1 checked only a minimum length here, so this screen could create
        // accounts that the registration page would have rejected.
        if (!_passwordManager.IsPasswordStrong(password, out string validationError))
        {
            TempData["Error"] = validationError;
            return RedirectToAction(nameof(Screen));
        }

        if (await _db.Users.AnyAsync(u => u.Email == email))
        {
            TempData["Error"] = "A user with that email already exists.";
            return RedirectToAction(nameof(Screen));
        }

        var user = new User
        {
            Email = email,
            PasswordHash = PasswordHash.HashPassword(password),
            Role = role,
            CreatedAt = DateTime.UtcNow,
            LoginAttempts = 0,
            IsLocked = false
        };

        _db.Users.Add(user);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "A user with that email already exists.";
            return RedirectToAction(nameof(Screen));
        }

        await _passwordHistory.RecordAsync(user);

        _logger.LogInformation("Created user with id {Id}", user.Id);
        TempData["Success"] = "User created.";
        return RedirectToAction(nameof(Screen));
    }

    // UPDATE (email)
    [HttpPost]
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
    public async Task<IActionResult> Delete(int id)
    {
        if (GetCurrentUserId() == id)
        {
            TempData["Error"] = "You cannot delete the account you are signed in with.";
            return RedirectToAction(nameof(Screen));
        }

        var user = await _db.Users.FindAsync(id);
        if (user is null)
        {
            TempData["Error"] = "User not found.";
            return RedirectToAction(nameof(Screen));
        }

        if (await _db.Customers.AnyAsync(c => c.CreatedByUserId == id))
        {
            TempData["Error"] = "This user has customers on record and cannot be deleted.";
            return RedirectToAction(nameof(Screen));
        }

        if (user.Role == UserRole.Administrator && await IsLastAdministratorAsync(id))
        {
            TempData["Error"] = "This is the last administrator and cannot be deleted.";
            return RedirectToAction(nameof(Screen));
        }

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Deleted user with id {Id}", id);
        TempData["Success"] = "User deleted.";
        return RedirectToAction(nameof(Screen));
    }

    // ROLE
    [HttpPost]
    public async Task<IActionResult> SetRole(int id, UserRole role)
    {
        // Changing your own role from this screen is the easy way to lock everybody
        // out of it, so it is not allowed.
        if (GetCurrentUserId() == id)
        {
            TempData["Error"] = "You cannot change your own role.";
            return RedirectToAction(nameof(Screen));
        }

        var user = await _db.Users.FindAsync(id);
        if (user is null)
        {
            TempData["Error"] = "User not found.";
            return RedirectToAction(nameof(Screen));
        }

        if (user.Role == role)
        {
            return RedirectToAction(nameof(Screen));
        }

        if (user.Role == UserRole.Administrator && await IsLastAdministratorAsync(id))
        {
            TempData["Error"] = "This is the last administrator and cannot be demoted.";
            return RedirectToAction(nameof(Screen));
        }

        user.Role = role;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Set role {Role} on user {Id}", role, id);
        TempData["Success"] = $"User {id} is now a {role}.";
        return RedirectToAction(nameof(Screen));
    }

    // PASSWORD POLICY
    [HttpPost]
    public async Task<IActionResult> UpdatePolicy(PasswordRules rules)
    {
        if (!_policyWriter.Validate(rules, out var error))
        {
            TempData["Error"] = error;
            return RedirectToAction(nameof(Screen));
        }

        await _policyWriter.SaveAsync(rules);

        _logger.LogInformation("Password policy updated by user {Id}", GetCurrentUserId());
        TempData["Success"] = "Password policy saved. It applies to passwords set from now on.";
        return RedirectToAction(nameof(Screen));
    }

    // UNLOCK (reset lock state)
    [HttpPost]
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

            _logger.LogInformation("Unlocked user with id {Id}", id);
            TempData["Success"] = "User unlocked.";
        }

        return RedirectToAction(nameof(Screen));
    }

    private int? GetCurrentUserId()
    {
        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    }

    private async Task<bool> IsLastAdministratorAsync(int id)
    {
        return !await _db.Users.AnyAsync(u => u.Role == UserRole.Administrator && u.Id != id);
    }
}
