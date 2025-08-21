using BffGateway.Application.Commands.Auth.Login;
using BffGateway.WebApi.Models.V2;
using MediatR;
using Microsoft.AspNetCore.Mvc;

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

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseV2>> Login([FromBody] LoginRequestV2 request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Login request (v2) received for username: {Username}", request.Username);

        var command = new LoginCommand(request.Username, request.Password);
        var result = await _mediator.Send(command, cancellationToken);

        var response = new LoginResponseV2
        {
            Success = result.IsSuccess,
            Token = new TokenInfo
            {
                AccessToken = result.Jwt ?? string.Empty,
                ExpiresAt = result.ExpiresAt,
                TokenType = "Bearer"
            },
            User = result.IsSuccess ? new UserInfo { Username = request.Username } : null
        };

        if (result.IsSuccess)
        {
            _logger.LogInformation("Login (v2) successful for username: {Username}", request.Username);
            return Ok(response);
        }
        else
        {
            _logger.LogWarning("Login (v2) failed for username: {Username}", request.Username);
            return BadRequest(response);
        }
    }
}
