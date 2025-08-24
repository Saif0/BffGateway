using FluentValidation;
using BffGateway.Application.Common.Validators;
using BffGateway.Application.Abstractions.Services;
using BffGateway.Application.Constants;

namespace BffGateway.Application.Commands.Auth.Login;

public class LoginCommandValidator : LocalizedValidatorBase<LoginCommand>
{
    public LoginCommandValidator() : base()
    {
        ConfigureRules();
    }

    public LoginCommandValidator(IMessageService messageService) : base(messageService)
    {
        ConfigureRules();
    }

    private void ConfigureRules()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .WithMessage(GetLocalizedMessage(MessageKeys.Validation.UsernameRequired))
            .MaximumLength(100)
            .WithMessage(GetLocalizedMessage(MessageKeys.Validation.UsernameMaxLength));

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage(GetLocalizedMessage(MessageKeys.Validation.PasswordRequired))
            .MinimumLength(1)
            .WithMessage(GetLocalizedMessage(MessageKeys.Validation.PasswordRequired));
    }
}


