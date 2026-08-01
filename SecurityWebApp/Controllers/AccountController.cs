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
        var user = _context.Users.FirstOrDefault(u => u.Email == email);

        if (user != null && PasswordHash.ValidatePassword(password, user.PasswordHash))
        {
            return RedirectToAction("Index", "Home");
        }

        ViewBag.Error = "Invalid email or password";
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

        if (password.Length < 8)
        {
            ViewBag.Error = "Password must be at least 8 characters";
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
            PasswordHash = PasswordHash.HashPassword(password)
        };

        _context.Users.Add(user);
        _context.SaveChanges();

        ViewBag.Success = "Registration successful! You can now login.";
        return RedirectToAction("Login");
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