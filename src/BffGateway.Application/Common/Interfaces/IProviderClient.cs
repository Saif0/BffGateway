namespace BffGateway.Application.Common.Interfaces;

public interface IProviderClient
{
    Task<ProviderAuthResponse> AuthenticateAsync(ProviderAuthRequest request, CancellationToken cancellationToken = default);
    Task<ProviderPaymentResponse> ProcessPaymentAsync(ProviderPaymentRequest request, CancellationToken cancellationToken = default);
    Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default);
}

public record ProviderAuthRequest(string User, string Pwd);

public record ProviderAuthResponse(bool Success, string Token, DateTime ExpiresAt);

public record ProviderPaymentRequest(decimal Total, string Curr, string Dest);

public record ProviderPaymentResponse(bool Success, string TransactionId, string ProviderRef, DateTime ProcessedAt);
