using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BffGateway.WebApi.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddBffHealthChecks(this IServiceCollection services)
    {
        // Health checks are now handled by the HealthController
        // Keeping this extension for potential future use or if we want to add back middleware endpoints
        return services;
    }

    public static IEndpointRouteBuilder MapBffHealthChecks(this IEndpointRouteBuilder app)
    {
        // Health check endpoints are now handled by HealthController
        // Keeping this method for backward compatibility but not mapping any endpoints
        return app;
    }

    private static Task WriteResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var correlationId = context.Request.Headers["X-Correlation-ID"].ToString();

        var result = new
        {
            status = report.Status.ToString(),
            correlationId,
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            entries = report.Entries.ToDictionary(
                e => e.Key,
                e => new
                {
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    durationMs = e.Value.Duration.TotalMilliseconds
                })
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        return context.Response.WriteAsync(JsonSerializer.Serialize(result, options));
    }
}


