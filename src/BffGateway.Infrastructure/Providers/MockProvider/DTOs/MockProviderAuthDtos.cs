namespace BffGateway.Infrastructure.Providers.MockProvider.DTOs;

internal sealed record MockProviderAuthResponseDto(bool Success, string Token, DateTime ExpiresAt);
