using Microsoft.EntityFrameworkCore;
using Project_Bloodwave_Backend.Models;

namespace Project_Bloodwave_Backend.Data;

public class BloodwaveDbContext : DbContext
{
    public BloodwaveDbContext(DbContextOptions<BloodwaveDbContext> options) 
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>()
            .HasKey(u => u.Id);
        
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();
        
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasMany(u => u.RefreshTokens)
            .WithOne(rt => rt.User)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // RefreshToken configuration
        modelBuilder.Entity<RefreshToken>()
            .HasKey(rt => rt.Id);
    }
}
