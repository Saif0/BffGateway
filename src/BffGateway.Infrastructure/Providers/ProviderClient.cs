using System.Text;
using System.Text.Json;
using BffGateway.Application.Abstractions.Providers;
using BffGateway.Application.Common.DTOs;
using Microsoft.Extensions.Logging;

namespace BffGateway.Infrastructure.Providers;

public class ProviderClient : IProviderClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProviderClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ProviderClient(HttpClient httpClient, ILogger<ProviderClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<ProviderAuthResponse> AuthenticateAsync(ProviderAuthRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calling provider authentication for user: {User}", request.User);

        try
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/authenticate", content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var providerResponse = JsonSerializer.Deserialize<ProviderAuthResponseDto>(responseJson, _jsonOptions);

                _logger.LogInformation("Provider authentication successful for user: {User}", request.User);

                return new ProviderAuthResponse(
                    providerResponse?.Success ?? false,
                    providerResponse?.Token ?? string.Empty,
                    providerResponse?.ExpiresAt ?? DateTime.MinValue
                );
            }
            else
            {
                _logger.LogWarning("Provider authentication failed for user: {User}, Status: {StatusCode}",
                    request.User, response.StatusCode);
                return new ProviderAuthResponse(false, string.Empty, DateTime.MinValue);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during provider authentication for user: {User}", request.User);
            return new ProviderAuthResponse(false, string.Empty, DateTime.MinValue);
        }
    }

    public async Task<ProviderPaymentResponse> ProcessPaymentAsync(ProviderPaymentRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calling provider payment for amount: {Total} {Curr} to {Dest}",
            request.Total, request.Curr, request.Dest);

        try
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/pay", content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var providerResponse = JsonSerializer.Deserialize<ProviderPaymentResponseDto>(responseJson, _jsonOptions);

                _logger.LogInformation("Provider payment successful for amount: {Total} {Curr}",
                    request.Total, request.Curr);

                return new ProviderPaymentResponse(
                    providerResponse?.Success ?? false,
                    providerResponse?.TransactionId ?? string.Empty,
                    providerResponse?.ProviderRef ?? string.Empty,
                    providerResponse?.ProcessedAt ?? DateTime.MinValue
                );
            }
            else
            {
                _logger.LogWarning("Provider payment failed for amount: {Total} {Curr}, Status: {StatusCode}",
                    request.Total, request.Curr, response.StatusCode);
                return new ProviderPaymentResponse(false, string.Empty, string.Empty, DateTime.MinValue);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during provider payment for amount: {Total} {Curr}",
                request.Total, request.Curr);
            return new ProviderPaymentResponse(false, string.Empty, string.Empty, DateTime.MinValue);
        }
    }

    public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Performing provider health check");

            // Use a lightweight request to check provider health
            var response = await _httpClient.GetAsync("/api/authenticate", cancellationToken);

            var isHealthy = response.StatusCode != System.Net.HttpStatusCode.ServiceUnavailable;

            _logger.LogDebug("Provider health check result: {IsHealthy}", isHealthy);

            return isHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Provider health check failed");
            return false;
        }
    }
}

internal record ProviderAuthResponseDto(bool Success, string Token, DateTime ExpiresAt);
internal record ProviderPaymentResponseDto(bool Success, string TransactionId, string ProviderRef, DateTime ProcessedAt);
