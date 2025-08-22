namespace MockProvider.DTOs;

public record AuthenticateRequestDTO(string User, string Pwd);

public record AuthenticateResponseDTO(bool Success, string Token, DateTime ExpiresAt);


