using System.ComponentModel.DataAnnotations;

namespace BffGateway.WebApi.Models.V2;

public class CreatePaymentRequestV2
{
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero")]
    public decimal Amount { get; set; }

    [Required]
    public string Currency { get; set; } = string.Empty;

    [Required]
    public string DestinationAccount { get; set; } = string.Empty;
}

public class CreatePaymentResponseV2
{
    public bool IsSuccess { get; set; }
    public string? PaymentId { get; set; }
    public string? ProviderReference { get; set; }
    public string? ProcessedAt { get; set; }
}
