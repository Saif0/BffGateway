using MediatR;

namespace BffGateway.Application.Payments.Commands;

public record CreatePaymentCommand(decimal Amount, string Currency, string DestinationAccount)
    : IRequest<CreatePaymentResponse>;

public record CreatePaymentResponse(
    bool IsSuccess,
    string? PaymentId,
    string? ProviderReference,
    DateTime? ProcessedAt);
