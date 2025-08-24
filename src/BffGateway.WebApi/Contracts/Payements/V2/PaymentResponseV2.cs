using BffGateway.WebApi.Contracts;

namespace BffGateway.WebApi.Contracts.Payements.V2;

public record CreatePaymentResponseV2(
    bool IsSuccess,
    string? Message,
    string? PaymentId,
    string? ProviderReference,
    DateTime? ProcessedAt
) : ApiResponseBase(IsSuccess, Message);
