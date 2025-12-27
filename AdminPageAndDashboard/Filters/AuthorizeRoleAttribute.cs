using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AdminPageAndDashboard.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthorizeRoleAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _roles;

        public AuthorizeRoleAttribute(params string[] roles)
        {
            _roles = roles;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var userId = context.HttpContext.Session.GetInt32("UserId");
            var userRoles = context.HttpContext.Session.GetString("Roles");

            // Check if user is logged in
            if (!userId.HasValue)
            {
                context.Result = new RedirectToActionResult("Login", "Auth", null);
                return;
            }

            // If no specific roles required, user is authorized
            if (_roles.Length == 0)
                return;

            // Check if user has required role
            if (string.IsNullOrEmpty(userRoles))
            {
                // User has no roles assigned - deny access
                context.Result = new ViewResult
                {
                    ViewName = "~/Views/Shared/AccessDenied.cshtml"
                };
                return;
            }

            // Split roles and check for matches (case-insensitive)
            var userRoleList = userRoles.Split(',', StringSplitOptions.RemoveEmptyEntries);
            
            // Debug: Log the roles for troubleshooting
            System.Diagnostics.Debug.WriteLine($"User Roles: {string.Join(", ", userRoleList)}");
            System.Diagnostics.Debug.WriteLine($"Required Roles: {string.Join(", ", _roles)}");

            // Check if user has ANY of the required roles
            bool hasRequiredRole = false;
            foreach (var userRole in userRoleList)
            {
                foreach (var requiredRole in _roles)
                {
                    if (userRole.Trim().Equals(requiredRole, StringComparison.OrdinalIgnoreCase))
                    {
                        hasRequiredRole = true;
                        break;
                    }
                }
                if (hasRequiredRole) break;
            }

            if (!hasRequiredRole)
            {
                context.Result = new ViewResult
                {
                    ViewName = "~/Views/Shared/AccessDenied.cshtml"
                };
            }
        }
    }
}
