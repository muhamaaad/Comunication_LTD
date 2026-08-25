using Microsoft.EntityFrameworkCore;
using SecurityWebApp.Models;

namespace SecurityWebApp.Data;

// The tables themselves are defined in Database/schema.sql. Every property here
// maps to a column by name, so there is nothing to configure.
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
    public DbSet<PasswordHistory> PasswordHistories { get; set; }
}
