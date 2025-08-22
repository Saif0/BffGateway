namespace BffGateway.WebApi.Middleware;

public class DeprecationHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public DeprecationHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // For URL-segment versioning, add deprecation headers for v1 endpoints
        if (context.Request.Path.HasValue && context.Request.Path.Value!.StartsWith("/v1/", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers.TryAdd("Deprecation", "true");
                // Example sunset date; adjust as appropriate
                context.Response.Headers.TryAdd("Sunset", "Wed, 31 Dec 2025 23:59:59 GMT");
                context.Response.Headers.TryAdd("Link", "</swagger/v2/swagger.json>; rel=successor-version");
                context.Response.Headers.TryAdd("Warning", "299 - \"v1 is deprecated; migrate to v2\"");
                return Task.CompletedTask;
            });
        }

        await _next(context);
    }
}


