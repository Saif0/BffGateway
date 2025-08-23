namespace BffGateway.Application.Commands.Auth.Login;

public record LoginResponseDTO(bool IsSuccess, string? Jwt, DateTime? ExpiresAt, int? UpstreamStatusCode = null);


