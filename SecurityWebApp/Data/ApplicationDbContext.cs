using Microsoft.EntityFrameworkCore;
using SecurityWebApp.Models;

namespace SecurityWebApp.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<User> Users { get; set; }
}