using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Polly.CircuitBreaker;
using System.Net;
using System.Text.Json;
using BffGateway.Application.Abstractions.Services;
using BffGateway.WebApi.Constants;

namespace BffGateway.WebApi.Exceptions;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IMessageService _messageService;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IMessageService messageService)
    {
        _logger = logger;
        _messageService = messageService;
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

    private (int StatusCode, string Title, string Detail) MapException(Exception exception)
    {
        return exception switch
        {
            BrokenCircuitException => (
                (int)HttpStatusCode.ServiceUnavailable,
                _messageService.GetMessage(MessageKeys.Errors.ServiceUnavailable),
                _messageService.GetMessage(MessageKeys.Errors.ServiceUnavailableDetail)
            ),
            TaskCanceledException or OperationCanceledException => (
                (int)HttpStatusCode.GatewayTimeout,
                _messageService.GetMessage(MessageKeys.Errors.GatewayTimeout),
                _messageService.GetMessage(MessageKeys.Errors.GatewayTimeoutDetail)
            ),
            HttpRequestException httpEx when httpEx.Message.Contains("timeout") => (
                (int)HttpStatusCode.GatewayTimeout,
                _messageService.GetMessage(MessageKeys.Errors.GatewayTimeout),
                _messageService.GetMessage(MessageKeys.Errors.GatewayTimeoutDetail)
            ),
            HttpRequestException httpEx when httpEx.Message.Contains("connection") => (
                (int)HttpStatusCode.BadGateway,
                _messageService.GetMessage(MessageKeys.Errors.BadGateway),
                _messageService.GetMessage(MessageKeys.Errors.BadGatewayDetail)
            ),
            ArgumentException or ArgumentNullException => (
                (int)HttpStatusCode.BadRequest,
                _messageService.GetMessage(MessageKeys.Errors.BadRequest),
                _messageService.GetMessage(MessageKeys.Errors.BadRequestDetail)
            ),
            UnauthorizedAccessException => (
                (int)HttpStatusCode.Unauthorized,
                _messageService.GetMessage(MessageKeys.Errors.Unauthorized),
                _messageService.GetMessage(MessageKeys.Errors.UnauthorizedDetail)
            ),
            _ => (
                (int)HttpStatusCode.InternalServerError,
                _messageService.GetMessage(MessageKeys.Errors.InternalServerError),
                _messageService.GetMessage(MessageKeys.Errors.InternalServerErrorDetail)
            )
        };
    }
}
