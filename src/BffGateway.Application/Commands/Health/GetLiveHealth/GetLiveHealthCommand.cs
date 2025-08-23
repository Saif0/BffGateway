using BffGateway.Application.Common.DTOs.Health;
using MediatR;

namespace BffGateway.Application.Commands.Health.GetLiveHealth;

public record GetLiveHealthCommand(string? CorrelationId) : IRequest<HealthReportDto>;


