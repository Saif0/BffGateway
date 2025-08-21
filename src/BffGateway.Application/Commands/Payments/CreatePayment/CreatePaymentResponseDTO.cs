namespace BffGateway.Application.Commands.Payments.CreatePayment;

public record CreatePaymentResponseDTO(
    bool IsSuccess,
    string? PaymentId,
    string? ProviderReference,
    DateTime? ProcessedAt);


