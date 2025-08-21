using System.ComponentModel.DataAnnotations;

namespace BffGateway.WebApi.Models.V1;

public class LoginRequestV1
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseV1
{
    public bool IsSuccess { get; set; }
    public string? Jwt { get; set; }
    public string? ExpiresAt { get; set; }
}
