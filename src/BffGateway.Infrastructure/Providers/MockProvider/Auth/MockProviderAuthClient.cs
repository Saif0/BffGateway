using BffGateway.Application.Common.DTOs.Auth;
using BffGateway.Application.Common.Enums;
using BffGateway.Infrastructure.Providers.MockProvider.DTOs;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace BffGateway.Infrastructure.Providers.MockProvider.Auth;

public class MockProviderAuthClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MockProviderAuthClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public MockProviderAuthClient(HttpClient httpClient, ILogger<MockProviderAuthClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<ProviderAuthResponse> AuthenticateAsync(
    ProviderAuthRequest request,
    SimulationScenario scenario = SimulationScenario.None,
    CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calling MockProvider authentication");

        try
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var sw = Stopwatch.StartNew();
            var url = $"/api/authenticate?scenario={scenario}";
            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            sw.Stop();

            _logger.LogInformation("MockProvider call {Path} ended with {StatusCode} in {ElapsedMs}ms",
                "/api/authenticate", (int)response.StatusCode, sw.ElapsedMilliseconds);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("MockProvider authentication failed");
                return new ProviderAuthResponse(false, string.Empty, DateTime.MinValue);
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var providerResponse = JsonSerializer.Deserialize<MockProviderAuthResponseDto>(responseJson, _jsonOptions);

            return new ProviderAuthResponse(
                providerResponse?.Success ?? false,
                providerResponse?.Token ?? string.Empty,
                providerResponse?.ExpiresAt ?? DateTime.MinValue
            );
        }
        catch (BrokenCircuitException bce)
        {
            _logger.LogWarning(bce, "Circuit breaker open for MockProvider {Path}", "/api/authenticate");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during MockProvider authentication");
            return new ProviderAuthResponse(false, string.Empty, DateTime.MinValue);
        }
    }

    // Additional MockProvider-specific auth endpoints
    // public async Task<ProviderAuthResponse> RefreshTokenAsync(...)
    // public async Task<bool> ValidateTokenAsync(...)
    // public async Task<bool> RevokeTokenAsync(...)
}
