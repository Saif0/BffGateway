namespace BffGateway.Application.Common.DTOs;

public record ProviderAuthRequest(string User, string Pwd);

public record ProviderAuthResponse(bool Success, string Token, DateTime ExpiresAt);


