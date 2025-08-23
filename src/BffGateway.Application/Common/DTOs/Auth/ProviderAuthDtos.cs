namespace BffGateway.Application.Common.DTOs.Auth;

public sealed record ProviderAuthRequest(string User, string Pwd);

public sealed record ProviderAuthResponse(bool Success, string Token, DateTime ExpiresAt);


