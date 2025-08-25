using BffGateway.Application.Commands.Payments.CreatePayment;
using BffGateway.Application.Common.Enums;
using BffGateway.WebApi.Contracts.Payements.V1;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using BffGateway.WebApi.Extensions;

namespace BffGateway.WebApi.Controllers.V1;

[ApiController]
[ApiVersion("1.0", Deprecated = true)]
[Route("v{version:apiVersion}/payments")]
[ApiExplorerSettings(GroupName = "v1")]
[Obsolete("v1 is deprecated; use v2")]
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
    public async Task<ActionResult<CreatePaymentResponseV1>> CreatePayment([FromBody] CreatePaymentRequestV1 request, [FromQuery] SimulationScenario scenario = SimulationScenario.None, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Payment request received for amount: {Amount} {Currency} to {DestinationAccount} with scenario: {Scenario}",
            request.Amount, request.Currency, request.DestinationAccount, scenario);

        var command = new CreatePaymentCommand(request.Amount, request.Currency, request.DestinationAccount, scenario);
        var result = await _mediator.Send(command, cancellationToken);

        var response = new CreatePaymentResponseV1(
            result.IsSuccess,
            result.Message,
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

            return this.MapUpstreamStatusCode(response, status);
        }
    }
}
