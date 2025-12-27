using AdminPageAndDashboard.Data;
using Microsoft.EntityFrameworkCore;

namespace AdminPageAndDashboard.Services
{
    public class SettingsService
    {
        private readonly AdminDbContext _context;
        private readonly ILogger<SettingsService> _logger;

        public SettingsService(AdminDbContext context, ILogger<SettingsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<string?> GetSettingAsync(string key)
        {
            try
            {
                var setting = await _context.SystemSettings
                    .FirstOrDefaultAsync(s => s.SettingKey == key);

                return setting?.SettingValue;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting setting {key}: {ex.Message}");
                return null;
            }
        }

        public async Task<T?> GetSettingAsync<T>(string key, T? defaultValue = default)
        {
            try
            {
                var value = await GetSettingAsync(key);
                if (value == null) return defaultValue;

                return (T?)Convert.ChangeType(value, typeof(T));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error converting setting {key}: {ex.Message}");
                return defaultValue;
            }
        }

        public async Task SetSettingAsync(string key, string value)
        {
            try
            {
                var setting = await _context.SystemSettings
                    .FirstOrDefaultAsync(s => s.SettingKey == key);

                if (setting == null)
                {
                    setting = new Models.SystemSetting { SettingKey = key };
                    _context.SystemSettings.Add(setting);
                }

                setting.SettingValue = value;
                setting.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error setting {key}: {ex.Message}");
                throw;
            }
        }
    }
}
