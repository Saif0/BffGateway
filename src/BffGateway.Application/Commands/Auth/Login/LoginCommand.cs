using BffGateway.Application.Common.Enums;
using MediatR;

namespace BffGateway.Application.Commands.Auth.Login;

public record LoginCommand(string Username, string Password, SimulationScenario Scenario = SimulationScenario.None) : IRequest<LoginResponseDto>;


