using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecurityWebApp.Data;
using SecurityWebApp.Models;

namespace SecurityWebApp.Controllers;

// Adding a customer and showing the name back on the screen is the flow the
// vulnerable build uses to demonstrate SQL injection and stored XSS. Here every
// read and write goes through EF Core, which parameterises the SQL, and every
// value is rendered through Razor, which HTML-encodes it.
[Authorize]
public class CustomersController : Controller
{
    private const int MaxNameLength = 150;

    private static readonly Regex PhonePattern = new(@"^[0-9+()\-\s]{6,30}$", RegexOptions.Compiled);

    private readonly ApplicationDbContext _context;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(ApplicationDbContext context, ILogger<CustomersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? q)
    {
        var query = _context.Customers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            // EF turns this into a parameterised LIKE, not string concatenation.
            query = query.Where(c => EF.Functions.Like(c.Name, $"%{term}%")
                                  || EF.Functions.Like(c.Email!, $"%{term}%"));
        }

        var customers = await query
            .OrderByDescending(c => c.CreatedAt)
            .ThenByDescending(c => c.Id)
            .ToListAsync();

        ViewBag.Query = q;
        ViewBag.Success = TempData["Success"];
        ViewBag.Error = TempData["Error"];
        ViewBag.NewCustomerName = TempData["NewCustomerName"];

        return View(customers);
    }

    [HttpPost]
    public async Task<IActionResult> Create(string? name, string? email, string? phone)
    {
        name = name?.Trim() ?? string.Empty;
        email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();

        if (name.Length == 0)
        {
            TempData["Error"] = "Customer name is required.";
            return RedirectToAction(nameof(Index));
        }

        if (name.Length > MaxNameLength)
        {
            TempData["Error"] = $"Customer name must be {MaxNameLength} characters or fewer.";
            return RedirectToAction(nameof(Index));
        }

        if (email is not null && !new EmailAddressAttribute().IsValid(email))
        {
            TempData["Error"] = "Please enter a valid email address.";
            return RedirectToAction(nameof(Index));
        }

        if (phone is not null && !PhonePattern.IsMatch(phone))
        {
            TempData["Error"] = "Please enter a valid phone number.";
            return RedirectToAction(nameof(Index));
        }

        var createdByUserId = GetCurrentUserId();
        if (createdByUserId is null)
        {
            return Forbid();
        }

        var customer = new Customer
        {
            Name = name,
            Email = email,
            Phone = phone,
            CreatedByUserId = createdByUserId.Value,
            CreatedAt = DateTime.UtcNow
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} added customer {CustomerId}.", createdByUserId, customer.Id);

        TempData["Success"] = "Customer added.";
        TempData["NewCustomerName"] = customer.Name;
        return RedirectToAction(nameof(Index));
    }

    private int? GetCurrentUserId()
    {
        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    }
}
