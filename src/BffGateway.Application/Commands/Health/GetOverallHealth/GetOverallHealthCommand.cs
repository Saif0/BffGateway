using BffGateway.Application.Common.DTOs.Health;
using MediatR;

namespace BffGateway.Application.Commands.Health.GetOverallHealth;

public record GetOverallHealthCommand(string? CorrelationId) : IRequest<HealthReportDto>;


