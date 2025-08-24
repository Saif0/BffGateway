using System.ComponentModel.DataAnnotations;

namespace BffGateway.WebApi.Contracts.Payements.V2;

public record CreatePaymentRequestV2(
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero")]
    decimal Amount,
    [Required] string Currency,
    [Required] string DestinationAccount
);