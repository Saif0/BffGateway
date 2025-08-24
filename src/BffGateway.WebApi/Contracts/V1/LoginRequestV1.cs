using System.ComponentModel.DataAnnotations;

namespace BffGateway.WebApi.Contracts.V1;

public class LoginRequestV1
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}


