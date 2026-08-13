using SecurityWebApp.Helpers;
using SecurityWebApp.Data;
using SecurityWebApp.Models;
using Microsoft.AspNetCore.Mvc;

namespace SecurityWebApp.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;

    public AccountController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Login()
    {
        ViewBag.Success = TempData["Success"];
        return View();
    }

    [HttpPost]
    public IActionResult Login(string email, string password)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ViewBag.Error = "Email and password are required.";
            return View();
        }

        var user = _context.Users.FirstOrDefault(u => u.Email == email);

        if (user == null)
        {
            ViewBag.Error = "Invalid email or password.";
            return View();
        }

        // Check if account is locked
        var loginAttemptManager = new LoginAttemptManager(_context);
        if (loginAttemptManager.IsAccountLocked(user))
        {
            ViewBag.Error = $"Account is locked. Try again after 15 minutes.";
            return View();
        }

        // Verify password
        if (user != null && PasswordHash.ValidatePassword(password, user.PasswordHash))
        {
            // Login successful - reset attempts
            loginAttemptManager.ResetAttempts(user);
            return RedirectToAction("Index", "Home");
        }

        // Password is wrong - record failed attempt
        loginAttemptManager.RecordFailedAttempt(user);
        
        int remainingAttempts = 3 - user.LoginAttempts;
        if (remainingAttempts > 0)
        {
            ViewBag.Error = $"Invalid password. {remainingAttempts} attempts remaining.";
        }
        else
        {
            ViewBag.Error = "Account locked due to too many failed login attempts.";
        }
        return View();
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Register(string email, string password, string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ViewBag.Error = "Email and password are required";
            return View();
        }

        if (password != confirmPassword)
        {
            ViewBag.Error = "Passwords do not match";
            return View();
        }

        // Use PasswordManager for complex validation
        var passwordManager = new PasswordManager();
        if (!passwordManager.IsPasswordStrong(password, out string validationError))
        {
            ViewBag.Error = validationError;
            return View();
        }

        // Validate email format
        if (!IsValidEmail(email))
        {
            ViewBag.Error = "Invalid email format";
            return View();
        }

        if (_context.Users.Any(u => u.Email == email))
        {
            ViewBag.Error = "Email already registered";
            return View();
        }

        var user = new User
        {
            Email = email,
            PasswordHash = PasswordHash.HashPassword(password),
            LoginAttempts = 0,
            IsLocked = false
        };

        _context.Users.Add(user);
        _context.SaveChanges();

        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    public IActionResult ForgotPassword(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            ViewBag.Error = "Email or username is required";
            return View();
        }

        var user = _context.Users.FirstOrDefault(u => u.Email == identifier);
        if (user == null)
        {
            ViewBag.Error = "No account was found with this email or username";
            return View();
        }

        var resetToken = new PasswordResetToken
        {
            UserId = user.Id,
            ResetToken = ResetCodeGenerator.GenerateSha1Code(),
            Expiration = DateTime.UtcNow.AddMinutes(15),
            Used = false
        };

        _context.PasswordResetTokens.Add(resetToken);
        _context.SaveChanges();

        TempData["Success"] = "Code sent successfully!";
        TempData["ResetCode"] = resetToken.ResetToken;
        return RedirectToAction("VerifyResetCode");
    }

    [HttpGet]
    public IActionResult VerifyResetCode()
    {
        ViewBag.Success = TempData["Success"];
        ViewBag.ResetCode = TempData["ResetCode"];
        return View();
    }

    [HttpPost]
    public IActionResult VerifyResetCode(string resetCode)
    {
        if (string.IsNullOrWhiteSpace(resetCode))
        {
            ViewBag.Error = "Verification code is required";
            return View();
        }

        var token = _context.PasswordResetTokens.FirstOrDefault(t =>
            t.ResetToken == resetCode.Trim().ToLower() && !t.Used);

        if (token == null || token.Expiration < DateTime.UtcNow)
        {
            ViewBag.Error = "Invalid or expired verification code";
            return View();
        }

        HttpContext.Session.SetInt32("ResetUserId", token.UserId);
        HttpContext.Session.SetInt32("ResetTokenId", token.Id);

        return RedirectToAction("ResetPassword");
    }

    [HttpGet]
    public IActionResult ResetPassword()
    {
        if (HttpContext.Session.GetInt32("ResetUserId") == null)
        {
            return RedirectToAction("ForgotPassword");
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ResetPassword(string newPassword, string confirmPassword)
    {
        var userId = HttpContext.Session.GetInt32("ResetUserId");
        var tokenId = HttpContext.Session.GetInt32("ResetTokenId");

        if (userId == null || tokenId == null)
        {
            return RedirectToAction("ForgotPassword");
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

        var passwordManager = new PasswordManager();
        if (!passwordManager.IsPasswordStrong(newPassword, out string validationError))
        {
            ViewBag.Error = validationError;
            return View();
        }

        var user = _context.Users.FirstOrDefault(u => u.Id == userId.Value);
        var token = _context.PasswordResetTokens.FirstOrDefault(t => t.Id == tokenId.Value && !t.Used);

        if (user == null || token == null || token.Expiration < DateTime.UtcNow)
        {
            ViewBag.Error = "Password reset session is invalid or expired";
            return View();
        }

        user.PasswordHash = PasswordHash.HashPassword(newPassword);
        token.Used = true;
        _context.SaveChanges();

        HttpContext.Session.Remove("ResetUserId");
        HttpContext.Session.Remove("ResetTokenId");

        TempData["Success"] = "Password reset successfully. You can now log in.";
        return RedirectToAction("Login");
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

/*
public class AccountController : Controller
{
    // Displays the login page
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    // Handles form submission
    [HttpPost]
    public IActionResult Login(string username, string password)
    {
        // Add your login logic here
        if (username == "admin" && password == "password")
        {
            return RedirectToAction("Index", "Home");
        }
        
        ViewBag.Error = "Invalid login";
        return View();
    }
}
*/
