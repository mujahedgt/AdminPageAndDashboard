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
            ViewBag.RefreshIntervalMs = _configuration.GetValue<int>("Dashboard:RefreshIntervalMs", 5000);
            ViewBag.MaxRequestsToDisplay = _configuration.GetValue<int>("Dashboard:MaxRequestsToDisplay", 100);
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardData()
        {
            try
            {
                var healthTask = SafeApiCall(() => _apiMiddlewareClient.GetHealthAsync());
                var statisticsTask = SafeApiCall(() => _isolationForestClient.GetStatisticsAsync());
                var recentRequestsTask = SafeApiCall(() => _apiMiddlewareClient.GetRoutingDecisionsAsync(1, 10));
                var chartDataTask = SafeApiCall(() => _apiMiddlewareClient.GetChartDataAsync());

                await Task.WhenAll(healthTask, statisticsTask, recentRequestsTask, chartDataTask);

                var health = await healthTask;
                var statistics = await statisticsTask;
                var recentRequests = await recentRequestsTask;
                var chartData = await chartDataTask;

                // Fix 1: Return actual health JSON content, not JsonElement
                var systemHealth = health != null
                    ? JsonSerializer.Deserialize<object>(health.RootElement.GetRawText())
                    : new { status = "offline", message = "Service unavailable" };

                var response = new
                {
                    systemHealth, // Now a proper object, not JsonElement
                    statistics = ExtractStats(statistics),
                    chart_data = ExtractChartData(chartData), // Already perfect (flat)
                    recentRequests = ExtractRecentRequestsData(recentRequests)
                };

                return Json(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Dashboard error: {ex.Message}");
                return Json(new
                {
                    systemHealth = new { status = "error", message = "Dashboard unavailable" },
                    statistics = GetDefaultStats(),
                    chart_data = GetDefaultChartData(),
                    recentRequests = GetDefaultRequests()
                });
            }
        }
        private object ExtractRecentRequestsData(JsonDocument? recentRequests)
        {
            if (recentRequests == null)
                return GetDefaultRequests();

            var root = recentRequests.RootElement;

            var totalPages = root.TryGetProperty("totalPages", out var tp) ? tp.GetInt32() : 1;
            var currentPage = root.TryGetProperty("page", out var cp) ? cp.GetInt32() : 1;

            var items = root.TryGetProperty("items", out var itemsEl) && itemsEl.ValueKind == JsonValueKind.Array
                ? itemsEl.EnumerateArray()
                : Enumerable.Empty<JsonElement>();

            var data = items.Select(item =>
            {
                var routing = item.GetProperty("routing");
                var request = item.GetProperty("request");

                return new
                {
                    request_id = routing.TryGetProperty("requestId", out var rid) ? rid.GetString() : "N/A",
                    timestamp = routing.TryGetProperty("decidedAt", out var da) ? da.GetDateTime().ToString("o") : null,
                    client_ip = request.TryGetProperty("clientIp", out var ip) ? ip.GetString() : "N/A",
                    endpoint = request.TryGetProperty("path", out var path) ? path.GetString() : "N/A",
                    method = request.TryGetProperty("method", out var m) ? m.GetString() : "N/A",
                    routed_to = routing.TryGetProperty("routedTo", out var rt) ? rt.GetString() : "unknown",
                    is_anomaly = routing.TryGetProperty("isAnomaly", out var ia) && ia.GetBoolean(),
                    confidence = routing.TryGetProperty("confidence", out var conf) ? conf.GetDouble() : 0.0,
                    model_version = routing.TryGetProperty("modelVersion", out var mv) ? mv.GetString() : "unknown",
                    response_status = routing.TryGetProperty("responseStatusCode", out var sc) ? sc.GetInt32() : 0,
                    response_time_ms = routing.TryGetProperty("responseTimeMs", out var rtms) ? rtms.GetInt32() : 0
                };
            }).ToArray();

            return new
            {
                data,
                totalPages,
                currentPage
            };
        }

        private object ExtractStats(JsonDocument? statsDoc)
        {
            if (statsDoc == null)
                return GetDefaultStats();

            var root = statsDoc.RootElement;

            var anomalyCount = root.TryGetProperty("anomaly_count", out var a) ? a.GetInt32() : 0;
            var legitimateCount = root.TryGetProperty("legitimate_count", out var l) ? l.GetInt32() : 0;

            return new
            {
                total_requests = root.TryGetProperty("total_requests_analyzed", out var tr) ? tr.GetInt32() : 0,
                anomaly_count = anomalyCount,
                legitimate_count = legitimateCount,
                anomaly_rate = root.TryGetProperty("anomaly_rate", out var ar) ? ar.GetDouble() : 0.0,
                routing_breakdown = new
                {
                    honeypot = new { count = anomalyCount },
                    real_system = new { count = legitimateCount }
                },
                average_confidence = root.TryGetProperty("average_confidence", out var ac) ? ac.GetDouble() : 0.0,
                uptime_hours = root.TryGetProperty("uptime_hours", out var uh) ? uh.GetDouble() : 0.0,
                timestamp = DateTime.UtcNow.ToString("o")
            };
        }

        private object ExtractChartData(JsonDocument? chartDoc)
        {
            if (chartDoc == null)
                return GetDefaultChartData();

            var root = chartDoc.RootElement;

            var routingBreakdown = new
            {
                labels = root.TryGetProperty("routingBreakdown", out var rb)
                    ? rb.GetProperty("labels").EnumerateArray().Select(x => x.GetString()!).ToArray()
                    : new[] { "Honeypot", "Real System" },
                data = rb.GetProperty("data").EnumerateArray().Select(x => x.GetInt32()).ToArray()
                    ?? new[] { 0, 0 }
            };

            var anomalyTrend = new
            {
                labels = root.TryGetProperty("anomalyTrend", out var at)
                    ? at.GetProperty("labels").EnumerateArray().Select(x => x.GetString()!).ToArray()
                    : new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" },
                data = at.GetProperty("data").EnumerateArray().Select(x => x.GetInt32()).ToArray()
                    ?? new[] { 0, 0, 0, 0, 0, 0, 0 }
            };

            var legitimateTrends = new
            {
                labels = root.TryGetProperty("legitimateTrends", out var lt)
                    ? lt.GetProperty("labels").EnumerateArray().Select(x => x.GetString()!).ToArray()
                    : new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" },
                data = lt.GetProperty("data").EnumerateArray().Select(x => x.GetInt32()).ToArray()
                    ?? new[] { 0, 0, 0, 0, 0, 0, 0 }
            };

            return new
            {
                routingBreakdown,
                anomalyTrend,
                legitimateTrends  // Correct property name
            };
        }

        private object GetDefaultChartData()
        {
            return new
            {
                routingBreakdown = new { labels = new[] { "Honeypot", "Real System" }, data = new[] { 0, 0 } },
                anomalyTrend = new { labels = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" }, data = new[] { 0, 0, 0, 0, 0, 0, 0 } },
                legitimateTrends = new { labels = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" }, data = new[] { 0, 0, 0, 0, 0, 0, 0 } }
            };
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

        private object GetDefaultStats()
        {
            return new
            {
                total_requests = 0,
                anomaly_count = 0,
                legitimate_count = 0,
                anomaly_rate = 0.0,
                routing_breakdown = new { honeypot = new { count = 0 }, real_system = new { count = 0 } },
                average_confidence = 0.0,
                uptime_hours = 0.0,
                timestamp = DateTime.UtcNow.ToString("o")
            };
        }

        private object GetDefaultRequests()
        {
            return new { data = Array.Empty<object>(), totalPages = 1, currentPage = 1 };
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