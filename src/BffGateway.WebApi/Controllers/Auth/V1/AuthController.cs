using BffGateway.Application.Commands.Auth.Login;
using BffGateway.Application.Common.Enums;
using BffGateway.WebApi.Contracts.V1;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using BffGateway.WebApi.Extensions;

namespace BffGateway.WebApi.Controllers.V1;

[ApiController]
[ApiVersion("1.0", Deprecated = true)]
[Route("v{version:apiVersion}/auth")]
[ApiExplorerSettings(GroupName = "v1")]
[Obsolete("v1 is deprecated; use v2")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IMediator mediator, ILogger<AuthController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseV1>> Login([FromBody] LoginRequestV1 request, [FromQuery] SimulationScenario scenario = SimulationScenario.None, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Login request received for username: {Username} with scenario: {Scenario}", request.Username, scenario);

        var command = new LoginCommand(request.Username, request.Password, scenario);
        var result = await _mediator.Send(command, cancellationToken);

        var response = new LoginResponseV1(
            result.IsSuccess,
            result.Message,
            result.Jwt,
            result.ExpiresAt
        );

        if (result.IsSuccess)
        {
            _logger.LogInformation("Login successful for username: {Username}", request.Username);
            return Ok(response);
        }
        else
        {
            var status = result.UpstreamStatusCode;
            _logger.LogWarning("Login failed for username: {Username} with upstream status: {Status}", request.Username, status);

            return this.MapUpstreamStatusCode(response, status);
        }
    }
}
