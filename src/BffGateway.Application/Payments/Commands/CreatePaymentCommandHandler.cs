using BffGateway.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BffGateway.Application.Payments.Commands;

public class CreatePaymentCommandHandler : IRequestHandler<CreatePaymentCommand, CreatePaymentResponse>
{
    private readonly IProviderClient _providerClient;
    private readonly ILogger<CreatePaymentCommandHandler> _logger;

    public CreatePaymentCommandHandler(IProviderClient providerClient, ILogger<CreatePaymentCommandHandler> logger)
    {
        _providerClient = providerClient;
        _logger = logger;
    }

    public async Task<CreatePaymentResponse> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing payment request for amount: {Amount} {Currency} to {DestinationAccount}",
            request.Amount, request.Currency, request.DestinationAccount);

        try
        {
            var providerRequest = new ProviderPaymentRequest(request.Amount, request.Currency, request.DestinationAccount);
            var providerResponse = await _providerClient.ProcessPaymentAsync(providerRequest, cancellationToken);

            var response = new CreatePaymentResponse(
                providerResponse.Success,
                providerResponse.Success ? providerResponse.TransactionId : null,
                providerResponse.Success ? providerResponse.ProviderRef : null,
                providerResponse.Success ? providerResponse.ProcessedAt : null
            );

            _logger.LogInformation("Payment request processed successfully for amount: {Amount} {Currency}, Success: {Success}",
                request.Amount, request.Currency, response.IsSuccess);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment request for amount: {Amount} {Currency}",
                request.Amount, request.Currency);
            return new CreatePaymentResponse(false, null, null, null);
        }
    }
}
