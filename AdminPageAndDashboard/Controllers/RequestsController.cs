using AdminPageAndDashboard.Filters;
using AdminPageAndDashboard.Services.ApiClients;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AdminPageAndDashboard.Controllers
{
    [AuthorizeRole("Admin", "Operator", "Viewer")]
    public class RequestsController : Controller
    {
        private readonly ApiMiddlewareClient _apiMiddlewareClient;
        private readonly HoneypotClient _honeypotClient;
        private readonly ILogger<RequestsController> _logger;

        public RequestsController(
            ApiMiddlewareClient apiMiddlewareClient,
            HoneypotClient honeypotClient,
            ILogger<RequestsController> logger)
        {
            _apiMiddlewareClient = apiMiddlewareClient;
            _honeypotClient = honeypotClient;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetRequests(int page = 1, int pageSize = 20, string? clientIp = null)
        {
            try
            {
                // Validate parameters
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                _logger.LogInformation($"GetRequests called with page={page}, pageSize={pageSize}, clientIp={clientIp}");

                var filters = string.Empty;
                if (!string.IsNullOrWhiteSpace(clientIp))
                {
                    filters = $"&clientIp={Uri.EscapeDataString(clientIp)}";
                }

                // Fetch data from API
                var data = await _apiMiddlewareClient.GetRoutingDecisionsAsync(page, pageSize, filters);

                if (data == null)
                {
                    _logger.LogWarning("API returned null for routing decisions");
                    return Json(new 
                    { 
                        error = "Backend service is unavailable. Please ensure the API Middleware service is running.",
                        data = new List<object>(),
                        totalPages = 0,
                        currentPage = page
                    });
                }

                var root = data.RootElement;

                // Pagination
                var totalPages = root.TryGetProperty("totalPages", out var tp)
                    ? tp.GetInt32()
                    : 1;

                var currentPage = root.TryGetProperty("page", out var cp)
                    ? cp.GetInt32()
                    : 1;

                // Items array
                var items = root.TryGetProperty("items", out var itemsEl) && itemsEl.ValueKind == JsonValueKind.Array
                    ? itemsEl.EnumerateArray()
                    : Enumerable.Empty<JsonElement>();

                _logger.LogInformation($"Retrieved {items.Count()} requests from API");

                var response = new
                {
                    data = items.Select(item =>
                    {
                        // Nested objects
                        var routing = item.GetProperty("routing");
                        var request = item.GetProperty("request");

                        return new
                        {
                            request_id = routing.TryGetProperty("requestId", out var rid)
                                ? rid.GetString()
                                : "N/A",

                            timestamp = routing.TryGetProperty("decidedAt", out var da)
                                ? da.GetString()
                                : null,

                            client_ip = request.TryGetProperty("clientIp", out var ip)
                                ? ip.GetString()
                                : "N/A",

                            endpoint = request.TryGetProperty("path", out var path)
                                ? path.GetString()
                                : "N/A",

                            method = request.TryGetProperty("method", out var m)
                                ? m.GetString()
                                : "N/A",

                            routed_to = routing.TryGetProperty("routedTo", out var rt)
                                ? rt.GetString()
                                : "unknown",

                            is_anomaly = routing.TryGetProperty("isAnomaly", out var ia)
                                ? ia.GetBoolean()
                                : false,

                            confidence = routing.TryGetProperty("confidence", out var conf)
                                ? conf.GetDouble()
                                : 0.0,

                            model_version = routing.TryGetProperty("modelVersion", out var mv)
                                ? mv.GetString()
                                : "unknown",

                            response_status = routing.TryGetProperty("responseStatusCode", out var sc)
                                ? sc.GetInt32()
                                : 0,

                            response_time_ms = routing.TryGetProperty("responseTimeMs", out var rtms)
                                ? rtms.GetInt32()
                                : 0
                        };
                    }).ToList(),

                    totalPages = totalPages,
                    currentPage = currentPage
                };

                return Json(response);

            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"HTTP error fetching requests: {ex.Message}");
                return Json(new 
                { 
                    error = $"Service unavailable: {ex.Message}",
                    data = new List<object>(),
                    totalPages = 0,
                    currentPage = page
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error fetching requests: {ex.Message}");
                return Json(new 
                { 
                    error = $"An error occurred: {ex.Message}",
                    data = new List<object>(),
                    totalPages = 0,
                    currentPage = page
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    return BadRequest(new { error = "Request ID is required" });

                var data = await _apiMiddlewareClient.GetRoutingDecisionDetailsAsync(id);

                if (data == null)
                    return NotFound(new { error = "Request not found or service unavailable" });

                return Json(data.RootElement);
            }
            catch (HttpRequestException)
            {
                return StatusCode(503, new { error = "Service unavailable" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching request details: {ex.Message}");
                return StatusCode(500, new { error = "An error occurred", details = ex.Message });
            }
        }
    }
}
