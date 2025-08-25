using OpenTelemetry.Exporter;
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
            var otlpEndpoint = configuration.GetValue<string>("Observability:Otlp:Endpoint");
            if (string.IsNullOrWhiteSpace(otlpEndpoint))
            {
                throw new InvalidOperationException("Missing required configuration 'Observability:Otlp:Endpoint' when OpenTelemetry is enabled.");
            }

            var otlpProtocolSetting = configuration.GetValue<string>("Observability:Otlp:Protocol");
            if (string.IsNullOrWhiteSpace(otlpProtocolSetting))
            {
                throw new InvalidOperationException("Missing required configuration 'Observability:Otlp:Protocol' when OpenTelemetry is enabled. Allowed values: 'Grpc' or 'HttpProtobuf'.");
            }

            OtlpExportProtocol otlpProtocol;
            if (string.Equals(otlpProtocolSetting, "Grpc", StringComparison.OrdinalIgnoreCase))
            {
                otlpProtocol = OtlpExportProtocol.Grpc;
            }
            else if (string.Equals(otlpProtocolSetting, "HttpProtobuf", StringComparison.OrdinalIgnoreCase))
            {
                otlpProtocol = OtlpExportProtocol.HttpProtobuf;
            }
            else
            {
                throw new InvalidOperationException("Invalid value for 'Observability:Otlp:Protocol'. Allowed values: 'Grpc' or 'HttpProtobuf'.");
            }

            services.AddOpenTelemetry()
                .ConfigureResource(resource => resource.AddService("BffGateway"))
                .WithTracing(tracing => tracing
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.Filter = httpContext => true;
                        options.EnrichWithHttpRequest = (activity, request) =>
                        {
                            activity.SetTag("http.request_content_length", request.ContentLength);
                        };
                        options.EnrichWithHttpResponse = (activity, response) =>
                        {
                            activity.SetTag("http.response_content_length", response.ContentLength);
                        };
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.EnrichWithHttpRequestMessage = (activity, request) =>
                        {
                            activity.SetTag("http.request_content_length", request.Content?.Headers.ContentLength);
                        };
                        options.EnrichWithHttpResponseMessage = (activity, response) =>
                        {
                            activity.SetTag("http.response_content_length", response.Content?.Headers.ContentLength);
                        };
                    })
                    .AddOtlpExporter(otlp =>
                    {
                        otlp.Endpoint = new Uri(otlpEndpoint);
                        otlp.Protocol = otlpProtocol;
                    }))
                .WithMetrics(metrics => metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddOtlpExporter(otlp =>
                    {
                        otlp.Endpoint = new Uri(otlpEndpoint);
                        otlp.Protocol = otlpProtocol;
                    }));
        }

        return services;
    }
}
