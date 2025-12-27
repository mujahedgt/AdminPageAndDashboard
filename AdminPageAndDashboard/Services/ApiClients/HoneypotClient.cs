using System.Text.Json;

namespace AdminPageAndDashboard.Services.ApiClients
{
    public class HoneypotClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<HoneypotClient> _logger;

        public HoneypotClient(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<HoneypotClient> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        private string GetBaseUrl()
        {
            return _configuration.GetValue<string>("ServiceUrls:Honeypot")
                ?? "http://localhost:5001";
        }

        /// <summary>
        /// Health check endpoint (capital H per API spec).
        /// </summary>
        public async Task<JsonDocument?> GetHealthAsync()
        {
            try
            {
                var baseUrl = GetBaseUrl();
                var response = await _httpClient.GetAsync($"{baseUrl}/Health");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonDocument.Parse(content);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning($"Honeypot health check failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get honeypot captured requests with filtering (camelCase parameters).
        /// </summary>
        public async Task<JsonDocument?> GetRequestsAsync(
            int page = 1,
            int pageSize = 20,
            string? ipAddress = null,
            string? endpoint = null,
            int? statusCode = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var baseUrl = GetBaseUrl();
                var url = $"{baseUrl}/audit/requests?page={page}&pageSize={pageSize}";

                // Add optional query parameters (camelCase as per API spec)
                if (!string.IsNullOrWhiteSpace(ipAddress))
                    url += $"&ipAddress={Uri.EscapeDataString(ipAddress)}";

                if (!string.IsNullOrWhiteSpace(endpoint))
                    url += $"&endpoint={Uri.EscapeDataString(endpoint)}";

                if (statusCode.HasValue)
                    url += $"&statusCode={statusCode.Value}";

                if (dateFrom.HasValue)
                    url += $"&dateFrom={dateFrom.Value:O}";

                if (dateTo.HasValue)
                    url += $"&dateTo={dateTo.Value:O}";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonDocument.Parse(content);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning($"Failed to fetch honeypot requests: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Delete a specific honeypot request by ID.
        /// </summary>
        public async Task<bool> DeleteRequestAsync(int requestId)
        {
            try
            {
                if (requestId <= 0)
                    throw new ArgumentException("Request ID must be greater than 0", nameof(requestId));

                var baseUrl = GetBaseUrl();
                var response = await _httpClient.DeleteAsync($"{baseUrl}/audit/requests/{requestId}");
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning($"Failed to delete honeypot request: {ex.Message}");
                return false;
            }
        }
    }
}