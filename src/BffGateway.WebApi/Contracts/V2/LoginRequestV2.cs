using System.ComponentModel.DataAnnotations;

namespace BffGateway.WebApi.Contracts.V2;

public class LoginRequestV2
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}


