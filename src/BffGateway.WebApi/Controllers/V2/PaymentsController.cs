using BffGateway.Application.Commands.Payments.CreatePayment;
using BffGateway.WebApi.Models.V2;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System;

namespace BffGateway.WebApi.Controllers.V2;

[ApiController]
[ApiVersion("2.0")]
[Route("v{version:apiVersion}/payments")]
[ApiExplorerSettings(GroupName = "v2")]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IMediator mediator, ILogger<PaymentsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<CreatePaymentResponseV2>> CreatePayment([FromBody] CreatePaymentRequestV2 request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Payment request received for amount: {Amount} {Currency} to {DestinationAccount}",
            request.Amount, request.Currency, request.DestinationAccount);

        var command = new CreatePaymentCommand(request.Amount, request.Currency, request.DestinationAccount);
        var result = await _mediator.Send(command, cancellationToken);

        var response = new CreatePaymentResponseV2
        {
            IsSuccess = result.IsSuccess,
            PaymentId = result.PaymentId,
            ProviderReference = result.ProviderReference,
            ProcessedAt = result.ProcessedAt?.ToString("O") // ISO 8601 format
        };

        if (result.IsSuccess)
        {
            _logger.LogInformation("Payment successful for amount: {Amount} {Currency}", request.Amount, request.Currency);
            return Ok(response);
        }
        else
        {
            _logger.LogWarning("Payment failed for amount: {Amount} {Currency}", request.Amount, request.Currency);
            return BadRequest(response);
        }
    }
}
