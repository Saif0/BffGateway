using BffGateway.Application.Common.DTOs.Payment;
using BffGateway.Infrastructure.Providers.StripeProvider.DTOs;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace BffGateway.Infrastructure.Providers.StripeProvider.Payments;

public class StripeProviderPaymentClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<StripeProviderPaymentClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public StripeProviderPaymentClient(HttpClient httpClient, ILogger<StripeProviderPaymentClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase // Keep camelCase for now (can be customized per provider)
        };
    }

    public async Task<ProviderPaymentResponse> ProcessPaymentAsync(
    ProviderPaymentRequest request,
    CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calling Stripe payment for amount: {Total} {Curr}",
            request.Total, request.Curr);

        try
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var sw = Stopwatch.StartNew();
            var response = await _httpClient.PostAsync("/v1/payment_intents", content, cancellationToken);
            sw.Stop();

            _logger.LogInformation("Stripe call {Path} ended with {StatusCode} in {ElapsedMs}ms",
                "/v1/payment_intents", (int)response.StatusCode, sw.ElapsedMilliseconds);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Stripe payment failed for amount: {Total} {Curr}", request.Total, request.Curr);
                return new ProviderPaymentResponse(false, string.Empty, string.Empty, DateTime.MinValue);
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var stripeResponse = JsonSerializer.Deserialize<StripePaymentIntentResponseDto>(responseJson, _jsonOptions);

            // Map Stripe response to common format
            var isSuccessful = stripeResponse?.Status == "succeeded";
            _logger.LogInformation("Stripe payment {Status} for amount: {Total} {Curr}",
                stripeResponse?.Status, request.Total, request.Curr);

            return new ProviderPaymentResponse(
                isSuccessful,
                stripeResponse?.Id ?? string.Empty,
                stripeResponse?.Id ?? string.Empty, // Stripe uses same ID
                stripeResponse?.Created ?? DateTime.MinValue
            );
        }
        catch (BrokenCircuitException bce)
        {
            _logger.LogWarning(bce, "Circuit breaker open for Stripe {Path}", "/v1/payment_intents");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Stripe payment for amount: {Total} {Curr}",
                request.Total, request.Curr);
            return new ProviderPaymentResponse(false, string.Empty, string.Empty, DateTime.MinValue);
        }
    }

    // Stripe-specific payment endpoints
    public async Task<ProviderPaymentResponse> CreatePaymentIntentAsync(
        decimal amount,
        string currency,
        CancellationToken cancellationToken = default)
    {
        // Stripe-specific implementation
        var requestObj = new { amount = (long)(amount * 100), currency }; // Stripe uses cents
        var json = JsonSerializer.Serialize(requestObj, _jsonOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/v1/payment_intents", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new ProviderPaymentResponse(false, "", "", DateTime.UtcNow);
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var stripeResponse = JsonSerializer.Deserialize<StripePaymentIntentResponseDto>(responseJson, _jsonOptions);

        return new ProviderPaymentResponse(true, stripeResponse?.Id ?? "", stripeResponse?.Id ?? "", DateTime.UtcNow);
    }

    public async Task<ProviderPaymentResponse> ConfirmPaymentIntentAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default)
    {
        // Stripe-specific confirmation
        var json = JsonSerializer.Serialize(new { }, _jsonOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"/v1/payment_intents/{paymentIntentId}/confirm", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new ProviderPaymentResponse(false, "", "", DateTime.UtcNow);
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var stripeResponse = JsonSerializer.Deserialize<StripePaymentIntentResponseDto>(responseJson, _jsonOptions);

        return new ProviderPaymentResponse(true, stripeResponse?.Id ?? "", stripeResponse?.Id ?? "", DateTime.UtcNow);
    }
}
