using AdminPageAndDashboard.Filters;
using AdminPageAndDashboard.Services;
using AdminPageAndDashboard.Services.ApiClients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;

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

                JsonElement activeModel;
                bool hasActiveModel = root.TryGetProperty("active_model", out activeModel);

                var response = new
                {
                    current_version = hasActiveModel && activeModel.TryGetProperty("version", out var cv)
                        ? cv.GetString()
                        : "v1.0",

                    accuracy = hasActiveModel && activeModel.TryGetProperty("accuracy_score", out var acc) && acc.ValueKind != JsonValueKind.Null
                        ? acc.GetDouble()
                        : 0.85,

                    last_trained = hasActiveModel && activeModel.TryGetProperty("training_date", out var lt)
                        ? lt.GetString()
                        : DateTime.UtcNow.ToString("O"),

                    total_samples = hasActiveModel && activeModel.TryGetProperty("training_samples", out var ts)
                        ? ts.GetInt32()
                        : 0,

                    anomalies_detected = root.TryGetProperty("anomaly_count", out var ad)
                        ? ad.GetInt32()
                        : 0
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

                // Get data array
                var requestsData = root.TryGetProperty("data", out var dataEl) && dataEl.ValueKind == JsonValueKind.Array
                    ? dataEl.EnumerateArray().ToList()
                    : new List<JsonElement>();

                var response = new
                {
                    data = requestsData.Select(r => new
                    {
                        record_id = r.TryGetProperty("id", out var rcid)
                            ? rcid.GetInt32()
                            : 0,
                        request_id = r.TryGetProperty("request_id", out var rid)
                            ? rid.GetString()
                            : "N/A",

                        client_ip = r.TryGetProperty("ip_address", out var ip)
                            ? ip.GetString()
                            : "N/A",

                        prediction = r.TryGetProperty("is_anomaly", out var pred)
                            ? pred.GetBoolean()
                            : false,

                        confidence = r.TryGetProperty("confidence", out var conf)
                            ? conf.GetDouble()
                            : 0.0,

                        user_label = r.TryGetProperty("user_label", out var ul) && ul.ValueKind != JsonValueKind.Null
                            ? ul.GetBoolean()
                            : (bool?)null,

                        model_version = r.TryGetProperty("model_version", out var mv)
                            ? mv.GetString()
                            : "unknown",

                        analyzed_at = r.TryGetProperty("analyzed_at", out var at)
                            ? at.GetString()
                            : null
                    }).ToList(),

                    page = root.TryGetProperty("page", out var p) ? p.GetInt32() : 1,
                    pageSize = root.TryGetProperty("page_size", out var ps) ? ps.GetInt32() : 20,
                    totalRecords = root.TryGetProperty("total_records", out var tr) ? tr.GetInt32() : 0,
                    totalPages = root.TryGetProperty("total_pages", out var tp) ? tp.GetInt32() : 1
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
        public async Task<IActionResult> TrainModel([FromBody] TrainModelRequest request)
        {
            var userIdValue = HttpContext.Session.GetInt32("UserId");

            if (!userIdValue.HasValue || string.IsNullOrWhiteSpace(request?.ModelVersion))
                return BadRequest(new { error = "Session expired or invalid model version" });

            try
            {
                var userId = userIdValue.Value;
                var result = await _isolationForestClient.TrainModelAsync(request.ModelVersion, request.UseCorrectedLabels);

                await _activityLogService.LogActivityAsync(
                    userId,
                    "TRAIN_MODEL",
                    "MLModel",
                    request.ModelVersion,
                    new { request.UseCorrectedLabels }.ToString()
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
        public async Task<IActionResult> RetrainModel([FromBody] RetrainModelRequest request)
        {
            var userIdValue = HttpContext.Session.GetInt32("UserId");

            if (!userIdValue.HasValue || string.IsNullOrWhiteSpace(request?.ModelVersion))
                return BadRequest(new { error = "Session expired or invalid model version" });

            try
            {
                var userId = userIdValue.Value;
                var result = await _isolationForestClient.RetrainModelAsync(request.ModelVersion);

                await _activityLogService.LogActivityAsync(
                    userId,
                    "RETRAIN_MODEL",
                    "MLModel",
                    request.ModelVersion
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
        public async Task<IActionResult> ChangeLabel([FromBody] ChangeLabelRequest request)
        {
            var userIdValue = HttpContext.Session.GetInt32("UserId");

            if (!userIdValue.HasValue)
                return BadRequest(new { error = "Session expired" });

            if (string.IsNullOrWhiteSpace(request?.RequestId))
                return BadRequest(new { error = "Request ID is required and cannot be empty" });

            try
            {
                var userId = userIdValue.Value;
                var username = HttpContext.Session.GetString("Username") ?? "System";

                var result = await _isolationForestClient.UpdateLabelAsync(request.RequestId, request.Label, username);

                if (result == null)
                    return StatusCode(503, new { error = "ML Service unavailable or returned null response" });

                await _activityLogService.LogActivityAsync(
                    userId,
                    "CHANGE_LABEL",
                    "Audit",
                    request.RequestId,
                    new { request.Label }.ToString()
                );

                return Json(new { success = true, message = "Label updated successfully" });
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

    /// <summary>
    /// Request DTOs for JSON binding
    /// </summary>
    public class TrainModelRequest
    {
        [JsonPropertyName("modelVersion")]
        public string? ModelVersion { get; set; }

        [JsonPropertyName("useCorrectedLabels")]
        public bool UseCorrectedLabels { get; set; } = true;
    }

    public class RetrainModelRequest
    {
        [JsonPropertyName("modelVersion")]
        public string? ModelVersion { get; set; }
    }

    public class ChangeLabelRequest
    {
        [JsonPropertyName("requestId")]
        public string? RequestId { get; set; }

        [JsonPropertyName("label")]
        public bool Label { get; set; }
    }
}
