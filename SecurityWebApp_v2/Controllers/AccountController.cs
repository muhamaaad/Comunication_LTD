using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SecurityWebApp.Data;
using SecurityWebApp.Helpers;
using SecurityWebApp.Models;
using SecurityWebApp.Services;

namespace SecurityWebApp.Controllers;

public class AccountController : Controller
{
    public const string AdministratorRole = nameof(UserRole.Administrator);

    private const string ResetUserIdKey = "ResetUserId";
    private const string ResetTokenIdKey = "ResetTokenId";
    private const string ResetAttemptsKey = "ResetVerifyAttempts";
    private const int MaxResetVerifyAttempts = 5;

    // Every failed login says the same thing, so the form cannot be used to find
    // out which addresses are registered.
    private const string LoginFailureMessage = "Invalid email or password.";

    // Verifying against a throwaway hash costs the same as verifying a real one,
    // which keeps an unknown address from answering faster than a known one.
    private static readonly string DummyPasswordHash = PasswordHash.HashPassword(Guid.NewGuid().ToString());

    private readonly ApplicationDbContext _context;
    private readonly PasswordManager _passwordManager;
    private readonly LoginAttemptManager _loginAttemptManager;
    private readonly PasswordHistoryService _passwordHistory;
    private readonly IEmailService _emailService;
    private readonly PasswordResetPolicy _resetPolicy;
    private readonly LoginPolicy _loginPolicy;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        ApplicationDbContext context,
        PasswordManager passwordManager,
        LoginAttemptManager loginAttemptManager,
        PasswordHistoryService passwordHistory,
        IEmailService emailService,
        IOptionsSnapshot<PasswordResetPolicy> resetPolicy,
        IOptionsSnapshot<LoginPolicy> loginPolicy,
        ILogger<AccountController> logger)
    {
        _context = context;
        _passwordManager = passwordManager;
        _loginAttemptManager = loginAttemptManager;
        _passwordHistory = passwordHistory;
        _emailService = emailService;
        _resetPolicy = resetPolicy.Value;
        _loginPolicy = loginPolicy.Value;
        _logger = logger;
    }

    // ---------------------------------------------------------------- Login

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.Success = TempData["Success"];
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ViewBag.Error = "Email and password are required.";
            return View();
        }

        email = email.Trim();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
        {
            PasswordHash.ValidatePassword(password, DummyPasswordHash);
            ViewBag.Error = LoginFailureMessage;
            return View();
        }

        if (_loginAttemptManager.IsAccountLocked(user))
        {
            ViewBag.Error = $"Account is locked. Try again after {_loginPolicy.LockoutMinutes} minutes.";
            return View();
        }

        if (!PasswordHash.ValidatePassword(password, user.PasswordHash))
        {
            _loginAttemptManager.RecordFailedAttempt(user);

            ViewBag.Error = user.IsLocked
                ? "Account locked due to too many failed login attempts."
                : LoginFailureMessage;
            return View();
        }

        _loginAttemptManager.ResetAttempts(user);
        user.LastLogin = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Drop anything a previous visitor left in the session before issuing
        // the authentication cookie.
        HttpContext.Session.Clear();
        await SignInAsync(user);

        _logger.LogInformation("User {UserId} signed in.", user.Id);

        return RedirectToLocal(returnUrl);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    // ------------------------------------------------------------- Register

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(string email, string password, string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ViewBag.Error = "Email and password are required";
            return View();
        }

        email = email.Trim();

        if (!new EmailAddressAttribute().IsValid(email))
        {
            ViewBag.Error = "Invalid email format";
            return View();
        }

        if (password != confirmPassword)
        {
            ViewBag.Error = "Passwords do not match";
            return View();
        }

        if (!_passwordManager.IsPasswordStrong(password, out string validationError))
        {
            ViewBag.Error = validationError;
            return View();
        }

        if (await _context.Users.AnyAsync(u => u.Email == email))
        {
            ViewBag.Error = "Email already registered";
            return View();
        }

        var user = new User
        {
            Email = email,
            PasswordHash = PasswordHash.HashPassword(password),
            Role = await ResolveInitialRoleAsync(email),
            CreatedAt = DateTime.UtcNow,
            LoginAttempts = 0,
            IsLocked = false
        };

        _context.Users.Add(user);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // The unique index is the real guard; the check above only produces
            // a friendlier message when there is no race.
            ViewBag.Error = "Email already registered";
            return View();
        }

        await _passwordHistory.RecordAsync(user);

        await TrySendAsync(
            () => _emailService.SendWelcomeAsync(user.Email),
            "welcome email",
            user.Id);

        _logger.LogInformation("Registered user {UserId}.", user.Id);

        TempData["Success"] = "Account created. You can now log in.";
        return RedirectToAction(nameof(Login));
    }

    // ------------------------------------------------------ Change password

    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword()
    {
        return View();
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        if (string.IsNullOrWhiteSpace(currentPassword) ||
            string.IsNullOrWhiteSpace(newPassword) ||
            string.IsNullOrWhiteSpace(confirmPassword))
        {
            ViewBag.Error = "All fields are required";
            return View();
        }

        if (!PasswordHash.ValidatePassword(currentPassword, user.PasswordHash))
        {
            ViewBag.Error = "Current password is incorrect";
            return View();
        }

        if (newPassword != confirmPassword)
        {
            ViewBag.Error = "Passwords do not match";
            return View();
        }

        if (!_passwordManager.IsPasswordStrong(newPassword, out string validationError))
        {
            ViewBag.Error = validationError;
            return View();
        }

        if (!await _passwordHistory.IsPasswordUnusedAsync(user.Id, newPassword))
        {
            ViewBag.Error = $"You cannot reuse any of your last {_passwordHistory.HistoryLimit} passwords.";
            return View();
        }

        await _passwordHistory.ApplyNewPasswordAsync(user, newPassword);
        _logger.LogInformation("User {UserId} changed their password.", user.Id);

        ViewBag.Success = "Your password has been changed.";
        return View();
    }

    // ------------------------------------------------------ Forgot password

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        ViewBag.Error = TempData["Error"];
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ForgotPassword(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            ViewBag.Error = "Email address is required";
            return View();
        }

        identifier = identifier.Trim();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == identifier);

        // The answer is the same whether or not the address is registered, so this
        // page cannot be used to discover accounts. Version 1 said "No account was
        // found with this email".
        if (user != null)
        {
            // A new request replaces any code that is still outstanding.
            var outstanding = await _context.PasswordResetTokens
                .Where(t => t.UserId == user.Id && !t.Used)
                .ToListAsync();

            foreach (var previous in outstanding)
            {
                previous.Used = true;
            }

            var resetToken = new PasswordResetToken
            {
                UserId = user.Id,
                ResetToken = ResetCodeGenerator.GenerateSha1Code(),
                Expiration = DateTime.UtcNow.AddMinutes(_resetPolicy.TokenExpirationMinutes),
                Used = false
            };

            _context.PasswordResetTokens.Add(resetToken);
            await _context.SaveChangesAsync();

            var sent = await TrySendAsync(
                () => _emailService.SendPasswordResetCodeAsync(
                    user.Email, resetToken.ResetToken, _resetPolicy.TokenExpirationMinutes),
                "password reset code",
                user.Id);

            if (!sent)
            {
                ViewBag.Error = "We could not send the email right now. Please try again in a moment.";
                return View();
            }
        }

        HttpContext.Session.SetInt32(ResetAttemptsKey, 0);

        TempData["Success"] = "If that address is registered, a verification code is on its way.";
        return RedirectToAction(nameof(VerifyResetCode));
    }

    [HttpGet]
    public IActionResult VerifyResetCode()
    {
        ViewBag.Success = TempData["Success"];
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> VerifyResetCode(string resetCode)
    {
        if (string.IsNullOrWhiteSpace(resetCode))
        {
            ViewBag.Error = "Verification code is required";
            return View();
        }

        // Without a ceiling a 40-character code is still guessable given enough tries.
        var attempts = HttpContext.Session.GetInt32(ResetAttemptsKey) ?? 0;
        if (attempts >= MaxResetVerifyAttempts)
        {
            HttpContext.Session.Remove(ResetAttemptsKey);
            TempData["Error"] = "Too many incorrect codes. Please request a new one.";
            return RedirectToAction(nameof(ForgotPassword));
        }

        var candidate = resetCode.Trim().ToLowerInvariant();
        var token = await _context.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.ResetToken == candidate && !t.Used);

        if (token == null || token.Expiration < DateTime.UtcNow)
        {
            HttpContext.Session.SetInt32(ResetAttemptsKey, attempts + 1);
            ViewBag.Error = "Invalid or expired verification code";
            return View();
        }

        HttpContext.Session.Remove(ResetAttemptsKey);
        HttpContext.Session.SetInt32(ResetUserIdKey, token.UserId);
        HttpContext.Session.SetInt32(ResetTokenIdKey, token.Id);

        return RedirectToAction(nameof(ResetPassword));
    }

    [HttpGet]
    public IActionResult ResetPassword()
    {
        if (HttpContext.Session.GetInt32(ResetUserIdKey) == null)
        {
            return RedirectToAction(nameof(ForgotPassword));
        }

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(string newPassword, string confirmPassword)
    {
        var userId = HttpContext.Session.GetInt32(ResetUserIdKey);
        var tokenId = HttpContext.Session.GetInt32(ResetTokenIdKey);

        if (userId == null || tokenId == null)
        {
            return RedirectToAction(nameof(ForgotPassword));
        }

        if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
        {
            ViewBag.Error = "New password and confirmation are required";
            return View();
        }

        if (newPassword != confirmPassword)
        {
            ViewBag.Error = "Passwords do not match";
            return View();
        }

        if (!_passwordManager.IsPasswordStrong(newPassword, out string validationError))
        {
            ViewBag.Error = validationError;
            return View();
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId.Value);

        // The token has to belong to the user the session was issued for.
        var token = await _context.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.Id == tokenId.Value && t.UserId == userId.Value && !t.Used);

        if (user == null || token == null || token.Expiration < DateTime.UtcNow)
        {
            ClearResetSession();
            ViewBag.Error = "Password reset session is invalid or expired";
            return View();
        }

        if (!await _passwordHistory.IsPasswordUnusedAsync(user.Id, newPassword))
        {
            ViewBag.Error = $"You cannot reuse any of your last {_passwordHistory.HistoryLimit} passwords.";
            return View();
        }

        token.Used = true;
        await _passwordHistory.ApplyNewPasswordAsync(user, newPassword);

        // A completed reset also clears the lockout, otherwise the user still
        // cannot get in with the password they just chose.
        _loginAttemptManager.ResetAttempts(user);

        ClearResetSession();
        _logger.LogInformation("User {UserId} completed a password reset.", user.Id);

        TempData["Success"] = "Password reset successfully. You can now log in.";
        return RedirectToAction(nameof(Login));
    }

    // --------------------------------------------------------------- Shared

    // Bootstrap only: somebody has to be able to reach the System screen on a fresh
    // database. Once an administrator exists, roles are managed there and this rule
    // never fires again.
    private async Task<UserRole> ResolveInitialRoleAsync(string email)
    {
        var isSeedAddress = _loginPolicy.AdminEmails
            .Any(a => string.Equals(a, email, StringComparison.OrdinalIgnoreCase));

        if (!isSeedAddress)
        {
            return UserRole.User;
        }

        var administratorExists = await _context.Users.AnyAsync(u => u.Role == UserRole.Administrator);
        return administratorExists ? UserRole.User : UserRole.Administrator;
    }

    private async Task SignInAsync(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(id, out var userId))
        {
            return null;
        }

        return await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
    }

    private async Task<bool> TrySendAsync(Func<Task> send, string description, int userId)
    {
        try
        {
            await send();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not send {Description} for user {UserId}.", description, userId);
            return false;
        }
    }

    private void ClearResetSession()
    {
        HttpContext.Session.Remove(ResetUserIdKey);
        HttpContext.Session.Remove(ResetTokenIdKey);
        HttpContext.Session.Remove(ResetAttemptsKey);
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        // Url.IsLocalUrl keeps "returnUrl=https://evil.example" from turning the
        // login form into an open redirect.
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }
}
