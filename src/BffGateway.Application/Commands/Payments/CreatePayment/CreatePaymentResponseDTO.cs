namespace BffGateway.Application.Commands.Payments.CreatePayment;

public sealed record CreatePaymentResponseDTO(
    bool IsSuccess,
    string? PaymentId,
    string? ProviderReference,
    DateTime? ProcessedAt,
    int? UpstreamStatusCode = null);


