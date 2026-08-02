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