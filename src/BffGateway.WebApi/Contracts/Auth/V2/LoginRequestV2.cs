using System.ComponentModel.DataAnnotations;

namespace BffGateway.WebApi.Contracts.V2;

public record LoginRequestV2(
    [Required] string Username,
    [Required] string Password
);


