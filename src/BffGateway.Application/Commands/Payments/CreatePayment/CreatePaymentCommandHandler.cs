using BffGateway.Application.Abstractions.Providers;
using BffGateway.Application.Common.DTOs.Payment;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BffGateway.Application.Commands.Payments.CreatePayment;

public class CreatePaymentCommandHandler : IRequestHandler<CreatePaymentCommand, CreatePaymentResponseDTO>
{
    private readonly IProviderClient _providerClient;
    private readonly ILogger<CreatePaymentCommandHandler> _logger;

    public CreatePaymentCommandHandler(IProviderClient providerClient, ILogger<CreatePaymentCommandHandler> logger)
    {
        _providerClient = providerClient;
        _logger = logger;
    }

    public async Task<CreatePaymentResponseDTO> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing payment request for amount: {Amount} {Currency} to {DestinationAccount}",
            request.Amount, request.Currency, request.DestinationAccount);

        try
        {
            var providerRequest = new ProviderPaymentRequest(request.Amount, request.Currency, request.DestinationAccount);
            var providerResponse = await _providerClient.ProcessPaymentAsync(providerRequest, request.Scenario, cancellationToken);

            var response = new CreatePaymentResponseDTO(
                providerResponse.Success,
                providerResponse.Success ? providerResponse.TransactionId : null,
                providerResponse.Success ? providerResponse.ProviderRef : null,
                providerResponse.Success ? providerResponse.ProcessedAt : null,
                providerResponse.Success ? "Payment processed successfully" : "Payment failed",
                providerResponse.StatusCode
            );

            _logger.LogInformation("Payment request processed successfully for amount: {Amount} {Currency}, Success: {Success}",
                request.Amount, request.Currency, response.IsSuccess);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment request for amount: {Amount} {Currency}",
                request.Amount, request.Currency);
            return new CreatePaymentResponseDTO(false, null, null, null, "Internal server error", 500);
        }
    }
}


