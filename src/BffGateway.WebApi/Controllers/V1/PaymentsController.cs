using BffGateway.Application.Payments.Commands;
using BffGateway.WebApi.Models.V1;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BffGateway.WebApi.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/payments")]
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
    public async Task<ActionResult<CreatePaymentResponseV1>> CreatePayment([FromBody] CreatePaymentRequestV1 request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Payment request received for amount: {Amount} {Currency} to {DestinationAccount}",
            request.Amount, request.Currency, request.DestinationAccount);

        var command = new CreatePaymentCommand(request.Amount, request.Currency, request.DestinationAccount);
        var result = await _mediator.Send(command, cancellationToken);

        var response = new CreatePaymentResponseV1
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
