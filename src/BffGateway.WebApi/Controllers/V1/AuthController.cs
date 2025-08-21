using BffGateway.Application.Auth.Commands;
using BffGateway.WebApi.Models.V1;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BffGateway.WebApi.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/auth")]
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
    public async Task<ActionResult<LoginResponseV1>> Login([FromBody] LoginRequestV1 request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Login request received for username: {Username}", request.Username);

        var command = new LoginCommand(request.Username, request.Password);
        var result = await _mediator.Send(command, cancellationToken);

        var response = new LoginResponseV1
        {
            IsSuccess = result.IsSuccess,
            Jwt = result.Jwt,
            ExpiresAt = result.ExpiresAt?.ToString("O") // ISO 8601 format
        };

        if (result.IsSuccess)
        {
            _logger.LogInformation("Login successful for username: {Username}", request.Username);
            return Ok(response);
        }
        else
        {
            _logger.LogWarning("Login failed for username: {Username}", request.Username);
            return BadRequest(response);
        }
    }
}
