using BffGateway.Application.Commands.Payments.CreatePayment;
using MediatR;

namespace BffGateway.Application.Commands.Payments.CreatePayment;

public record CreatePaymentCommand(decimal Amount, string Currency, string DestinationAccount)
    : IRequest<CreatePaymentResponseDTO>;


