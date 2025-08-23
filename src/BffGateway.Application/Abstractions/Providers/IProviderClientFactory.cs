using System;

namespace BffGateway.Application.Abstractions.Providers;

public interface IProviderClientFactory
{
    /// <summary>
    /// Returns an <see cref="IProviderClient"/> for the specified provider key.
    /// If the key is null or empty, the default provider is returned.
    /// </summary>
    /// <param name="providerKey">A provider name/key registered in DI.</param>
    /// <returns>An <see cref="IProviderClient"/> instance.</returns>
    IProviderClient GetClient(string providerKey = "DefaultProvider");
}


