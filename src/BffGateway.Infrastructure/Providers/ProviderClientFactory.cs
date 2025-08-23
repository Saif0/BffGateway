using System;
using BffGateway.Application.Abstractions.Providers;
using BffGateway.Infrastructure.Providers.MockProvider;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace BffGateway.Infrastructure.Providers;

public class ProviderClientFactory : IProviderClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;

    public ProviderClientFactory(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory)
    {
        _httpClientFactory = httpClientFactory;
        _loggerFactory = loggerFactory;
    }

    public IProviderClient GetClient(string providerKey = "MockProvider")
    {
        if (string.IsNullOrWhiteSpace(providerKey))
        {
            providerKey = "MockProvider";
        }

        var httpClient = _httpClientFactory.CreateClient(providerKey);

        return providerKey.ToLowerInvariant() switch
        {
            "mockprovider" => new MockProviderClient(httpClient, _loggerFactory),
            // "stripeprovider" => new StripeProviderClient(httpClient, _loggerFactory),
            // "paypalprovider" => new PayPalProviderClient(httpClient, _loggerFactory),
            _ => throw new NotSupportedException($"Provider '{providerKey}' is not supported. Supported providers: MockProvider")
        };
    }
}


