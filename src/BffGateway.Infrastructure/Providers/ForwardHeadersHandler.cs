using System.Diagnostics;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace BffGateway.Infrastructure.Providers;

public class ForwardHeadersHandler : DelegatingHandler
{
    private const string CorrelationHeaderName = "X-Correlation-ID";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ForwardHeadersHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            // Forward Authorization header if present
            if (httpContext.Request.Headers.TryGetValue("Authorization", out var authValues))
            {
                if (AuthenticationHeaderValue.TryParse(authValues.ToString(), out var header))
                {
                    request.Headers.Authorization = header;
                }
            }

            // Ensure correlation id header exists and forward it
            var correlationId = httpContext.Request.Headers[CorrelationHeaderName].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(correlationId))
            {
                correlationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString();
                httpContext.Request.Headers[CorrelationHeaderName] = correlationId;
            }

            // Ensure correlation id is propagated outbound and align Activity if present
            request.Headers.Remove(CorrelationHeaderName);
            request.Headers.TryAddWithoutValidation(CorrelationHeaderName, correlationId);
            Activity.Current?.SetTag("correlation.id", correlationId);
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}


