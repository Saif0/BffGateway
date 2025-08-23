namespace MockProvider.DTOs;

public sealed record AuthenticateRequestDTO(string User, string Pwd);

public sealed record AuthenticateResponseDTO(bool Success, string Token, DateTime ExpiresAt);


