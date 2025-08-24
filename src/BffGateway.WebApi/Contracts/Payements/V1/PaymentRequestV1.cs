using System.ComponentModel.DataAnnotations;

namespace BffGateway.WebApi.Contracts.Payements.V1;

public record CreatePaymentRequestV1(
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero")]
    decimal Amount,
    [Required] string Currency,
    [Required] string DestinationAccount
);
