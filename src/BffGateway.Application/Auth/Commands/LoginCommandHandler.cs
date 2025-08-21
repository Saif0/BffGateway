using BffGateway.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BffGateway.Application.Auth.Commands;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IProviderClient _providerClient;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(IProviderClient providerClient, ILogger<LoginCommandHandler> logger)
    {
        _providerClient = providerClient;
        _logger = logger;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing login request for username: {Username}", request.Username);

        try
        {
            var providerRequest = new ProviderAuthRequest(request.Username, request.Password);
            var providerResponse = await _providerClient.AuthenticateAsync(providerRequest, cancellationToken);

            var response = new LoginResponse(
                providerResponse.Success,
                providerResponse.Success ? providerResponse.Token : null,
                providerResponse.Success ? providerResponse.ExpiresAt : null
            );

            _logger.LogInformation("Login request processed successfully for username: {Username}, Success: {Success}",
                request.Username, response.IsSuccess);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing login request for username: {Username}", request.Username);
            return new LoginResponse(false, null, null);
        }
    }
}
