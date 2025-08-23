namespace BffGateway.Application.DTOs.Payment;

public record ProviderPaymentRequest(decimal Total, string Curr, string Dest);

public record ProviderPaymentResponse(bool Success, string TransactionId, string ProviderRef, DateTime ProcessedAt);


