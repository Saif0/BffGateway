using BffGateway.Application.Abstractions.Providers;
using BffGateway.Application.Common.DTOs.Health;
using Microsoft.Extensions.Logging;
using MediatR;

namespace BffGateway.Application.Commands.Health.GetReadyHealth;

public class GetReadyHealthCommandHandler : IRequestHandler<GetReadyHealthCommand, HealthReportDto>
{
    private readonly IProviderClient _providerClient;
    private readonly ILogger<GetReadyHealthCommandHandler> _logger;

    public GetReadyHealthCommandHandler(IProviderClient providerClient, ILogger<GetReadyHealthCommandHandler> logger)
    {
        _providerClient = providerClient;
        _logger = logger;
    }

    public async Task<HealthReportDto> Handle(GetReadyHealthCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Health readiness check (command) requested");

        var startTime = DateTime.UtcNow;

        var selfEntry = new HealthEntryDto
        {
            Status = "Healthy",
            Description = "Application is running",
            DurationMs = 0
        };

        var providerEntry = new HealthEntryDto
        {
            Status = "Healthy",
            Description = "Provider is responding",
            DurationMs = 0
        };

        try
        {
            var providerStart = DateTime.UtcNow;
            var isProviderHealthy = await _providerClient.HealthCheckAsync(cancellationToken);
            providerEntry.DurationMs = (DateTime.UtcNow - providerStart).TotalMilliseconds;

            if (!isProviderHealthy)
            {
                providerEntry.Status = "Degraded";
                providerEntry.Description = "Provider is not responding properly";
                _logger.LogWarning("Provider health check failed - provider not responding");
            }
        }
        catch (OperationCanceledException)
        {
            providerEntry.Status = "Degraded";
            providerEntry.Description = "Provider health check timed out";
            _logger.LogWarning("Provider health check timed out");
        }
        catch (Exception ex) when (ex.GetType().Name == "BrokenCircuitException")
        {
            providerEntry.Status = "Degraded";
            providerEntry.Description = "Provider circuit breaker is open";
            _logger.LogWarning("Provider circuit breaker is open");
        }
        catch (Exception ex)
        {
            providerEntry.Status = "Unhealthy";
            providerEntry.Description = "Provider health check failed";
            _logger.LogError(ex, "Provider health check failed with exception");
        }

        var overallStatus = providerEntry.Status == "Healthy" && selfEntry.Status == "Healthy" ? "Healthy" :
                           providerEntry.Status == "Unhealthy" ? "Unhealthy" : "Degraded";

        return new HealthReportDto
        {
            Status = overallStatus,
            CorrelationId = request.CorrelationId,
            TotalDurationMs = (DateTime.UtcNow - startTime).TotalMilliseconds,
            Entries = new Dictionary<string, HealthEntryDto>
            {
                ["self"] = selfEntry,
                ["provider"] = providerEntry
            }
        };
    }
}


