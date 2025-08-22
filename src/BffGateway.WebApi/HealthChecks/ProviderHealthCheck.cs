using BffGateway.Application.Abstractions.Providers;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Polly.CircuitBreaker;

namespace BffGateway.WebApi.HealthChecks;

public class ProviderHealthCheck : IHealthCheck
{
    private readonly IProviderClient _providerClient;
    private readonly ILogger<ProviderHealthCheck> _logger;

    public ProviderHealthCheck(IProviderClient providerClient, ILogger<ProviderHealthCheck> logger)
    {
        _providerClient = providerClient;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Performing provider health check");

            var isHealthy = await _providerClient.HealthCheckAsync(cancellationToken);

            if (isHealthy)
            {
                _logger.LogDebug("Provider health check passed");
                return HealthCheckResult.Healthy("Provider is responding");
            }
            else
            {
                _logger.LogWarning("Provider health check failed - provider not responding");
                return HealthCheckResult.Degraded("Provider is not responding properly");
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Provider health check timed out");
            return HealthCheckResult.Degraded("Provider health check timed out");
        }
        catch (BrokenCircuitException)
        {
            _logger.LogWarning("Provider circuit breaker is open");
            return HealthCheckResult.Degraded("Provider circuit breaker is open");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Provider health check failed with exception");
            return HealthCheckResult.Unhealthy("Provider health check failed", ex);
        }
    }
}
