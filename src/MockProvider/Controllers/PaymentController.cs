using Microsoft.AspNetCore.Mvc;

namespace MockProvider.Controllers;

[ApiController]
[Route("api")]
public class PaymentController : ControllerBase
{
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(ILogger<PaymentController> logger)
    {
        _logger = logger;
    }

    [HttpPost("pay")]
    public async Task<IActionResult> Pay([FromBody] PayRequest request)
    {
        _logger.LogInformation("Payment request for amount: {Total} {Curr} to {Dest}",
            request.Total, request.Curr, request.Dest);

        // Simulate processing delay
        await Task.Delay(Random.Shared.Next(30, 100));

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

        if (request.Dest == "timeout")
        {
            await Task.Delay(5000); // Simulate timeout
        }

        // Generate mock payment response
        var response = new PayResponse
        {
            Success = true,
            TransactionId = Guid.NewGuid().ToString(),
            ProviderRef = $"PROV_{DateTime.UtcNow:yyyyMMddHHmmss}_{Random.Shared.Next(1000, 9999)}",
            ProcessedAt = DateTime.UtcNow
        };

        return Ok(response);
    }
}

public class PayRequest
{
    public decimal Total { get; set; }
    public string Curr { get; set; } = string.Empty;
    public string Dest { get; set; } = string.Empty;
}

public class PayResponse
{
    public bool Success { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string ProviderRef { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
}
