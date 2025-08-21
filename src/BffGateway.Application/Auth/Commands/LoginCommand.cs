using MediatR;

namespace BffGateway.Application.Auth.Commands;

public record LoginCommand(string Username, string Password) : IRequest<LoginResponse>;

public record LoginResponse(bool IsSuccess, string? Jwt, DateTime? ExpiresAt);
