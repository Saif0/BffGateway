namespace BffGateway.Application.Common.DTOs;

public record ProviderPaymentRequest(decimal Total, string Curr, string Dest);

public record ProviderPaymentResponse(bool Success, string TransactionId, string ProviderRef, DateTime ProcessedAt);


