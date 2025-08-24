using BffGateway.Application.Abstractions.Providers;
using BffGateway.Application.Common.DTOs.Payment;
using MediatR;
using BffGateway.Application.Abstractions.Services;
using Microsoft.Extensions.Logging;
using BffGateway.Application.Constants;

namespace BffGateway.Application.Commands.Payments.CreatePayment;

public class CreatePaymentCommandHandler : IRequestHandler<CreatePaymentCommand, CreatePaymentResponseDTO>
{
    private readonly IProviderClient _providerClient;
    private readonly ILogger<CreatePaymentCommandHandler> _logger;
    private readonly IMessageService _messageService;

    public CreatePaymentCommandHandler(IProviderClient providerClient, ILogger<CreatePaymentCommandHandler> logger, IMessageService messageService)
    {
        _providerClient = providerClient;
        _logger = logger;
        _messageService = messageService;
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
                providerResponse.Success ? _messageService.GetMessage(MessageKeys.Payments.PaymentSuccess) : _messageService.GetMessage(MessageKeys.Payments.PaymentFailed),
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
            return new CreatePaymentResponseDTO(false, null, null, null, _messageService.GetMessage(MessageKeys.Errors.InternalServerError), 500);
        }
    }
}


