namespace BffGateway.Infrastructure.Providers.MockProvider.DTOs;

internal sealed record MockProviderPaymentResponseDto(bool Success, string TransactionId, string ProviderRef, DateTime ProcessedAt);
