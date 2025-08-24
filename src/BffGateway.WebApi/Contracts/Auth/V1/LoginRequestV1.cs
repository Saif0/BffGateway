using System.ComponentModel.DataAnnotations;

namespace BffGateway.WebApi.Contracts.V1;

public record LoginRequestV1(
    [Required] string Username,
    [Required] string Password
);


