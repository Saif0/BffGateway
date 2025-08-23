using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MockProvider.DTOs;
using MockProvider.DTOs.Enums;

namespace MockProvider.Controllers;

[ApiController]
[Route("api")]
public class PaymentController : ControllerBase
{
    private readonly ILogger<PaymentController> _logger;
    private readonly LatencyOptions _latency;

    public PaymentController(ILogger<PaymentController> logger, IOptionsSnapshot<LatencyOptions> latencyOptions)
    {
        _logger = logger;
        _latency = latencyOptions.Value;
    }

    [HttpPost("pay")]
    public async Task<IActionResult> Pay([FromBody] PayRequestDTO request, [FromQuery] SimulationScenario scenario = SimulationScenario.None)
    {
        _logger.LogInformation("Payment request for amount: {Total} {Curr} to {Dest} with scenario: {Scenario}",
            request.Total, request.Curr, request.Dest, scenario);

        // Simulate processing delay (configurable)
        var min = Math.Max(0, _latency.PayMinMs);
        var max = Math.Max(min + 1, _latency.PayMaxMs + 1); // upper bound exclusive
        await Task.Delay(Random.Shared.Next(min, max));

        // Validate request
        if (request.Total <= 0 || string.IsNullOrEmpty(request.Curr) || string.IsNullOrEmpty(request.Dest))
        {
            return BadRequest(new { error = "Invalid payment request" });
        }

        // Simulate failure/timeout/limit based on scenario
        if (scenario == SimulationScenario.Fail)
        {
            return StatusCode(500, new { error = "Payment processing failed" });
        }
        if (scenario == SimulationScenario.LimitExceeded)
        {
            return StatusCode(429, new { error = "Request Exceeding Limit" });
        }
        if (scenario == SimulationScenario.Timeout)
        {
            await Task.Delay(_latency.PayTimeoutMs); // Simulate timeout
        }

        // Generate mock payment response
        var response = new PayResponseDTO(
            true,
            Guid.NewGuid().ToString(),
            $"PROV_{DateTime.UtcNow:yyyyMMddHHmmss}_{Random.Shared.Next(1000, 9999)}",
            DateTime.UtcNow
        );

        return Ok(response);
    }
}
