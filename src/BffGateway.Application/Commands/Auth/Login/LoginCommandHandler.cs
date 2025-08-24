using BffGateway.Application.Abstractions.Providers;
using BffGateway.Application.Common.DTOs.Auth;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BffGateway.Application.Commands.Auth.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponseDto>
{
    private readonly IProviderClient _providerClient;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(IProviderClient providerClient, ILogger<LoginCommandHandler> logger)
    {
        _providerClient = providerClient;
        _logger = logger;
    }

    public async Task<LoginResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing login request for username: {Username}", request.Username);

        try
        {
            var providerRequest = new ProviderAuthRequest(request.Username, request.Password);
            var providerResponse = await _providerClient.AuthenticateAsync(providerRequest, request.Scenario, cancellationToken);

            var response = new LoginResponseDto(
                providerResponse.Success,
                providerResponse.Success ? providerResponse.Token : null,
                providerResponse.Success ? providerResponse.ExpiresAt : null,
                providerResponse.Success ? "Login successful" : "Login failed",
                providerResponse.StatusCode
            );

            if (response.IsSuccess)
            {
                _logger.LogInformation("Login request processed successfully for username: {Username}, Success: {Success}",
                    request.Username, response.IsSuccess);
            }
            else
            {
                _logger.LogWarning("Login request failed for username: {Username}, StatusCode: {StatusCode}",
                    request.Username, response.UpstreamStatusCode);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing login request for username: {Username}", request.Username);
            return new LoginResponseDto(false, null, null, "Internal server error", 500);
        }
    }
}


