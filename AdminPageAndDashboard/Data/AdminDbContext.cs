using AdminPageAndDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace AdminPageAndDashboard.Data
{
    public class AdminDbContext : DbContext
    {
        public AdminDbContext(DbContextOptions<AdminDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<UserRole> UserRoles { get; set; } = null!;
        public DbSet<ActivityLog> ActivityLogs { get; set; } = null!;
        public DbSet<SystemSetting> SystemSettings { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // UserRole relationships
            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            // ActivityLog relationships
            modelBuilder.Entity<ActivityLog>()
                .HasOne(al => al.User)
                .WithMany(u => u.ActivityLogs)
                .HasForeignKey(al => al.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // SystemSetting configuration
            modelBuilder.Entity<SystemSetting>()
                .ToTable("system_settings")
                .HasKey(s => s.Id);

            modelBuilder.Entity<SystemSetting>()
                .Property(s => s.SettingKey)
                .HasColumnName("setting_key")
                .IsRequired()
                .HasMaxLength(100);

            modelBuilder.Entity<SystemSetting>()
                .Property(s => s.SettingValue)
                .HasColumnName("setting_value")
                .IsRequired();

            modelBuilder.Entity<SystemSetting>()
                .Property(s => s.Description)
                .HasColumnName("description")
                .HasMaxLength(255);

            modelBuilder.Entity<SystemSetting>()
                .Property(s => s.UpdatedAt)
                .HasColumnName("updated_at");

            // Unique index on setting_key
            modelBuilder.Entity<SystemSetting>()
                .HasIndex(s => s.SettingKey)
                .IsUnique();

            // Indices
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
        }
    }
}