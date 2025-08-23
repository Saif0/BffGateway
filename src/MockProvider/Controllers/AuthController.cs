using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MockProvider.DTOs;
using MockProvider.DTOs.Enums;

namespace MockProvider.Controllers;

[ApiController]
[Route("api")]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;
    private readonly LatencyOptions _latency;

    public AuthController(ILogger<AuthController> logger, IOptionsSnapshot<LatencyOptions> latencyOptions)
    {
        _logger = logger;
        _latency = latencyOptions.Value;
    }

    [HttpPost("authenticate")]
    public async Task<IActionResult> Authenticate([FromBody] AuthenticateRequestDTO request, [FromQuery] SimulationScenario scenario = SimulationScenario.None)
    {
        _logger.LogInformation("Authentication attempt for user: {User} with scenario: {Scenario}", request.User, scenario);

        // Simulate processing delay (configurable)
        var min = Math.Max(0, _latency.AuthMinMs);
        var max = Math.Max(min + 1, _latency.AuthMaxMs + 1); // upper bound exclusive
        await Task.Delay(Random.Shared.Next(min, max));

        // Simulate some authentication logic
        if (string.IsNullOrEmpty(request.User) || string.IsNullOrEmpty(request.Pwd))
        {
            return BadRequest(new { error = "Invalid credentials" });
        }

        // Simulate failure/timeout/limit based on scenario
        if (scenario == SimulationScenario.Fail)
        {
            return StatusCode(500, new { error = "Internal server error" });
        }
        if (scenario == SimulationScenario.Timeout)
        {
            await Task.Delay(_latency.AuthTimeoutMs); // Simulate timeout
        }
        if (scenario == SimulationScenario.LimitExceeded)
        {
            return StatusCode(429, new { error = "Request Exceeding Limit" });
        }

        // Generate mock JWT token
        var token = GenerateMockJwt(request.User);
        var expiresAt = DateTime.UtcNow.AddHours(1);

        var response = new AuthenticateResponseDTO(true, token, expiresAt);

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
