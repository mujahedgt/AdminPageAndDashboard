using AdminPageAndDashboard.Services;
using AdminPageAndDashboard.Services.ApiClients;
using AdminPageAndDashboard.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace AdminPageAndDashboard.Controllers
{
    [AuthorizeRole("Admin", "Operator")]
    public class ModelsController : Controller
    {
        private readonly IsolationForestClient _isolationForestClient;
        private readonly ActivityLogService _activityLogService;

        public ModelsController(
            IsolationForestClient isolationForestClient,
            ActivityLogService activityLogService)
        {
            _isolationForestClient = isolationForestClient;
            _activityLogService = activityLogService;
        }

        [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetModelStatistics()
        {
            try
            {
                var data = await _isolationForestClient.GetStatisticsAsync();

                if (data == null)
                    return StatusCode(500, new { error = "No data from service" });

                var root = data.RootElement;
                var response = new
                {
                    current_version = root.TryGetProperty("current_version", out var cv) ? cv.GetString() : "v1.0",
                    accuracy = root.TryGetProperty("accuracy", out var acc) ? acc.GetDouble() : 0.85,
                    last_trained = root.TryGetProperty("last_trained", out var lt) ? lt.GetString() : DateTime.UtcNow.ToString("O"),
                    total_samples = root.TryGetProperty("total_samples", out var ts) ? ts.GetInt32() : 0,
                    anomalies_detected = root.TryGetProperty("anomalies_detected", out var ad) ? ad.GetInt32() : 0
                };

                return Json(response);
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(503, new { error = "ML Service unavailable", details = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred", details = ex.Message });
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetRequests(int page = 1, int pageSize = 20)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var data = await _isolationForestClient.GetAuditAsync(page, pageSize);

                if (data == null)
                    return StatusCode(500, new { error = "No data from service" });

                var root = data.RootElement;
                var requestsData = root.TryGetProperty("data", out var dataEl)
                    ? dataEl.EnumerateArray().ToList()
                    : new List<System.Text.Json.JsonElement>();

                var response = new
                {
                    data = requestsData.Select(r => new
                    {
                        request_id = r.TryGetProperty("request_id", out var rid) ? rid.GetString() : "N/A",
                        client_ip = r.TryGetProperty("client_ip", out var ip) ? ip.GetString() : "N/A",
                        prediction = r.TryGetProperty("prediction", out var pred) ? pred.GetBoolean() : false,
                        confidence = r.TryGetProperty("confidence", out var conf) ? conf.GetDouble() : 0.0,
                        user_label = r.TryGetProperty("user_label", out var ul) ? ul.GetBoolean() : false
                    }).ToList(),
                    page = page,
                    pageSize = pageSize
                };

                return Json(response);
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(503, new { error = "ML Service unavailable", details = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred", details = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TrainModel(string? modelVersion, bool useCorrectedLabels = true)
        {
            var userIdValue = HttpContext.Session.GetInt32("UserId");

            if (!userIdValue.HasValue || string.IsNullOrWhiteSpace(modelVersion))
                return BadRequest(new { error = "Session expired or invalid model version" });

            try
            {
                var userId = userIdValue.Value;
                var result = await _isolationForestClient.TrainModelAsync(modelVersion, useCorrectedLabels);

                await _activityLogService.LogActivityAsync(
                    userId,
                    "TRAIN_MODEL",
                    "MLModel",
                    modelVersion,
                    new { useCorrectedLabels }.ToString()
                );

                return Json(result.RootElement);
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(503, new { error = "ML Service unavailable", details = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(500, new { error = "Failed to train model", details = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An unexpected error occurred", details = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RetrainModel(string? modelVersion)
        {
            var userIdValue = HttpContext.Session.GetInt32("UserId");

            if (!userIdValue.HasValue || string.IsNullOrWhiteSpace(modelVersion))
                return BadRequest(new { error = "Session expired or invalid model version" });

            try
            {
                var userId = userIdValue.Value;
                var result = await _isolationForestClient.RetrainModelAsync(modelVersion);

                await _activityLogService.LogActivityAsync(
                    userId,
                    "RETRAIN_MODEL",
                    "MLModel",
                    modelVersion
                );

                return Json(result.RootElement);
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(503, new { error = "ML Service unavailable", details = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(500, new { error = "Failed to retrain model", details = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An unexpected error occurred", details = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeLabel(int requestId, bool label)
        {
            var userIdValue = HttpContext.Session.GetInt32("UserId");

            if (!userIdValue.HasValue)
                return Unauthorized(new { error = "Session expired" });

            try
            {
                var userId = userIdValue.Value;
                var username = HttpContext.Session.GetString("Username") ?? "System";

                var result = await _isolationForestClient.UpdateLabelAsync(requestId, label, username);

                await _activityLogService.LogActivityAsync(
                    userId,
                    "CHANGE_LABEL",
                    "Audit",
                    requestId.ToString(),
                    new { label }.ToString()
                );

                return Json(result.RootElement);
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(503, new { error = "ML Service unavailable", details = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An unexpected error occurred", details = ex.Message });
            }
        }
    }
}
