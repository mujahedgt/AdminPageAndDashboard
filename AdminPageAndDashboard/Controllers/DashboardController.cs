using Microsoft.AspNetCore.Mvc;
using AdminPageAndDashboard.Services.ApiClients;
using AdminPageAndDashboard.Filters;
using AdminPageAndDashboard.Services;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace AdminPageAndDashboard.Controllers
{
    [AuthorizeRole("Admin", "Operator", "Viewer")]
    public class DashboardController : Controller
    {
        private readonly ApiMiddlewareClient _apiMiddlewareClient;
        private readonly IsolationForestClient _isolationForestClient;
        private readonly HoneypotClient _honeypotClient;
        private readonly ActivityLogService _activityLogService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            ApiMiddlewareClient apiMiddlewareClient,
            IsolationForestClient isolationForestClient,
            HoneypotClient honeypotClient,
            ActivityLogService activityLogService,
            IConfiguration configuration,
            ILogger<DashboardController> logger)
        {
            _apiMiddlewareClient = apiMiddlewareClient;
            _isolationForestClient = isolationForestClient;
            _honeypotClient = honeypotClient;
            _activityLogService = activityLogService;
            _configuration = configuration;
            _logger = logger;
        }

        public IActionResult Index()
        {
            // Pass dashboard configuration to view
            ViewBag.RefreshIntervalMs = _configuration.GetValue<int>("Dashboard:RefreshIntervalMs", 5000);
            ViewBag.MaxRequestsToDisplay = _configuration.GetValue<int>("Dashboard:MaxRequestsToDisplay", 100);
            
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardData()
        {
            try
            {
                var health = await SafeApiCall(() => _apiMiddlewareClient.GetHealthAsync());
                var recentRequests = await SafeApiCall(() => _apiMiddlewareClient.GetRoutingDecisionsAsync(1, 10));

                var response = new
                {
                    systemHealth = health?.RootElement ?? GetDefaultHealth(),
                    statistics = GetDefaultStats(),
                    recentRequests = ExtractRecentRequestsData(recentRequests)
                };

                return Json(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Dashboard error: {ex.Message}");
                return Json(new
                {
                    systemHealth = GetDefaultHealth(),
                    statistics = GetDefaultStats(),
                    recentRequests = GetDefaultRequests()
                });
            }
        }

        private object ExtractRecentRequestsData(JsonDocument? recentRequests)
        {
            if (recentRequests == null)
                return GetDefaultRequests();

            if (recentRequests.RootElement.TryGetProperty("data", out var data))
                return data;

            return GetDefaultRequests();
        }

        private async Task<JsonDocument?> SafeApiCall(Func<Task<JsonDocument?>> apiCall)
        {
            try
            {
                return await apiCall();
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"API call failed: {ex.Message}");
                return null;
            }
        }

        private object GetDefaultHealth()
        {
            return new { status = "offline", message = "Services unavailable" };
        }

        private object GetDefaultStats()
        {
            return new
            {
                total_requests = 0,
                anomalies_detected = 0,
                anomaly_rate = 0.0,
                timestamp = DateTime.UtcNow
            };
        }

        private object GetDefaultRequests()
        {
            return new { };
        }

        [HttpGet]
        public async Task<IActionResult> GetChartData()
        {
            try
            {
                var response = new
                {
                    routingBreakdown = new
                    {
                        labels = new[] { "Honeypot", "Real System" },
                        data = new[] { 0, 0 }
                    },
                    anomalyTrend = new
                    {
                        labels = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" },
                        data = new[] { 0, 0, 0, 0, 0, 0, 0 }
                    }
                };

                return Json(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Chart data error: {ex.Message}");
                return Json(new
                {
                    routingBreakdown = new { labels = new[] { "Honeypot", "Real System" }, data = new[] { 0, 0 } },
                    anomalyTrend = new { labels = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" }, data = new[] { 0, 0, 0, 0, 0, 0, 0 } }
                });
            }
        }

        [HttpGet]
        public IActionResult SetTheme(string? theme)
        {
            if (string.IsNullOrWhiteSpace(theme) || (theme != "dark" && theme != "light"))
                return BadRequest(new { error = "Invalid theme value" });

            HttpContext.Session.SetString("Theme", theme);
            return Ok();
        }
    }
}
