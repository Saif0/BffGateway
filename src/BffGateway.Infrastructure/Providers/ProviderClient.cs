using System.Text;
using System.Text.Json;
using System.Diagnostics;
using Polly.CircuitBreaker;
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
        _logger.LogInformation("Calling provider authentication");

        try
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var sw = Stopwatch.StartNew();
            var response = await _httpClient.PostAsync("/api/authenticate", content, cancellationToken);
            sw.Stop();

            _logger.LogInformation("Provider call {Path} ended with {StatusCode} in {ElapsedMs}ms",
                "/api/authenticate", (int)response.StatusCode, sw.ElapsedMilliseconds);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var providerResponse = JsonSerializer.Deserialize<ProviderAuthResponseDto>(responseJson, _jsonOptions);

                return new ProviderAuthResponse(
                    providerResponse?.Success ?? false,
                    providerResponse?.Token ?? string.Empty,
                    providerResponse?.ExpiresAt ?? DateTime.MinValue
                );
            }
            else
            {
                _logger.LogWarning("Provider authentication failed with status {StatusCode}",
                    response.StatusCode);
                return new ProviderAuthResponse(false, string.Empty, DateTime.MinValue);
            }
        }
        catch (BrokenCircuitException bce)
        {
            _logger.LogWarning(bce, "Circuit breaker open for {Path}", "/api/authenticate");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during provider authentication");
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

            var sw = Stopwatch.StartNew();
            var response = await _httpClient.PostAsync("/api/pay", content, cancellationToken);
            sw.Stop();

            _logger.LogInformation("Provider call {Path} ended with {StatusCode} in {ElapsedMs}ms",
                "/api/pay", (int)response.StatusCode, sw.ElapsedMilliseconds);

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
        catch (BrokenCircuitException bce)
        {
            _logger.LogWarning(bce, "Circuit breaker open for {Path}", "/api/pay");
            throw;
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
            var response = await _httpClient.GetAsync("/api/ping", cancellationToken);

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
