using BffGateway.Application.Common.DTOs.Health;
using Microsoft.Extensions.Logging;
using MediatR;

namespace BffGateway.Application.Commands.Health.GetLiveHealth;

public class GetLiveHealthCommandHandler : IRequestHandler<GetLiveHealthCommand, HealthReportDto>
{
    private readonly ILogger<GetLiveHealthCommandHandler> _logger;

    public GetLiveHealthCommandHandler(ILogger<GetLiveHealthCommandHandler> logger)
    {
        _logger = logger;
    }

    public Task<HealthReportDto> Handle(GetLiveHealthCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Health liveness check (command) requested");

        var startTime = DateTime.UtcNow;

        var report = new HealthReportDto
        {
            Status = "Healthy",
            CorrelationId = request.CorrelationId,
            TotalDurationMs = (DateTime.UtcNow - startTime).TotalMilliseconds,
            Entries = new Dictionary<string, HealthEntryDto>
            {
                ["self"] = new HealthEntryDto
                {
                    Status = "Healthy",
                    Description = "Application is running",
                    DurationMs = (DateTime.UtcNow - startTime).TotalMilliseconds
                }
            }
        };

        return Task.FromResult(report);
    }
}


