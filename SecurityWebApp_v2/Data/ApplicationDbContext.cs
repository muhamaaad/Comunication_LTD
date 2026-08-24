using Microsoft.EntityFrameworkCore;
using SecurityWebApp.Models;

namespace SecurityWebApp.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
    public DbSet<PasswordHistory> PasswordHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            // Username is what people log in with, so the database enforces that
            // it is unique. NOCASE means "Admin" and "admin" are the same name.
            entity.Property(u => u.Username)
                  .IsRequired()
                  .HasMaxLength(50)
                  .UseCollation("NOCASE");

            entity.HasIndex(u => u.Username).IsUnique();

            entity.Property(u => u.Email)
                  .IsRequired()
                  .HasMaxLength(255)
                  .UseCollation("NOCASE");

            entity.HasIndex(u => u.Email).IsUnique();

            entity.Property(u => u.PasswordHash)
                  .IsRequired()
                  .HasMaxLength(255);

            // Stored as "Regular" / "Admin" so the column reads plainly in SQL.
            entity.Property(u => u.Role)
                  .IsRequired()
                  .HasMaxLength(20)
                  .HasConversion<string>();
        });

        modelBuilder.Entity<PasswordHistory>(entity =>
        {
            entity.Property(h => h.PasswordHash)
                  .IsRequired()
                  .HasMaxLength(255);

            entity.HasOne<User>()
                  .WithMany()
                  .HasForeignKey(h => h.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.Property(t => t.ResetToken)
                  .IsRequired()
                  .HasMaxLength(40);

            entity.HasIndex(t => t.ResetToken).IsUnique();

            entity.HasOne<User>()
                  .WithMany()
                  .HasForeignKey(t => t.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
