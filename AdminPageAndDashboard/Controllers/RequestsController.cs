using AdminPageAndDashboard.Services.ApiClients;
using AdminPageAndDashboard.Filters;
using Microsoft.AspNetCore.Mvc;

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
                
                // Extract data safely
                var totalPages = root.TryGetProperty("totalPages", out var totalPagesEl) 
                    ? totalPagesEl.GetInt32() 
                    : 1;

                var requestsData = root.TryGetProperty("data", out var dataEl) 
                    ? dataEl.EnumerateArray().ToList() 
                    : new List<System.Text.Json.JsonElement>();

                _logger.LogInformation($"Retrieved {requestsData.Count} requests from API");

                var response = new
                {
                    data = requestsData.Select(r => new
                    {
                        request_id = r.GetProperty("request_id").GetString(),
                        timestamp = r.GetProperty("timestamp").GetString(),
                        client_ip = r.TryGetProperty("client_ip", out var ip) ? ip.GetString() : "N/A",
                        endpoint = r.TryGetProperty("endpoint", out var ep) ? ep.GetString() : "N/A",
                        method = r.TryGetProperty("method", out var m) ? m.GetString() : "N/A",
                        routed_to = r.TryGetProperty("routed_to", out var rt) ? rt.GetString() : "unknown",
                        is_anomaly = r.TryGetProperty("is_anomaly", out var ia) ? ia.GetBoolean() : false,
                        confidence = r.TryGetProperty("confidence", out var conf) ? conf.GetDouble() : 0.0
                    }).ToList(),
                    totalPages = totalPages,
                    currentPage = page
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
