using BffGateway.Application.Common.Interfaces;
using BffGateway.Infrastructure.Configuration;
using BffGateway.Infrastructure.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace BffGateway.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure provider options
        services.Configure<ProviderOptions>(configuration.GetSection(ProviderOptions.SectionName));

        // Add HTTP client with Polly policies
        services.AddHttpClient<IProviderClient, ProviderClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<ProviderOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
        {
            // Configure connection timeout
            // Note: HttpClientHandler doesn't directly support ConnectTimeout in .NET
            // This would typically be configured at the infrastructure level
        })
        .AddPolicyHandler((serviceProvider, request) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<ProviderOptions>>().Value;
            return CreateRetryPolicy(options.Retry);
        })
        .AddPolicyHandler((serviceProvider, request) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<ProviderOptions>>().Value;
            return CreateCircuitBreakerPolicy(options.CircuitBreaker);
        })
        .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(30)));

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy(RetryOptions options)
    {
        var jitterer = new Random();

        return HttpPolicyExtensions
            .HandleTransientHttpError() // HttpRequestException and 5XX and 408 status codes
            .Or<TimeoutRejectedException>()
            .WaitAndRetryAsync(
                retryCount: options.MaxRetries,
                sleepDurationProvider: retryAttempt =>
                {
                    var delay = TimeSpan.FromMilliseconds(options.BaseDelayMs * Math.Pow(2, retryAttempt - 1));
                    var jitter = TimeSpan.FromMilliseconds(jitterer.Next(0, options.MaxJitterMs));
                    return delay + jitter;
                },
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    // Log retry attempts - in production would use proper logger from DI
                    System.Console.WriteLine($"Retry {retryCount} for {context.OperationKey} in {timespan.TotalMilliseconds}ms due to: {outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()}");
                });
    }

    private static IAsyncPolicy<HttpResponseMessage> CreateCircuitBreakerPolicy(CircuitBreakerOptions options)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: options.FailureThreshold,
                durationOfBreak: TimeSpan.FromSeconds(options.DurationOfBreakSeconds),
                onBreak: (exception, duration) =>
                {
                    // Log circuit breaker opening
                },
                onReset: () =>
                {
                    // Log circuit breaker closing
                },
                onHalfOpen: () =>
                {
                    // Log circuit breaker half-open
                });
    }
}
