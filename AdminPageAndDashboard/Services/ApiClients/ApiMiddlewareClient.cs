using System.Text.Json;

namespace AdminPageAndDashboard.Services.ApiClients
{
    public class ApiMiddlewareClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ApiMiddlewareClient> _logger;

        public ApiMiddlewareClient(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<ApiMiddlewareClient> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            
            // Set a reasonable timeout
            _httpClient.Timeout = TimeSpan.FromSeconds(60);
        }

        private string GetBaseUrl()
        {
            var url = _configuration.GetValue<string>("ServiceUrls:ApiMiddleware");
            if (string.IsNullOrWhiteSpace(url))
            {
                _logger.LogWarning("ApiMiddleware URL not configured, using default: http://localhost:6000");
                return "http://localhost:6000";
            }
            return url;
        }

        public async Task<JsonDocument?> GetHealthAsync()
        {
            try
            {
                var baseUrl = GetBaseUrl();
                _logger.LogInformation($"Attempting health check on: {baseUrl}/health");
                
                var response = await _httpClient.GetAsync($"{baseUrl}/health");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonDocument.Parse(content);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning($"Health check failed: {ex.Message}");
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning($"Health check timeout: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error in health check: {ex.Message}");
                return null;
            }
        }

        public async Task<JsonDocument?> GetRoutingDecisionsAsync(
            int page = 1, 
            int pageSize = 20,
            string? clientIp = null,
            bool? isAnomaly = null,
            string? routedTo = null,
            float? minConfidence = null,
            float? maxConfidence = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            int? statusCode = null)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var baseUrl = GetBaseUrl();
                var url = $"{baseUrl}/audit/routing?page={page}&pageSize={pageSize}";

                // Add optional query parameters (camelCase as per API spec)
                if (!string.IsNullOrWhiteSpace(clientIp))
                    url += $"&clientIp={Uri.EscapeDataString(clientIp)}";

                if (isAnomaly.HasValue)
                    url += $"&isAnomaly={isAnomaly.Value.ToString().ToLower()}";

                if (!string.IsNullOrWhiteSpace(routedTo))
                    url += $"&routedTo={Uri.EscapeDataString(routedTo)}";

                if (minConfidence.HasValue)
                    url += $"&minConfidence={minConfidence.Value}";

                if (maxConfidence.HasValue)
                    url += $"&maxConfidence={maxConfidence.Value}";

                if (dateFrom.HasValue)
                    url += $"&dateFrom={dateFrom.Value:O}";

                if (dateTo.HasValue)
                    url += $"&dateTo={dateTo.Value:O}";

                if (statusCode.HasValue)
                    url += $"&statusCode={statusCode.Value}";

                _logger.LogDebug($"Fetching routing decisions from: {url}");
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonDocument.Parse(content);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning($"Failed to fetch routing decisions: {ex.Message}");
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning($"Routing decisions request timeout: {ex.Message}");
                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogError($"Invalid JSON response from routing decisions: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error fetching routing decisions: {ex.Message}");
                return null;
            }
        }

        public async Task<JsonDocument?> GetRoutingDecisionDetailsAsync(string requestId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(requestId))
                    throw new ArgumentException("Request ID cannot be null or empty", nameof(requestId));

                var baseUrl = GetBaseUrl();
                var url = $"{baseUrl}/audit/routing/{Uri.EscapeDataString(requestId)}";
                
                _logger.LogDebug($"Fetching routing decision details from: {url}");
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonDocument.Parse(content);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning($"Failed to fetch routing decision details: {ex.Message}");
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning($"Routing decision details request timeout: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error fetching routing decision details: {ex.Message}");
                return null;
            }
        }

        public async Task<JsonDocument?> GetCachedRequestsAsync()
        {
            try
            {
                var baseUrl = GetBaseUrl();
                var url = $"{baseUrl}/api/cached-requests";
                
                _logger.LogDebug($"Fetching cached requests from: {url}");
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonDocument.Parse(content);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning($"Failed to fetch cached requests: {ex.Message}");
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning($"Cached requests request timeout: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error fetching cached requests: {ex.Message}");
                return null;
            }
        }

        public async Task<JsonDocument?> GetCachedRequestAsync(int id)
        {
            try
            {
                if (id <= 0)
                    throw new ArgumentException("ID must be greater than 0", nameof(id));

                var baseUrl = GetBaseUrl();
                var url = $"{baseUrl}/api/cached-requests/{id}";
                
                _logger.LogDebug($"Fetching cached request from: {url}");
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonDocument.Parse(content);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning($"Failed to fetch cached request: {ex.Message}");
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning($"Cached request fetch timeout: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error fetching cached request: {ex.Message}");
                return null;
            }
        }
        public async Task<JsonDocument?> GetChartDataAsync()
        {
            try
            {
                var baseUrl = GetBaseUrl();
                var url = $"{baseUrl}/audit/chart";

                _logger.LogDebug($"Fetching chart data from: {url}");

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonDocument.Parse(content);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning($"Failed to fetch chart data: {ex.Message}");
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning($"Chart data request timeout: {ex.Message}");
                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogError($"Invalid JSON response from chart data endpoint: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error fetching chart data: {ex.Message}");
                return null;
            }
        }
    }
}
