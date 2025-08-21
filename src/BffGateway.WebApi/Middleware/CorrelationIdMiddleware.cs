using Serilog.Context;

namespace BffGateway.WebApi.Middleware;

public class CorrelationIdMiddleware
{
    private const string CorrelationHeader = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = EnsureCorrelationId(context);

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            context.Response.Headers[CorrelationHeader] = correlationId;
            await _next(context);
        }
    }

    private static string EnsureCorrelationId(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(CorrelationHeader, out var header) || string.IsNullOrWhiteSpace(header))
        {
            var generated = Guid.NewGuid().ToString();
            context.Request.Headers[CorrelationHeader] = generated;
            return generated;
        }

        return header.ToString();
    }
}


