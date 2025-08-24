using Microsoft.Extensions.Localization;

namespace BffGateway.WebApi.Services;

/// <summary>
/// Implementation of message service for accessing localized messages
/// </summary>
public class MessageService : BffGateway.Application.Abstractions.Services.IMessageService
{
    private readonly IStringLocalizerFactory _localizerFactory;
    private readonly string _assemblyName;

    public MessageService(IStringLocalizerFactory factory)
    {
        _localizerFactory = factory;
        _assemblyName = typeof(MessageService).Assembly.GetName().Name ?? "BffGateway.WebApi";
    }

    /// <summary>
    /// Gets a localized message by key
    /// </summary>
    public string GetMessage(string key)
    {
        var localizer = _localizerFactory.Create("Messages", _assemblyName);
        return localizer[key].Value;
    }

    /// <summary>
    /// Gets a localized message by key with formatting arguments
    /// </summary>
    public string GetMessage(string key, params object[] args)
    {
        var localizer = _localizerFactory.Create("Messages", _assemblyName);
        return localizer[key, args].Value;
    }
}


