using FluentValidation;
using BffGateway.Application.Abstractions.Services;

namespace BffGateway.Application.Common.Validators;

/// <summary>
/// Base class for validators that provides localization support
/// </summary>
/// <typeparam name="T">The type being validated</typeparam>
public abstract class LocalizedValidatorBase<T> : AbstractValidator<T>
{
    protected readonly IMessageService? MessageService;

    protected LocalizedValidatorBase()
    {
        // MessageService will be null when the DI container hasn't been fully set up
        // In such cases, we'll fall back to default English messages
    }

    protected LocalizedValidatorBase(IMessageService messageService)
    {
        MessageService = messageService;
    }

    /// <summary>
    /// Creates a validation function that resolves the localized message at validation time
    /// </summary>
    /// <param name="key">The message key</param>
    /// <param name="defaultMessage">The default message to use if localization service is not available</param>
    /// <returns>A function that returns the localized message</returns>
    protected Func<T, string> GetLocalizedMessage(string key, string defaultMessage)
    {
        return (model) => MessageService?.GetMessage(key) ?? defaultMessage;
    }

    /// <summary>
    /// Creates a validation function that resolves the localized message with formatting at validation time
    /// </summary>
    /// <param name="key">The message key</param>
    /// <param name="defaultMessage">The default message to use if localization service is not available</param>
    /// <param name="args">Formatting arguments</param>
    /// <returns>A function that returns the formatted localized message</returns>
    protected Func<T, string> GetLocalizedMessage(string key, string defaultMessage, params object[] args)
    {
        return (model) =>
        {
            if (MessageService != null)
            {
                return MessageService.GetMessage(key, args);
            }

            return string.Format(defaultMessage, args);
        };
    }

    /// <summary>
    /// Creates a validation function that resolves the localized message at validation time (no hardcoded fallback)
    /// </summary>
    /// <param name="key">The message key</param>
    /// <returns>A function that returns the localized message</returns>
    protected Func<T, string> GetLocalizedMessage(string key)
    {
        return (model) => MessageService?.GetMessage(key) ?? key;
    }

    /// <summary>
    /// Creates a validation function that resolves the localized formatted message at validation time (no hardcoded fallback)
    /// </summary>
    /// <param name="key">The message key</param>
    /// <param name="args">Formatting arguments</param>
    /// <returns>A function that returns the formatted localized message</returns>
    protected Func<T, string> GetLocalizedMessageWithArgs(string key, params object[] args)
    {
        return (model) => MessageService?.GetMessage(key, args) ?? key;
    }
}
