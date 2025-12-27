using AdminPageAndDashboard.Models.ViewModels;
using AdminPageAndDashboard.Services;
using Microsoft.AspNetCore.Mvc;

namespace AdminPageAndDashboard.Controllers
{
    public class AuthController : Controller
    {
        private readonly AuthService _authService;
        private readonly ActivityLogService _activityLogService;

        public AuthController(AuthService authService, ActivityLogService activityLogService)
        {
            _authService = authService;
            _activityLogService = activityLogService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            // If already logged in, redirect to dashboard
            if (HttpContext.Session.GetInt32("UserId").HasValue)
                return RedirectToAction("Index", "Dashboard");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _authService.AuthenticateAsync(model.Username, model.Password);

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid username or password");
                return View(model);
            }

            // Get user roles
            var roles = await _authService.GetUserRolesAsync(user.Id);

            // DEBUG: Check if roles are loaded
            System.Diagnostics.Debug.WriteLine($"User: {user.Username}, Roles count: {roles.Count}");
            foreach (var role in roles)
            {
                System.Diagnostics.Debug.WriteLine($"Role: {role}");
            }

            // Set session
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("FullName", user.FullName);
            HttpContext.Session.SetString("Roles", string.Join(",", roles));

            // DEBUG: Verify session was set
            var rolesFromSession = HttpContext.Session.GetString("Roles");
            System.Diagnostics.Debug.WriteLine($"Roles in session: {rolesFromSession}");

            // Log activity
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _activityLogService.LogActivityAsync(user.Id, "LOGIN", ipAddress: ipAddress);

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId.HasValue)
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                await _activityLogService.LogActivityAsync(userId.Value, "LOGOUT", ipAddress: ipAddress);
            }

            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
