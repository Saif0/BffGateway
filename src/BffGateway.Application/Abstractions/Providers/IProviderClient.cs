using BffGateway.Application.DTOs.Auth;
using BffGateway.Application.DTOs.Payment;

namespace BffGateway.Application.Abstractions.Providers;

public interface IProviderClient
{
    Task<ProviderAuthResponse> AuthenticateAsync(ProviderAuthRequest request, CancellationToken cancellationToken = default);
    Task<ProviderPaymentResponse> ProcessPaymentAsync(ProviderPaymentRequest request, CancellationToken cancellationToken = default);
    Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default);
}


