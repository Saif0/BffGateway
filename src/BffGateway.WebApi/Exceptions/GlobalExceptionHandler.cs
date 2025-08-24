using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Polly.CircuitBreaker;
using System.Net;
using System.Text.Json;

namespace BffGateway.WebApi.Exceptions;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = MapException(exception);

        _logger.LogError(exception,
            "Exception occurred: {Message} | StatusCode: {StatusCode} | Path: {Path}",
            exception.Message, statusCode, httpContext.Request.Path);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path,
            Extensions = new Dictionary<string, object?>
            {
                ["traceId"] = httpContext.TraceIdentifier,
                ["timestamp"] = DateTimeOffset.UtcNow,
                ["isSuccess"] = false,
                ["message"] = detail
            }
        };

        // Add correlation ID if available
        if (httpContext.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId))
        {
            problemDetails.Extensions["correlationId"] = correlationId.ToString();
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        await httpContext.Response.WriteAsync(json, cancellationToken);

        return true;
    }

    private static (int StatusCode, string Title, string Detail) MapException(Exception exception)
    {
        return exception switch
        {
            BrokenCircuitException => (
                (int)HttpStatusCode.ServiceUnavailable,
                "Service Unavailable",
                "The upstream service is currently unavailable due to circuit breaker protection. Please try again later."
            ),
            TaskCanceledException or OperationCanceledException => (
                (int)HttpStatusCode.GatewayTimeout,
                "Gateway Timeout",
                "The request timed out while communicating with upstream services."
            ),
            HttpRequestException httpEx when httpEx.Message.Contains("timeout") => (
                (int)HttpStatusCode.GatewayTimeout,
                "Gateway Timeout",
                "The request timed out while communicating with upstream services."
            ),
            HttpRequestException httpEx when httpEx.Message.Contains("connection") => (
                (int)HttpStatusCode.BadGateway,
                "Bad Gateway",
                "Unable to establish connection with upstream services."
            ),
            ArgumentException or ArgumentNullException => (
                (int)HttpStatusCode.BadRequest,
                "Bad Request",
                "The request contains invalid parameters."
            ),
            UnauthorizedAccessException => (
                (int)HttpStatusCode.Unauthorized,
                "Unauthorized",
                "Authentication is required to access this resource."
            ),
            _ => (
                (int)HttpStatusCode.InternalServerError,
                "Internal Server Error",
                "An unexpected error occurred. Please try again later."
            )
        };
    }
}
