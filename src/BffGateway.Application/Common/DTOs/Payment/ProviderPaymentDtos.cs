namespace BffGateway.Application.Common.DTOs.Payment;

public sealed record ProviderPaymentRequest(decimal Total, string Curr, string Dest);

public sealed record ProviderPaymentResponse(bool Success, string TransactionId, string ProviderRef, DateTime ProcessedAt);


