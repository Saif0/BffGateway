using FluentValidation;

namespace BffGateway.Application.Commands.Payments.CreatePayment;

public class CreatePaymentCommandValidator : AbstractValidator<CreatePaymentCommand>
{
    private static readonly string[] SupportedCurrencies = { "USD", "EUR", "GBP", "CAD", "AUD" };

    public CreatePaymentCommandValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero")
            .LessThanOrEqualTo(1000000)
            .WithMessage("Amount must not exceed 1,000,000");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("Currency is required")
            .Must(BeASupportedCurrency)
            .WithMessage($"Currency must be one of: {string.Join(", ", SupportedCurrencies)}");

        RuleFor(x => x.DestinationAccount)
            .NotEmpty()
            .WithMessage("Destination account is required")
            .MaximumLength(50)
            .WithMessage("Destination account must not exceed 50 characters");
    }

    private static bool BeASupportedCurrency(string currency)
    {
        return SupportedCurrencies.Contains(currency?.ToUpperInvariant());
    }
}


