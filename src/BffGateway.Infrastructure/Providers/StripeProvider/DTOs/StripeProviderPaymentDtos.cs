namespace BffGateway.Infrastructure.Providers.StripeProvider.DTOs;

// Stripe-specific DTOs that might have different structure
internal sealed record StripePaymentIntentResponseDto(string Id, string Status, long Amount, string Currency, DateTime Created);
internal sealed record StripeChargeResponseDto(string Id, bool Paid, string ReceiptUrl, DateTime Created);
