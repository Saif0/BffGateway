using BffGateway.Application.Abstractions.Providers;
using BffGateway.Application.Common.DTOs.Auth;
using BffGateway.Application.Common.DTOs.Payment;
using BffGateway.Infrastructure.Providers.MockProvider.Auth;
using BffGateway.Infrastructure.Providers.MockProvider.Health;
using BffGateway.Infrastructure.Providers.MockProvider.Payments;
using Microsoft.Extensions.Logging;

namespace BffGateway.Infrastructure.Providers.MockProvider;

/// <summary>
/// MockProvider-specific implementation of IProviderClient.
/// Aggregates all MockProvider domain clients (Auth, Payment, Health).
/// </summary>
public class MockProviderClient : IProviderClient
{
    private readonly MockProviderAuthClient _authClient;
    private readonly MockProviderPaymentClient _paymentClient;
    private readonly MockProviderHealthClient _healthClient;

    public MockProviderClient(HttpClient httpClient, ILoggerFactory loggerFactory)
    {
        _authClient = new MockProviderAuthClient(httpClient, loggerFactory.CreateLogger<MockProviderAuthClient>());
        _paymentClient = new MockProviderPaymentClient(httpClient, loggerFactory.CreateLogger<MockProviderPaymentClient>());
        _healthClient = new MockProviderHealthClient(httpClient, loggerFactory.CreateLogger<MockProviderHealthClient>());
    }

    public Task<ProviderAuthResponse> AuthenticateAsync(ProviderAuthRequest request, CancellationToken cancellationToken = default)
        => _authClient.AuthenticateAsync(request, cancellationToken);

    public Task<ProviderPaymentResponse> ProcessPaymentAsync(ProviderPaymentRequest request, CancellationToken cancellationToken = default)
        => _paymentClient.ProcessPaymentAsync(request, cancellationToken);

    public Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
        => _healthClient.HealthCheckAsync(cancellationToken);
}
