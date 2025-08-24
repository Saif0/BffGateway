using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace BffGateway.WebApi.Extensions;

public static class ObservabilityExtensions
{
    public static IServiceCollection AddCustomObservability(this IServiceCollection services, IConfiguration configuration)
    {
        var enableOtel = configuration.GetValue<bool>("Observability:EnableOpenTelemetry");

        if (enableOtel)
        {
            services.AddOpenTelemetry()
                .ConfigureResource(resource => resource.AddService("BffGateway"))
                .WithTracing(tracing => tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddConsoleExporter())
                .WithMetrics(metrics => metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddConsoleExporter());
        }

        return services;
    }
}
