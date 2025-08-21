using System.ComponentModel.DataAnnotations;

namespace BffGateway.WebApi.Models.V2;

public class LoginRequestV2
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseV2
{
    public bool Success { get; set; }
    public TokenInfo? Token { get; set; }
    public UserInfo? User { get; set; }
}

public class TokenInfo
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public string TokenType { get; set; } = "Bearer";
}

public class UserInfo
{
    public string Username { get; set; } = string.Empty;
}
