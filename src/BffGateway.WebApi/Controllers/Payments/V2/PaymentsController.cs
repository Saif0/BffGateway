using BffGateway.Application.Commands.Payments.CreatePayment;
using BffGateway.Application.Common.Enums;
using BffGateway.WebApi.Contracts.Payements.V2;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;

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

    /// <summary>
    /// Create a new payment transaction
    /// </summary>
    /// <param name="request">Payment details</param>
    /// <param name="scenario">Simulation scenario for testing different behaviors</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment processing result</returns>
    [HttpPost]
    public async Task<ActionResult<CreatePaymentResponseV2>> CreatePayment([FromBody] CreatePaymentRequestV2 request, [FromQuery] SimulationScenario scenario = SimulationScenario.None, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Payment request received for amount: {Amount} {Currency} to {DestinationAccount} with scenario: {Scenario}",
            request.Amount, request.Currency, request.DestinationAccount, scenario);

        var command = new CreatePaymentCommand(request.Amount, request.Currency, request.DestinationAccount, scenario);
        var result = await _mediator.Send(command, cancellationToken);

        var response = new CreatePaymentResponseV2(
            result.IsSuccess,
            result.IsSuccess ? "Payment processed successfully" : "Payment failed",
            result.PaymentId,
            result.ProviderReference,
            result.ProcessedAt
        );

        if (result.IsSuccess)
        {
            _logger.LogInformation("Payment successful for amount: {Amount} {Currency}", request.Amount, request.Currency);
            return Ok(response);
        }
        else
        {
            var status = result.UpstreamStatusCode;
            _logger.LogWarning("Payment failed for amount: {Amount} {Currency} with upstream status: {Status}", request.Amount, request.Currency, status);

            if (status == (int)HttpStatusCode.TooManyRequests)
                return StatusCode((int)HttpStatusCode.TooManyRequests, response);
            if (status == (int)HttpStatusCode.RequestTimeout)
                return StatusCode((int)HttpStatusCode.GatewayTimeout, response);
            if (status >= 500)
                return StatusCode((int)HttpStatusCode.BadGateway, response);

            return BadRequest(response);
        }
    }
}
