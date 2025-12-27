using System.Text.Json;
using System.Text;

namespace AdminPageAndDashboard.Services.ApiClients
{
    public class IsolationForestClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<IsolationForestClient> _logger;

        public IsolationForestClient(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<IsolationForestClient> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        private string GetBaseUrl()
        {
            return _configuration.GetValue<string>("ServiceUrls:IsolationForestServer")
                ?? "http://localhost:8000";
        }

        /// <summary>
        /// Health check endpoint.
        /// </summary>
        public async Task<JsonDocument?> GetHealthAsync()
        {
            try
            {
                var baseUrl = GetBaseUrl();
                var response = await _httpClient.GetAsync($"{baseUrl}/");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonDocument.Parse(content);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning($"IsolationForest health check failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get comprehensive server and model performance statistics.
        /// Note: Endpoint has trailing slash as per API spec.
        /// </summary>
        public async Task<JsonDocument?> GetStatisticsAsync()
        {
            try
            {
                var baseUrl = GetBaseUrl();
                var response = await _httpClient.GetAsync($"{baseUrl}/statistics/");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonDocument.Parse(content);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning($"Failed to fetch IsolationForest statistics: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get analyzed requests with advanced filtering (snake_case parameters).
        /// </summary>
        public async Task<JsonDocument?> GetAuditAsync(
            int page = 1,
            int pageSize = 20,
            string? ipAddress = null,
            bool? isAnomaly = null,
            float? minConfidence = null,
            float? maxConfidence = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            bool? hasUserLabel = null)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var baseUrl = GetBaseUrl();
                var url = $"{baseUrl}/audit/requests?page={page}&page_size={pageSize}";

                // Add optional query parameters (snake_case as per API spec)
                if (!string.IsNullOrWhiteSpace(ipAddress))
                    url += $"&ip_address={Uri.EscapeDataString(ipAddress)}";

                if (isAnomaly.HasValue)
                    url += $"&is_anomaly={isAnomaly.Value}";

                if (minConfidence.HasValue)
                    url += $"&min_confidence={minConfidence.Value}";

                if (maxConfidence.HasValue)
                    url += $"&max_confidence={maxConfidence.Value}";

                if (dateFrom.HasValue)
                    url += $"&date_from={dateFrom.Value:O}";

                if (dateTo.HasValue)
                    url += $"&date_to={dateTo.Value:O}";

                if (hasUserLabel.HasValue)
                    url += $"&has_user_label={hasUserLabel.Value}";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonDocument.Parse(content);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning($"Failed to fetch audit requests: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Analyze a request for anomalies using the active Isolation Forest model.
        /// </summary>
        public async Task<JsonDocument?> AnalyzeRequestAsync(
            string requestId,
            string ipAddress,
            string endpoint,
            string httpMethod,
            Dictionary<string, string> headers,
            object? payload = null,
            DateTime? timestamp = null)
        {
            try
            {
                var baseUrl = GetBaseUrl();
                
                var requestBody = new
                {
                    request_id = requestId,
                    ip_address = ipAddress,
                    endpoint = endpoint,
                    http_method = httpMethod,
                    headers = headers,
                    payload = payload,
                    timestamp = timestamp ?? DateTime.UtcNow
                };

                var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{baseUrl}/analyze", content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonDocument.Parse(responseContent);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning($"Failed to analyze request: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Train a brand new Isolation Forest model.
        /// </summary>
        public async Task<JsonDocument?> TrainModelAsync(
            string modelVersion,
            bool useCorrectedLabels = true,
            object? trainingParams = null)
        {
            try
            {
                var baseUrl = GetBaseUrl();

                var requestBody = new
                {
                    model_version = modelVersion,
                    use_corrected_labels = useCorrectedLabels,
                    training_params = trainingParams
                };

                var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{baseUrl}/training/train", content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonDocument.Parse(responseContent);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning($"Failed to train model: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Retrain the model using user-corrected labels (feedback loop).
        /// </summary>
        public async Task<JsonDocument?> RetrainModelAsync(string modelVersion)
        {
            try
            {
                var baseUrl = GetBaseUrl();

                var requestBody = new { model_version = modelVersion };
                var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{baseUrl}/training/retrain", content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonDocument.Parse(responseContent);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning($"Failed to retrain model: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Update user feedback label for a specific request.
        /// </summary>
        public async Task<JsonDocument?> UpdateLabelAsync(
            int requestId,
            bool userLabel,
            string changedBy)
        {
            try
            {
                if (requestId <= 0)
                    throw new ArgumentException("Request ID must be greater than 0", nameof(requestId));

                var baseUrl = GetBaseUrl();

                var requestBody = new
                {
                    user_label = userLabel,
                    changed_by = changedBy
                };

                var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync(
                    $"{baseUrl}/labeling/label/{requestId}",
                    content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonDocument.Parse(responseContent);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning($"Failed to update label: {ex.Message}");
                return null;
            }
        }
    }
}
