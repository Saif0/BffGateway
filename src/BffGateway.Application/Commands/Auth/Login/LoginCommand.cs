using MediatR;

namespace BffGateway.Application.Commands.Auth.Login;

public record LoginCommand(string Username, string Password) : IRequest<LoginResponseDTO>;


