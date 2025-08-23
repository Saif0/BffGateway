using BffGateway.Application.Common.DTOs.Auth;
using BffGateway.Application.Common.DTOs.Payment;
using BffGateway.Application.Common.Enums;

namespace BffGateway.Application.Abstractions.Providers;

public interface IProviderClient
{
    Task<ProviderAuthResponse> AuthenticateAsync(ProviderAuthRequest request, SimulationScenario scenario = SimulationScenario.None, CancellationToken cancellationToken = default);
    Task<ProviderPaymentResponse> ProcessPaymentAsync(ProviderPaymentRequest request, SimulationScenario scenario = SimulationScenario.None, CancellationToken cancellationToken = default);
    Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default);
}


