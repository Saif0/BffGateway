using BffGateway.Application.Commands.Health.GetLiveHealth;
using BffGateway.Application.Commands.Health.GetOverallHealth;
using BffGateway.Application.Commands.Health.GetReadyHealth;
using Microsoft.AspNetCore.Mvc;
using MediatR;

namespace BffGateway.WebApi.Controllers;

[ApiController]
[Route("health")]
[ApiExplorerSettings(IgnoreApi = false)]
[ApiVersion("1.0")]
[ApiVersion("2.0")]
[ApiVersionNeutral]
public class HealthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<HealthController> _logger;

    public HealthController(IMediator mediator, ILogger<HealthController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Liveness probe - indicates if the application is running
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health check result for liveness</returns>
    [HttpGet("live")]
    public async Task<IActionResult> Live(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Health liveness check requested");

        var correlationId = Request.Headers["X-Correlation-ID"].ToString();
        var result = await _mediator.Send(new GetLiveHealthCommand(correlationId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Readiness probe - indicates if the application is ready to serve traffic
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health check result for readiness</returns>
    [HttpGet("ready")]
    public async Task<IActionResult> Ready(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Health readiness check requested");

        var correlationId = Request.Headers["X-Correlation-ID"].ToString();
        var result = await _mediator.Send(new GetReadyHealthCommand(correlationId), cancellationToken);
        return result.Status == "Healthy" ? Ok(result) : StatusCode(503, result);
    }

    /// <summary>
    /// Overall health status including all checks
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Complete health check result</returns>
    [HttpGet]
    public async Task<IActionResult> Health(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Complete health check requested");

        var correlationId = Request.Headers["X-Correlation-ID"].ToString();
        var result = await _mediator.Send(new GetOverallHealthCommand(correlationId), cancellationToken);
        return result.Status == "Healthy" ? Ok(result) : StatusCode(503, result);
    }
}
