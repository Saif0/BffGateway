using BffGateway.Application.Common.DTOs.Health;
using MediatR;

namespace BffGateway.Application.Commands.Health.GetReadyHealth;

public record GetReadyHealthCommand(string? CorrelationId) : IRequest<HealthReportDto>;


