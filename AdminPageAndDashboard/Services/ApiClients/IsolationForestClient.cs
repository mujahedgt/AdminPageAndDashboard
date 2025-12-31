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
        /// Now uses enhanced 9-feature detection system.
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

                var json = JsonSerializer.Serialize(requestBody);
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
        /// Train a brand new Isolation Forest model with enhanced 9-feature system.
        /// </summary>
        /// <param name="modelVersion">Version identifier for the new model</param>
        /// <param name="useCorrectedLabels">Whether to use user-corrected labels</param>
        /// <param name="contamination">Expected proportion of anomalies (0.0-0.5, default 0.1)</param>
        /// <param name="nEstimators">Number of trees in the forest (default 150)</param>
        /// <param name="maxSamples">Max samples per tree (default 256)</param>
        /// <param name="recalculateFeatures">Whether to recalculate all features before training (useful for upgrades)</param>
        public async Task<JsonDocument?> TrainModelAsync(
            string modelVersion,
            bool useCorrectedLabels = true,
            double? contamination = null,
            int? nEstimators = null,
            int? maxSamples = null,
            bool recalculateFeatures = true)
        {
            try
            {
                var baseUrl = GetBaseUrl();

                // Build training_params object with only non-null values
                var trainingParams = new Dictionary<string, object>();

                if (contamination.HasValue)
                    trainingParams["contamination"] = contamination.Value;

                if (nEstimators.HasValue)
                    trainingParams["n_estimators"] = nEstimators.Value;

                if (maxSamples.HasValue)
                    trainingParams["max_samples"] = maxSamples.Value;

                if (recalculateFeatures)
                    trainingParams["recalculate_features"] = true;

                var requestBody = new
                {
                    model_version = modelVersion,
                    use_corrected_labels = useCorrectedLabels,
                    training_params = trainingParams.Count > 0 ? trainingParams : null
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation($"Training model {modelVersion} with recalculate_features={recalculateFeatures}");

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
        /// <param name="modelVersion">Version identifier for the retrained model</param>
        /// <param name="recalculateFeatures">Whether to recalculate all features before retraining</param>
        public async Task<JsonDocument?> RetrainModelAsync(
            string modelVersion,
            bool recalculateFeatures = true)
        {
            try
            {
                var baseUrl = GetBaseUrl();

                var requestBody = new
                {
                    model_version = modelVersion,
                    recalculate_features = recalculateFeatures
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation($"Retraining model {modelVersion} with recalculate_features={recalculateFeatures}");

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
            string requestId,
            bool userLabel,
            string changedBy)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(requestId))
                {
                    _logger.LogError("UpdateLabelAsync: Request ID is empty");
                    throw new ArgumentException("Request ID cannot be empty", nameof(requestId));
                }

                var baseUrl = GetBaseUrl();
                var url = $"{baseUrl}/labeling/label/{Uri.EscapeDataString(requestId)}";

                var requestBody = new
                {
                    user_label = userLabel,
                    changed_by = changedBy
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation($"UpdateLabelAsync: Sending PUT request to {url} with payload: {json}");

                var response = await _httpClient.PutAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"UpdateLabelAsync failed with status {response.StatusCode}: {responseContent}");
                }

                response.EnsureSuccessStatusCode();

                var responseContent2 = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"UpdateLabelAsync successful: {responseContent2}");
                return JsonDocument.Parse(responseContent2);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"Failed to update label - HTTP error: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to update label - Unexpected error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get feature importance analysis from the current model.
        /// Helps understand which features contribute most to anomaly detection.
        /// </summary>
        public async Task<JsonDocument?> GetFeatureImportanceAsync()
        {
            try
            {
                var baseUrl = GetBaseUrl();
                var response = await _httpClient.GetAsync($"{baseUrl}/training/feature-importance");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonDocument.Parse(content);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning($"Failed to fetch feature importance: {ex.Message}");
                return null;
            }
        }
    }
}