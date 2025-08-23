namespace BffGateway.Infrastructure.Providers.StripeProvider.DTOs;

// Stripe-specific DTOs that might have different structure
internal record StripePaymentIntentResponseDto(string Id, string Status, long Amount, string Currency, DateTime Created);
internal record StripeChargeResponseDto(string Id, bool Paid, string ReceiptUrl, DateTime Created);
