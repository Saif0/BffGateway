using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Context;

namespace BffGateway.Infrastructure.Providers;

/// <summary>
/// Comprehensive structured logging handler for all outbound HTTP requests
/// with full request/response details and sensitive data filtering.
/// </summary>
public class StructuredHttpLoggingHandler : DelegatingHandler
{
    private readonly ILogger<StructuredHttpLoggingHandler> _logger;
    private readonly HashSet<string> _sensitiveHeaders;
    private readonly HashSet<string> _sensitiveBodyFields;
    private readonly int _maxBodySize;

    public StructuredHttpLoggingHandler(ILogger<StructuredHttpLoggingHandler> logger)
    {
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

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var requestId = Guid.NewGuid().ToString();
        var stopwatch = Stopwatch.StartNew();

        // Use LogContext to add request-scoped properties
        using (LogContext.PushProperty("RequestId", requestId))
        using (LogContext.PushProperty("RequestType", "Outbound"))
        using (LogContext.PushProperty("TraceId", Activity.Current?.TraceId.ToString()))
        using (LogContext.PushProperty("SpanId", Activity.Current?.SpanId.ToString()))
        {
            await LogOutboundRequestAsync(request, requestId);

            HttpResponseMessage? response = null;
            Exception? exception = null;

            try
            {
                response = await base.SendAsync(request, cancellationToken);
                return response;
            }
            catch (Exception ex)
            {
                exception = ex;
                throw;
            }
            finally
            {
                stopwatch.Stop();
                await LogOutboundResponseAsync(request, response, exception,
                    requestId, stopwatch.ElapsedMilliseconds);
            }
        }
    }

    private async Task LogOutboundRequestAsync(HttpRequestMessage request, string requestId)
    {
        // Read request body
        var requestBody = await ReadRequestBodyAsync(request);

        // Filter sensitive headers
        var headers = FilterSensitiveHeaders(request.Headers);

        // Add content headers if present
        if (request.Content?.Headers != null)
        {
            foreach (var header in request.Content.Headers)
            {
                var value = _sensitiveHeaders.Contains(header.Key)
                    ? "***MASKED***"
                    : string.Join(", ", header.Value);
                headers[header.Key] = value;
            }
        }

        _logger.LogInformation("HTTP Outbound Request Started {Method} {Url} | Headers: {@Headers} | Body: {Body} | Size: {BodySize}",
            request.Method.Method,
            request.RequestUri?.ToString(),
            headers,
            FilterSensitiveBodyContent(requestBody),
            requestBody?.Length ?? 0);
    }

    private async Task LogOutboundResponseAsync(HttpRequestMessage request,
        HttpResponseMessage? response, Exception? exception, string requestId, long elapsedMs)
    {
        if (exception != null)
        {
            _logger.LogError(exception, "HTTP Outbound Request Failed {Method} {Url} | Duration: {ElapsedMs}ms | Exception: {ExceptionType}",
                request.Method.Method,
                request.RequestUri?.ToString(),
                elapsedMs,
                exception.GetType().Name);
            return;
        }

        if (response == null)
        {
            _logger.LogWarning("HTTP Outbound Request Completed {Method} {Url} | Duration: {ElapsedMs}ms | No Response",
                request.Method.Method,
                request.RequestUri?.ToString(),
                elapsedMs);
            return;
        }

        // Read response body
        var responseBody = await ReadResponseBodyAsync(response);

        // Filter sensitive headers
        var headers = FilterSensitiveHeaders(response.Headers);

        // Add content headers if present
        if (response.Content?.Headers != null)
        {
            foreach (var header in response.Content.Headers)
            {
                var value = _sensitiveHeaders.Contains(header.Key)
                    ? "***MASKED***"
                    : string.Join(", ", header.Value);
                headers[header.Key] = value;
            }
        }

        // Determine log level based on status code and success
        var logLevel = GetLogLevelForResponse(response);

        _logger.Log(logLevel, "HTTP Outbound Request Completed {Method} {Url} | Status: {StatusCode} {ReasonPhrase} | Duration: {ElapsedMs}ms | Headers: {@Headers} | Body: {Body} | Size: {BodySize} | Success: {IsSuccess}",
            request.Method.Method,
            request.RequestUri?.ToString(),
            (int)response.StatusCode,
            response.ReasonPhrase,
            elapsedMs,
            headers,
            FilterSensitiveBodyContent(responseBody),
            responseBody?.Length ?? 0,
            response.IsSuccessStatusCode);
    }

    private async Task<string?> ReadRequestBodyAsync(HttpRequestMessage request)
    {
        if (request.Content == null)
            return null;

        try
        {
            // Get a copy of the content
            var content = await request.Content.ReadAsStringAsync();

            return content.Length > _maxBodySize
                ? content[.._maxBodySize] + "..."
                : content;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read outbound request body for logging");
            return $"[Error reading body: {ex.Message}]";
        }
    }

    private async Task<string?> ReadResponseBodyAsync(HttpResponseMessage response)
    {
        if (response.Content == null)
            return null;

        try
        {
            // Read the content without affecting the original stream
            var content = await response.Content.ReadAsStringAsync();

            return content.Length > _maxBodySize
                ? content[.._maxBodySize] + "..."
                : content;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read outbound response body for logging");
            return $"[Error reading body: {ex.Message}]";
        }
    }

    private Dictionary<string, object> FilterSensitiveHeaders(System.Net.Http.Headers.HttpHeaders headers)
    {
        var filteredHeaders = new Dictionary<string, object>();

        foreach (var header in headers)
        {
            var value = _sensitiveHeaders.Contains(header.Key)
                ? "***MASKED***"
                : string.Join(", ", header.Value);

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

    private static LogLevel GetLogLevelForResponse(HttpResponseMessage response)
    {
        return (int)response.StatusCode switch
        {
            >= 500 => LogLevel.Error,
            >= 400 => LogLevel.Warning,
            _ => LogLevel.Information
        };
    }
}
