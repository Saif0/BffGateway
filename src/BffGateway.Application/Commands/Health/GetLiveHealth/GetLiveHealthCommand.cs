using BffGateway.Application.Common.DTOs.Health;
using MediatR;

namespace BffGateway.Application.Commands.Health.GetLiveHealth;

public sealed record GetLiveHealthCommand(string? CorrelationId) : IRequest<HealthReportDto>;


