using BffGateway.WebApi.Contracts;

namespace BffGateway.WebApi.Contracts.Payements.V1;

public record CreatePaymentResponseV1(
    bool IsSuccess,
    string? Message,
    string? PaymentId,
    string? ProviderReference,
    string? ProcessedAt
) : ApiResponseBase(IsSuccess, Message);
