using BffGateway.WebApi.Middleware;
using Serilog;

namespace BffGateway.WebApi.Extensions;

public static class MiddlewareExtensions
{
    public static WebApplication UseCustomMiddleware(this WebApplication app)
    {
        app.UseExceptionHandler();

        // Use our comprehensive structured request logging instead of basic Serilog request logging
        app.UseMiddleware<StructuredRequestLoggingMiddleware>(Log.Logger);

        // TESTING NOTE: To test OpenTelemetry logging, comment out the above line and uncomment below:
        // if (app.Environment.IsDevelopment())
        // {
        //     app.UseHttpLogging();
        // }

        app.UseRouting();

        // Correlation ID middleware (now handled by StructuredRequestLoggingMiddleware)
        // app.UseMiddleware<CorrelationIdMiddleware>();

        // Deprecation headers for v1 endpoints
        app.UseMiddleware<DeprecationHeadersMiddleware>();

        // Health check endpoints
        app.MapBffHealthChecks();

        app.MapControllers();

        return app;
    }
}
