using AdminPageAndDashboard.Data;
using AdminPageAndDashboard.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace AdminPageAndDashboard.Services
{
    public class ActivityLogService
    {
        private readonly AdminDbContext _context;
        private readonly ILogger<ActivityLogService> _logger;

        public ActivityLogService(AdminDbContext context, ILogger<ActivityLogService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogActivityAsync(
            int userId,
            string action,
            string? entityType = null,
            string? entityId = null,
            object? details = null,
            string? ipAddress = null)
        {
            try
            {
                var log = new ActivityLog
                {
                    UserId = userId,
                    Action = action,
                    EntityType = entityType,
                    EntityId = entityId,
                    Details = details != null ? JsonConvert.SerializeObject(details) : null,
                    IpAddress = ipAddress,
                    Timestamp = DateTime.UtcNow
                };

                _context.ActivityLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error logging activity: {ex.Message}");
            }
        }

        public async Task<List<ActivityLog>> GetActivityLogsAsync(int userId, int limit = 50)
        {
            try
            {
                return await _context.ActivityLogs
                    .Where(al => al.UserId == userId)
                    .OrderByDescending(al => al.Timestamp)
                    .Take(limit)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching activity logs: {ex.Message}");
                return new List<ActivityLog>();
            }
        }
    }
}
