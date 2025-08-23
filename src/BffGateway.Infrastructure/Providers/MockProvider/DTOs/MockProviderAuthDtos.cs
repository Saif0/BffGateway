namespace BffGateway.Infrastructure.Providers.MockProvider.DTOs;

internal record MockProviderAuthResponseDto(bool Success, string Token, DateTime ExpiresAt);
