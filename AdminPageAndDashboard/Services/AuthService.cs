using AdminPageAndDashboard.Data;
using AdminPageAndDashboard.Models;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;

namespace AdminPageAndDashboard.Services
{
    public class AuthService
    {
        private readonly AdminDbContext _context;
        private readonly ILogger<AuthService> _logger;

        public AuthService(AdminDbContext context, ILogger<AuthService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                    return null;

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

                if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                    return null;

                if (!user.IsActive)
                    return null;

                // Update last login
                user.LastLogin = DateTime.UtcNow;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Authentication error: {ex.Message}");
                return null;
            }
        }

        public async Task<List<string>> GetUserRolesAsync(int userId)
        {
            try
            {
                var roles = await _context.UserRoles
                    .Where(ur => ur.UserId == userId)
                    .Include(ur => ur.Role)
                    .Select(ur => ur.Role!.RoleName)
                    .ToListAsync();

                return roles;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching user roles: {ex.Message}");
                return new List<string>();
            }
        }

        public async Task<User> CreateUserAsync(string username, string email, string password, string fullName, List<int> roleIds)
        {
            try
            {
                // Validate that all roles exist
                var validRoles = await _context.Roles
                    .Where(r => roleIds.Contains(r.Id))
                    .ToListAsync();

                if (validRoles.Count != roleIds.Count)
                {
                    var invalidRoleIds = roleIds.Where(id => !validRoles.Any(r => r.Id == id)).ToList();
                    throw new InvalidOperationException($"Invalid role IDs: {string.Join(", ", invalidRoleIds)}. Valid roles are: {string.Join(", ", validRoles.Select(r => $"{r.RoleName}({r.Id})"))}");
                }

                var user = new User
                {
                    Username = username,
                    Email = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                    FullName = fullName,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Assign roles using the validated role objects
                foreach (var roleId in roleIds)
                {
                    var userRole = new UserRole
                    {
                        UserId = user.Id,
                        RoleId = roleId,
                        AssignedAt = DateTime.UtcNow
                    };
                    _context.UserRoles.Add(userRole);
                }

                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"User '{username}' created successfully with {roleIds.Count} role(s)");
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating user: {ex.Message}");
                throw;
            }
        }
    }
}
