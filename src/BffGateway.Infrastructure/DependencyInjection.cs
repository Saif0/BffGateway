using BffGateway.Application.Abstractions.Providers;
using BffGateway.Infrastructure.Configuration;
using BffGateway.Infrastructure.Providers;
using BffGateway.Infrastructure.Providers.MockProvider;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
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

        // Register delegating handler
        services.AddTransient<ForwardHeadersHandler>();

        // Named HTTP client for MockProvider (default)
        services.AddHttpClient("MockProvider", (serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<ProviderOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        })
        .ConfigurePrimaryHttpMessageHandler((serviceProvider) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<ProviderOptions>>().Value;
            return new SocketsHttpHandler
            {
                ConnectTimeout = TimeSpan.FromSeconds(options.ConnectTimeoutSeconds),
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            };
        })
        .AddHttpMessageHandler<ForwardHeadersHandler>()
        .AddPolicyHandler((serviceProvider, request) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<ProviderOptions>>().Value;
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("PollyHttpPolicies");
            return CreateRetryPolicy(options.Retry, logger);
        })
        .AddPolicyHandler((serviceProvider, request) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<ProviderOptions>>().Value;
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("PollyHttpPolicies");
            return CreateCircuitBreakerPolicy(options.CircuitBreaker, logger);
        })
        .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(30)));

        // Default IProviderClient directly uses MockProvider
        services.AddTransient<IProviderClient>(sp =>
        {
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("MockProvider");
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            return new MockProviderClient(httpClient, loggerFactory);
        });

        // Factory for resolving provider by name
        services.AddSingleton<IProviderClientFactory, ProviderClientFactory>();

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy(RetryOptions options, ILogger logger)
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
                    var reason = outcome.Exception?.GetType().Name ?? outcome.Result?.StatusCode.ToString();
                    logger.LogWarning(
                        "Retrying outbound call attempt {RetryAttempt} after {DelayMs}ms due to {Reason} (op={OperationKey})",
                        retryCount,
                        timespan.TotalMilliseconds,
                        reason,
                        context.OperationKey);
                });
    }

    private static IAsyncPolicy<HttpResponseMessage> CreateCircuitBreakerPolicy(CircuitBreakerOptions options, ILogger logger)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: options.FailureThreshold,
                durationOfBreak: TimeSpan.FromSeconds(options.DurationOfBreakSeconds),
                onBreak: (outcome, duration) =>
                {
                    var reason = outcome.Exception?.GetType().Name ?? outcome.Result?.StatusCode.ToString();
                    logger.LogWarning("Circuit breaker OPEN for outbound provider calls for {DurationSeconds}s due to {Reason}", duration.TotalSeconds, reason);
                },
                onReset: () =>
                {
                    logger.LogInformation("Circuit breaker RESET for outbound provider calls");
                },
                onHalfOpen: () =>
                {
                    logger.LogInformation("Circuit breaker HALF-OPEN for outbound provider calls");
                });
    }
}
