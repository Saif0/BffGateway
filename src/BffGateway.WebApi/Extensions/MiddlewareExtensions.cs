using BffGateway.WebApi.Middleware;
using Serilog;

namespace BffGateway.WebApi.Extensions;

public static class MiddlewareExtensions
{
    public static WebApplication UseCustomMiddleware(this WebApplication app)
    {
        app.UseExceptionHandler();

        app.UseSerilogRequestLogging(opts =>
        {
            opts.EnrichDiagnosticContext = (ctx, http) =>
            {
                ctx.Set("CorrelationId", http.Request.Headers["X-Correlation-ID"].ToString());
                ctx.Set("RequestHost", http.Request.Host.Value);
                ctx.Set("RequestPath", http.Request.Path);
            };
        });

        app.UseRouting();

        // Correlation ID middleware
        app.UseMiddleware<CorrelationIdMiddleware>();

        // Deprecation headers for v1 endpoints
        app.UseMiddleware<DeprecationHeadersMiddleware>();

        // Health check endpoints
        app.MapBffHealthChecks();

        app.MapControllers();

        return app;
    }
}
