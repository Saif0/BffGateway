using FluentValidation;
using BffGateway.Application.Common.Validators;
using BffGateway.Application.Abstractions.Services;
using BffGateway.Application.Constants;

namespace BffGateway.Application.Commands.Payments.CreatePayment;

public class CreatePaymentCommandValidator : LocalizedValidatorBase<CreatePaymentCommand>
{
    private static readonly string[] SupportedCurrencies = { "USD", "EUR", "GBP", "CAD", "AUD" };

    public CreatePaymentCommandValidator() : base()
    {
        ConfigureRules();
    }

    public CreatePaymentCommandValidator(IMessageService messageService) : base(messageService)
    {
        ConfigureRules();
    }

    private void ConfigureRules()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage(GetLocalizedMessage(MessageKeys.Validation.AmountGreaterThanZero))
            .LessThanOrEqualTo(1000000)
            .WithMessage(GetLocalizedMessage(MessageKeys.Validation.AmountMaxValue));

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage(GetLocalizedMessage(MessageKeys.Validation.CurrencyRequired))
            .Must(BeASupportedCurrency)
            .WithMessage(GetLocalizedMessageWithArgs(MessageKeys.Validation.CurrencyInvalid, string.Join(", ", SupportedCurrencies)));

        RuleFor(x => x.DestinationAccount)
            .NotEmpty()
            .WithMessage(GetLocalizedMessage(MessageKeys.Validation.DestinationAccountRequired))
            .MaximumLength(50)
            .WithMessage(GetLocalizedMessage(MessageKeys.Validation.DestinationAccountMaxLength));
    }

    private static bool BeASupportedCurrency(string currency)
    {
        return SupportedCurrencies.Contains(currency?.ToUpperInvariant());
    }
}


