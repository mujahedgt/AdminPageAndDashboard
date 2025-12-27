using AdminPageAndDashboard.Models;
using BCrypt.Net;

namespace AdminPageAndDashboard.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(AdminDbContext context)
        {
            try
            {
                // Create database if it doesn't exist
                await context.Database.EnsureCreatedAsync();

                // Seed roles if they don't exist
                if (!context.Roles.Any())
                {
                    context.Roles.AddRange(
                        new Role { RoleName = "Admin", Description = "Full system access" },
                        new Role { RoleName = "Operator", Description = "Can manage requests and view data" },
                        new Role { RoleName = "Viewer", Description = "Read-only access to dashboard" }
                    );
                    await context.SaveChangesAsync();
                }

                // Seed default admin user if it doesn't exist
                if (!context.Users.Any(u => u.Username == "admin"))
                {
                    var adminUser = new User
                    {
                        Username = "admin",
                        Email = "admin@localhost",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                        FullName = "System Administrator",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    context.Users.Add(adminUser);
                    await context.SaveChangesAsync();

                    // Assign Admin role to admin user
                    var adminRole = context.Roles.FirstOrDefault(r => r.RoleName == "Admin");
                    if (adminRole != null)
                    {
                        context.UserRoles.Add(new UserRole
                        {
                            UserId = adminUser.Id,
                            RoleId = adminRole.Id,
                            AssignedAt = DateTime.UtcNow
                        });
                        await context.SaveChangesAsync();
                    }
                }

                // Note: SystemSettings are now seeded by the migration (admin5_fix_system_settings)
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error seeding database: {ex.Message}");
                throw;
            }
        }
    }
}
