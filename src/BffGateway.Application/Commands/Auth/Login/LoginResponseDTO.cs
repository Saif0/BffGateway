namespace BffGateway.Application.Commands.Auth.Login;

public record LoginResponseDto(
    bool IsSuccess,
    string? Jwt,
    DateTime? ExpiresAt,
    string? Message = null,
    int? UpstreamStatusCode = null);
