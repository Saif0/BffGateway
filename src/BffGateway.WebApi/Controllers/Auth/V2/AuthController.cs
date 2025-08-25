using BffGateway.Application.Commands.Auth.Login;
using BffGateway.Application.Common.Enums;
using BffGateway.WebApi.Contracts.V2;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using BffGateway.WebApi.Extensions;

namespace BffGateway.WebApi.Controllers.V2;

[ApiController]
[ApiVersion("2.0")]
[Route("v{version:apiVersion}/auth")]
[ApiExplorerSettings(GroupName = "v2")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IMediator mediator, ILogger<AuthController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Authenticate a user and return a JWT token
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <param name="scenario">Simulation scenario for testing different behaviors</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication result with JWT token</returns>
    [HttpPost("login")]
    [MapToApiVersion("2.0")]
    public async Task<ActionResult<LoginResponseV2>> Login([FromBody] LoginRequestV2 request, [FromQuery] SimulationScenario scenario = SimulationScenario.None, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Login request (v2) received for username: {Username} with scenario: {Scenario}", request.Username, scenario);

        var command = new LoginCommand(request.Username, request.Password, scenario);
        var result = await _mediator.Send(command, cancellationToken);

        var response = new LoginResponseV2(
            result.IsSuccess,
            result.Message,
            new TokenInfo(
                result.Jwt ?? string.Empty,
                result.ExpiresAt,
                "Bearer"
            ),
            result.IsSuccess ? new UserInfo(request.Username) : null
        );

        if (result.IsSuccess)
        {
            _logger.LogInformation("Login (v2) successful for username: {Username}", request.Username);
            return Ok(response);
        }
        else
        {
            var status = result.UpstreamStatusCode;
            _logger.LogWarning("Login (v2) failed for username: {Username} with upstream status: {Status}", request.Username, status);

            return this.MapUpstreamStatusCode(response, status);
        }
    }
}
