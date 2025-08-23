using BffGateway.Application.DTOs.Payment;
using BffGateway.Infrastructure.Providers.MockProvider.DTOs;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace BffGateway.Infrastructure.Providers.MockProvider.Payments;

public class MockProviderPaymentClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MockProviderPaymentClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public MockProviderPaymentClient(HttpClient httpClient, ILogger<MockProviderPaymentClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<ProviderPaymentResponse> ProcessPaymentAsync(
    ProviderPaymentRequest request,
    CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calling MockProvider payment for amount: {Total} {Curr} to {Dest}",
            request.Total, request.Curr, request.Dest);

        try
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var sw = Stopwatch.StartNew();
            var response = await _httpClient.PostAsync("/api/pay", content, cancellationToken);
            sw.Stop();

            _logger.LogInformation("MockProvider call {Path} ended with {StatusCode} in {ElapsedMs}ms",
                "/api/pay", (int)response.StatusCode, sw.ElapsedMilliseconds);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("MockProvider payment failed for amount: {Total} {Curr}", request.Total, request.Curr);
                return new ProviderPaymentResponse(false, string.Empty, string.Empty, DateTime.MinValue);
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var providerResponse = JsonSerializer.Deserialize<MockProviderPaymentResponseDto>(responseJson, _jsonOptions);

            _logger.LogInformation("MockProvider payment successful for amount: {Total} {Curr}",
                request.Total, request.Curr);

            return new ProviderPaymentResponse(
                providerResponse?.Success ?? false,
                providerResponse?.TransactionId ?? string.Empty,
                providerResponse?.ProviderRef ?? string.Empty,
                providerResponse?.ProcessedAt ?? DateTime.MinValue
            );
        }
        catch (BrokenCircuitException bce)
        {
            _logger.LogWarning(bce, "Circuit breaker open for MockProvider {Path}", "/api/pay");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during MockProvider payment for amount: {Total} {Curr}",
                request.Total, request.Curr);
            return new ProviderPaymentResponse(false, string.Empty, string.Empty, DateTime.MinValue);
        }
    }

    // Additional MockProvider-specific payment endpoints
    // public async Task<ProviderPaymentResponse> RefundPaymentAsync(...)
    // public async Task<ProviderPaymentResponse> GetPaymentStatusAsync(...)
    // public async Task<ProviderPaymentResponse> CancelPaymentAsync(...)
    // public async Task<ProviderPaymentResponse> GetPaymentHistoryAsync(...)
}
