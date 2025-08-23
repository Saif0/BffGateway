using BffGateway.Application.Commands.Payments.CreatePayment;
using BffGateway.Application.Common.Enums;
using MediatR;

namespace BffGateway.Application.Commands.Payments.CreatePayment;

public record CreatePaymentCommand(decimal Amount, string Currency, string DestinationAccount, SimulationScenario Scenario = SimulationScenario.None)
    : IRequest<CreatePaymentResponseDTO>;


