namespace BffGateway.Application.Common.DTOs.Auth;

public record ProviderAuthRequest(string User, string Pwd);

public record ProviderAuthResponse(bool Success, string Token, DateTime ExpiresAt);


