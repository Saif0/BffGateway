namespace BffGateway.Infrastructure.Providers.MockProvider.DTOs;

internal record MockProviderPaymentResponseDto(bool Success, string TransactionId, string ProviderRef, DateTime ProcessedAt);
