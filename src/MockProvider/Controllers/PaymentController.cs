using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MockProvider.DTOs;

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
    public async Task<IActionResult> Pay([FromBody] PayRequestDTO request)
    {
        _logger.LogInformation("Payment request for amount: {Total} {Curr} to {Dest}",
            request.Total, request.Curr, request.Dest);

        // Simulate processing delay (configurable)
        var min = Math.Max(0, _latency.PayMinMs);
        var max = Math.Max(min + 1, _latency.PayMaxMs + 1); // upper bound exclusive
        await Task.Delay(Random.Shared.Next(min, max));

        // Validate request
        if (request.Total <= 0 || string.IsNullOrEmpty(request.Curr) || string.IsNullOrEmpty(request.Dest))
        {
            return BadRequest(new { error = "Invalid payment request" });
        }

        // Simulate failure for specific test cases
        if (request.Dest == "fail")
        {
            return StatusCode(500, new { error = "Payment processing failed" });
        }
        // Simulate Request Exceeding Limit
        if (request.Dest == "limit")
        {
            return StatusCode(429, new { error = "Request Exceeding Limit" });
        }

        if (request.Dest == "timeout")
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
