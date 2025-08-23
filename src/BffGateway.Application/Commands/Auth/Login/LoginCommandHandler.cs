using BffGateway.Application.Abstractions.Providers;
using BffGateway.Application.Common.DTOs.Auth;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BffGateway.Application.Commands.Auth.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponseDTO>
{
    private readonly IProviderClient _providerClient;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(IProviderClient providerClient, ILogger<LoginCommandHandler> logger)
    {
        _providerClient = providerClient;
        _logger = logger;
    }

    public async Task<LoginResponseDTO> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing login request for username: {Username}", request.Username);

        try
        {
            var providerRequest = new ProviderAuthRequest(request.Username, request.Password);
            var providerResponse = await _providerClient.AuthenticateAsync(providerRequest, request.Scenario, cancellationToken);

            var response = new LoginResponseDTO(
                providerResponse.Success,
                providerResponse.Success ? providerResponse.Token : null,
                providerResponse.Success ? providerResponse.ExpiresAt : null,
                providerResponse.StatusCode
            );

            _logger.LogInformation("Login request processed successfully for username: {Username}, Success: {Success}",
                request.Username, response.IsSuccess);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing login request for username: {Username}", request.Username);
            return new LoginResponseDTO(false, null, null, 500);
        }
    }
}


