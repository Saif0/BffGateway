using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Extensions;
using Serilog;
using Serilog.Context;

namespace BffGateway.WebApi.Middleware;

/// <summary>
/// Comprehensive structured logging middleware that captures all inbound HTTP requests and responses
/// with full context and sensitive data filtering.
/// </summary>
public class StructuredRequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly Serilog.ILogger _logger;
    private readonly HashSet<string> _sensitiveHeaders;
    private readonly HashSet<string> _sensitiveBodyFields;
    private readonly int _maxBodySize;

    public StructuredRequestLoggingMiddleware(RequestDelegate next, Serilog.ILogger logger)
    {
        _next = next;
        _logger = logger;
        _maxBodySize = 8192; // 8KB max body logging

        // Define sensitive headers to mask
        _sensitiveHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Authorization", "Cookie", "Set-Cookie", "X-API-Key",
            "Authentication", "Proxy-Authorization", "WWW-Authenticate"
        };

        // Define sensitive body fields to mask
        _sensitiveBodyFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "password", "token", "secret", "key", "authorization",
            "cardNumber", "cvv", "pin", "ssn", "creditCard"
        };
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrSetCorrelationId(context);
        var requestId = Guid.NewGuid().ToString();
        var stopwatch = Stopwatch.StartNew();

        // Use LogContext to add request-scoped properties (including distributed trace IDs)
        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("RequestId", requestId))
        using (LogContext.PushProperty("RequestType", "Inbound"))
        using (LogContext.PushProperty("TraceId", Activity.Current?.TraceId.ToString()))
        using (LogContext.PushProperty("SpanId", Activity.Current?.SpanId.ToString()))
        {
            await LogInboundRequestAsync(context, correlationId, requestId);

            // Capture original response body stream
            var originalResponseBody = context.Response.Body;
            using var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();

                // Copy captured response body back to original stream
                responseBodyStream.Position = 0;
                await responseBodyStream.CopyToAsync(originalResponseBody);
                context.Response.Body = originalResponseBody;

                await LogInboundResponseAsync(context, correlationId, requestId,
                    responseBodyStream, stopwatch.ElapsedMilliseconds);
            }
        }
    }

    private async Task LogInboundRequestAsync(HttpContext context, string correlationId, string requestId)
    {
        var request = context.Request;

        // Enable buffering to allow multiple reads
        request.EnableBuffering();

        // Read request body
        var requestBody = await ReadRequestBodyAsync(request);

        // Filter sensitive headers
        var headers = FilterSensitiveHeaders(request.Headers);

        // Log structured request
        _logger.Information("HTTP Request Started {Method} {Url} | Headers: {@Headers} | Body: {Body} | Size: {BodySize} | RemoteIP: {RemoteIP} | UserAgent: {UserAgent}",
            request.Method,
            request.GetDisplayUrl(),
            headers,
            FilterSensitiveBodyContent(requestBody),
            requestBody?.Length ?? 0,
            GetClientIpAddress(context),
            request.Headers.UserAgent.ToString());
    }

    private async Task LogInboundResponseAsync(HttpContext context, string correlationId,
        string requestId, MemoryStream responseBodyStream, long elapsedMs)
    {
        var response = context.Response;
        var request = context.Request;

        // Read response body
        responseBodyStream.Position = 0;
        var responseBody = await ReadResponseBodyAsync(responseBodyStream);

        // Filter sensitive headers
        var headers = FilterSensitiveHeaders(response.Headers);

        // Determine log level based on status code
        var logLevel = GetLogLevelForStatusCode(response.StatusCode);

        _logger.Write(logLevel, "HTTP Request Completed {Method} {Url} | Status: {StatusCode} | Duration: {ElapsedMs}ms | Headers: {@Headers} | Body: {Body} | Size: {BodySize}",
            request.Method,
            request.GetDisplayUrl(),
            response.StatusCode,
            elapsedMs,
            headers,
            FilterSensitiveBodyContent(responseBody),
            responseBody?.Length ?? 0);
    }

    private async Task<string?> ReadRequestBodyAsync(HttpRequest request)
    {
        if (request.ContentLength == null || request.ContentLength == 0 || !request.Body.CanRead)
            return null;

        var bodySize = Math.Min(request.ContentLength.Value, _maxBodySize);

        request.Body.Position = 0;
        using var reader = new StreamReader(request.Body, Encoding.UTF8, false, (int)bodySize, true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0; // Reset for next middleware

        return body.Length > _maxBodySize ? body[.._maxBodySize] + "..." : body;
    }

    private async Task<string?> ReadResponseBodyAsync(MemoryStream responseBodyStream)
    {
        if (responseBodyStream.Length == 0)
            return null;

        responseBodyStream.Position = 0;
        var bodySize = Math.Min(responseBodyStream.Length, _maxBodySize);

        using var reader = new StreamReader(responseBodyStream, Encoding.UTF8, false, (int)bodySize, true);
        var body = await reader.ReadToEndAsync();

        return body.Length > _maxBodySize ? body[.._maxBodySize] + "..." : body;
    }

    private Dictionary<string, object> FilterSensitiveHeaders(IHeaderDictionary headers)
    {
        var filteredHeaders = new Dictionary<string, object>();

        foreach (var header in headers)
        {
            var value = _sensitiveHeaders.Contains(header.Key)
                ? "***MASKED***"
                : header.Value.ToString();

            filteredHeaders[header.Key] = value;
        }

        return filteredHeaders;
    }

    private string? FilterSensitiveBodyContent(string? body)
    {
        if (string.IsNullOrEmpty(body))
            return body;

        try
        {
            // Try to parse as JSON and mask sensitive fields
            using var document = JsonDocument.Parse(body);
            return MaskSensitiveJsonFields(document.RootElement).ToString();
        }
        catch
        {
            // If not JSON, return as-is (could enhance for other formats)
            return body;
        }
    }

    private JsonElement MaskSensitiveJsonFields(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var maskedObject = new Dictionary<string, object?>();

            foreach (var property in element.EnumerateObject())
            {
                if (_sensitiveBodyFields.Contains(property.Name))
                {
                    maskedObject[property.Name] = "***MASKED***";
                }
                else
                {
                    maskedObject[property.Name] = property.Value.ValueKind switch
                    {
                        JsonValueKind.Object => MaskSensitiveJsonFields(property.Value),
                        JsonValueKind.Array => property.Value.EnumerateArray()
                            .Select(MaskSensitiveJsonFields).ToArray(),
                        _ => property.Value.Clone()
                    };
                }
            }

            return JsonSerializer.SerializeToElement(maskedObject);
        }

        return element;
    }

    private static string GetOrSetCorrelationId(HttpContext context)
    {
        const string correlationHeaderName = "X-Correlation-ID";

        if (context.Request.Headers.TryGetValue(correlationHeaderName, out var correlationId)
            && !string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId.ToString();
        }

        var newCorrelationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString();
        context.Request.Headers[correlationHeaderName] = newCorrelationId;
        context.Response.Headers[correlationHeaderName] = newCorrelationId;

        return newCorrelationId;
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        var xForwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xForwardedFor))
        {
            return xForwardedFor.Split(',')[0].Trim();
        }

        var xRealIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xRealIp))
        {
            return xRealIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    private static Serilog.Events.LogEventLevel GetLogLevelForStatusCode(int statusCode)
    {
        return statusCode switch
        {
            >= 500 => Serilog.Events.LogEventLevel.Error,
            >= 400 => Serilog.Events.LogEventLevel.Warning,
            _ => Serilog.Events.LogEventLevel.Information
        };
    }
}
