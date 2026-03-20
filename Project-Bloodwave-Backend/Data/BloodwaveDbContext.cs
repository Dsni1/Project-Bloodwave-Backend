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
    public DbSet<Match> Matches { get; set; }
    public DbSet<Item> Items { get; set; }
    public DbSet<MatchItem> MatchItems { get; set; }
    public DbSet<Weapon> Weapons { get; set; }
    public DbSet<MatchWeapon> MatchWeapons { get; set; }
    public DbSet<Achievment> Achievments { get; set; }
    public DbSet<UserAchievment> UserAchievments { get; set; }

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

        modelBuilder.Entity<User>()
            .HasMany(u => u.UserAchievments)
            .WithOne(ua => ua.User)
            .HasForeignKey(ua => ua.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // RefreshToken configuration
        modelBuilder.Entity<RefreshToken>()
            .HasKey(rt => rt.Id);

        // Achievment configuration
        modelBuilder.Entity<Achievment>()
            .HasKey(a => a.Id);

        // UserAchievment configuration (M:N User-Achievment)
        modelBuilder.Entity<UserAchievment>()
            .HasKey(ua => ua.Id);

        modelBuilder.Entity<UserAchievment>()
            .HasIndex(ua => new { ua.UserId, ua.AchievmentId })
            .IsUnique();

        modelBuilder.Entity<UserAchievment>()
            .HasOne(ua => ua.Achievment)
            .WithMany(a => a.UserAchievments)
            .HasForeignKey(ua => ua.AchievmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Match configuration
        modelBuilder.Entity<Match>()
            .HasOne(m => m.User)
            .WithMany(u => u.Matches)
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // MatchItem configuration (M:N kapcsolat)
        modelBuilder.Entity<MatchItem>()
            .HasOne(mi => mi.Match)
            .WithMany(m => m.MatchItems)
            .HasForeignKey(mi => mi.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MatchItem>()
            .HasOne(mi => mi.Item)
            .WithMany(i => i.MatchItems)
            .HasForeignKey(mi => mi.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // MatchWeapon configuration (M:N kapcsolat)
        modelBuilder.Entity<MatchWeapon>()
            .HasOne(mw => mw.Match)
            .WithMany(m => m.MatchWeapons)
            .HasForeignKey(mw => mw.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MatchWeapon>()
            .HasOne(mw => mw.Weapon)
            .WithMany(w => w.MatchWeapons)
            .HasForeignKey(mw => mw.WeaponId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}