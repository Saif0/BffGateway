using BffGateway.WebApi.Contracts;

namespace BffGateway.WebApi.Contracts.V1;

public record LoginResponseV1(
    bool IsSuccess,
    string? Message,
    string? Jwt,
    DateTime? ExpiresAt
) : ApiResponseBase(IsSuccess, Message);


