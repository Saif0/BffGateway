namespace BffGateway.Infrastructure.Configuration;

public class ProviderOptions
{
    public const string SectionName = "Provider";

    public string BaseUrl { get; set; } = "http://localhost:5001";
    public int TimeoutSeconds { get; set; } = 30;
    public int ConnectTimeoutSeconds { get; set; } = 10;
    public RetryOptions Retry { get; set; } = new();
    public CircuitBreakerOptions CircuitBreaker { get; set; } = new();
}

public class RetryOptions
{
    public int MaxRetries { get; set; } = 3;
    public int BaseDelayMs { get; set; } = 1000;
    public int MaxJitterMs { get; set; } = 500;
}

public class CircuitBreakerOptions
{
    public int FailureThreshold { get; set; } = 5;
    public int SamplingDurationSeconds { get; set; } = 60;
    public int MinimumThroughput { get; set; } = 10;
    public int DurationOfBreakSeconds { get; set; } = 30;
}
