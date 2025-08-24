using BffGateway.WebApi.Contracts;

namespace BffGateway.WebApi.Contracts.V2;

public record LoginResponseV2(
    bool IsSuccess,
    string? Message,
    TokenInfo? Token,
    UserInfo? User
) : ApiResponseBase(IsSuccess, Message);

public record TokenInfo(
    string AccessToken,
    DateTime? ExpiresAt,
    string TokenType = "Bearer"
);

public record UserInfo(
    string Username
);


