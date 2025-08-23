using Microsoft.Extensions.Logging;

namespace BffGateway.Infrastructure.Providers.MockProvider.Health;

public class MockProviderHealthClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MockProviderHealthClient> _logger;

    public MockProviderHealthClient(HttpClient httpClient, ILogger<MockProviderHealthClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Performing MockProvider health check");

            var response = await _httpClient.GetAsync("/api/ping", cancellationToken);
            var isHealthy = response.StatusCode != System.Net.HttpStatusCode.ServiceUnavailable;

            _logger.LogDebug("MockProvider health check result: {IsHealthy}", isHealthy);
            return isHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MockProvider health check failed");
            return false;
        }
    }

    // Additional MockProvider-specific health endpoints
    // public async Task<bool> DeepHealthCheckAsync(...)
    // public async Task<HealthStatus> GetDetailedHealthAsync(...)
    // public async Task<bool> ReadinessCheckAsync(...)
}
