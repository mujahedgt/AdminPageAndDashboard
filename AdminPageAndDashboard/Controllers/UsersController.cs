using AdminPageAndDashboard.Services;
using AdminPageAndDashboard.Filters;
using AdminPageAndDashboard.Data;
using AdminPageAndDashboard.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdminPageAndDashboard.Controllers
{
    [AuthorizeRole("Admin")]
    public class UsersController : Controller
    {
        private readonly AuthService _authService;
        private readonly ActivityLogService _activityLogService;
        private readonly AdminDbContext _context;

        public UsersController(
            AuthService authService,
            ActivityLogService activityLogService,
            AdminDbContext context)
        {
            _authService = authService;
            _activityLogService = activityLogService;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var users = await _context.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .OrderBy(u => u.Username)
                    .ToListAsync();

                // Fetch roles for the modal
                var roles = await _context.Roles
                    .OrderBy(r => r.Id)
                    .ToListAsync();

                // Pass roles to view using ViewBag
                ViewBag.Roles = roles;

                return View(users);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to load users: {ex.Message}";
                ViewBag.Roles = new List<Role>();
                return View(new List<User>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string username, string email, string fullName, string password, int[] roleIds)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(username) || username.Length < 3 || username.Length > 50)
                {
                    TempData["Error"] = "Username must be between 3 and 50 characters";
                    return RedirectToAction("Index");
                }

                if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
                {
                    TempData["Error"] = "Valid email is required";
                    return RedirectToAction("Index");
                }

                if (string.IsNullOrWhiteSpace(fullName) || fullName.Length < 3)
                {
                    TempData["Error"] = "Full name must be at least 3 characters";
                    return RedirectToAction("Index");
                }

                if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                {
                    TempData["Error"] = "Password must be at least 6 characters";
                    return RedirectToAction("Index");
                }

                if (roleIds == null || roleIds.Length == 0)
                {
                    TempData["Error"] = "At least one role must be selected";
                    return RedirectToAction("Index");
                }

                // Check if username already exists
                if (await _context.Users.AnyAsync(u => u.Username == username))
                {
                    TempData["Error"] = "Username already exists";
                    return RedirectToAction("Index");
                }

                // Check if email already exists
                if (await _context.Users.AnyAsync(u => u.Email == email))
                {
                    TempData["Error"] = "Email already exists";
                    return RedirectToAction("Index");
                }

                var newUser = await _authService.CreateUserAsync(username, email, password, fullName, roleIds.ToList());

                var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                await _activityLogService.LogActivityAsync(
                    userId,
                    "CREATE_USER",
                    "User",
                    newUser.Id.ToString(),
                    new { username, email, roles = roleIds }.ToString()
                );

                TempData["Success"] = $"User '{username}' created successfully";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to create user: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserRoles)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    TempData["Error"] = "User not found";
                    return RedirectToAction("Index");
                }

                // Don't allow deleting own account
                var currentUserId = HttpContext.Session.GetInt32("UserId");
                if (currentUserId == id)
                {
                    TempData["Error"] = "Cannot delete your own account";
                    return RedirectToAction("Index");
                }

                // Remove user roles first (cascade delete should handle this, but being explicit)
                _context.UserRoles.RemoveRange(user.UserRoles);
                
                // Remove user
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                await _activityLogService.LogActivityAsync(
                    currentUserId ?? 0,
                    "DELETE_USER",
                    "User",
                    id.ToString(),
                    new { username = user.Username }.ToString()
                );

                TempData["Success"] = $"User '{user.Username}' deleted successfully";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to delete user: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string email, string fullName, bool isActive, int[] roleIds)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserRoles)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    TempData["Error"] = "User not found";
                    return RedirectToAction("Index");
                }

                // Validate input
                if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
                {
                    TempData["Error"] = "Valid email is required";
                    return RedirectToAction("Index");
                }

                if (string.IsNullOrWhiteSpace(fullName) || fullName.Length < 3)
                {
                    TempData["Error"] = "Full name must be at least 3 characters";
                    return RedirectToAction("Index");
                }

                if (roleIds == null || roleIds.Length == 0)
                {
                    TempData["Error"] = "At least one role must be selected";
                    return RedirectToAction("Index");
                }

                // Check if email already exists for another user
                if (email != user.Email && await _context.Users.AnyAsync(u => u.Email == email))
                {
                    TempData["Error"] = "Email already exists";
                    return RedirectToAction("Index");
                }

                user.Email = email;
                user.FullName = fullName;
                user.IsActive = isActive;
                user.UpdatedAt = DateTime.UtcNow;

                // Update roles
                _context.UserRoles.RemoveRange(user.UserRoles);

                foreach (var roleId in roleIds)
                {
                    _context.UserRoles.Add(new UserRole
                    {
                        UserId = user.Id,
                        RoleId = roleId,
                        AssignedAt = DateTime.UtcNow
                    });
                }

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                await _activityLogService.LogActivityAsync(
                    userId,
                    "EDIT_USER",
                    "User",
                    user.Id.ToString(),
                    new { email, fullName, isActive, roles = roleIds }.ToString()
                );

                TempData["Success"] = $"User '{user.Username}' updated successfully";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to update user: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUserRoles(int userId)
        {
            try
            {
                var roleIds = await _context.UserRoles
                    .Where(ur => ur.UserId == userId)
                    .Select(ur => ur.RoleId)
                    .ToListAsync();

                return Json(new { roleIds });
            }
            catch
            {
                return Json(new { roleIds = new List<int>() });
            }
        }
    }
}
