using Microsoft.AspNetCore.Mvc;

namespace MockProvider.Controllers;

[ApiController]
[Route("api")]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;

    public AuthController(ILogger<AuthController> logger)
    {
        _logger = logger;
    }

    [HttpPost("authenticate")]
    public async Task<IActionResult> Authenticate([FromBody] AuthenticateRequest request)
    {
        _logger.LogInformation("Authentication attempt for user: {User}", request.User);

        // Simulate processing delay
        await Task.Delay(Random.Shared.Next(50, 120));

        // Simulate some authentication logic
        if (string.IsNullOrEmpty(request.User) || string.IsNullOrEmpty(request.Pwd))
        {
            return BadRequest(new { error = "Invalid credentials" });
        }

        // Simulate failure for specific test cases
        if (request.User == "fail")
        {
            return StatusCode(500, new { error = "Internal server error" });
        }

        if (request.User == "timeout")
        {
            await Task.Delay(5000); // Simulate timeout
        }

        // Generate mock JWT token
        var token = GenerateMockJwt(request.User);
        var expiresAt = DateTime.UtcNow.AddHours(1);

        var response = new AuthenticateResponse
        {
            Success = true,
            Token = token,
            ExpiresAt = expiresAt
        };

        return Ok(response);
    }

    private string GenerateMockJwt(string user)
    {
        var header = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9";
        var payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{{\"sub\":\"{user}\",\"exp\":{DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()}}}"));
        var signature = "mock_signature_" + Guid.NewGuid().ToString("N")[..16];

        return $"{header}.{payload}.{signature}";
    }
}

public class AuthenticateRequest
{
    public string User { get; set; } = string.Empty;
    public string Pwd { get; set; } = string.Empty;
}

public class AuthenticateResponse
{
    public bool Success { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
