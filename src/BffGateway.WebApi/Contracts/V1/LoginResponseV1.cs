namespace BffGateway.WebApi.Contracts.V1;

public class LoginResponseV1
{
    public bool IsSuccess { get; set; }
    public string? Jwt { get; set; }
    public string? ExpiresAt { get; set; }
}


