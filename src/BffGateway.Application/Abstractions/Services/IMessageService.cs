namespace BffGateway.Application.Abstractions.Services;

/// <summary>
/// Service for accessing localized messages
/// </summary>
public interface IMessageService
{
    /// <summary>
    /// Gets a localized message by key
    /// </summary>
    /// <param name="key">The message key</param>
    /// <returns>The localized message</returns>
    string GetMessage(string key);

    /// <summary>
    /// Gets a localized message by key with formatting arguments
    /// </summary>
    /// <param name="key">The message key</param>
    /// <param name="args">Formatting arguments</param>
    /// <returns>The formatted localized message</returns>
    string GetMessage(string key, params object[] args);
}
