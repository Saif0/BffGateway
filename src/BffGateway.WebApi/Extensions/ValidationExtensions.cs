using Microsoft.AspNetCore.Mvc;
using BffGateway.Application.Abstractions.Services;
using BffGateway.WebApi.Constants;

namespace BffGateway.WebApi.Extensions;

public static class ValidationExtensions
{
    public static IServiceCollection AddCustomValidation(this IServiceCollection services)
    {
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var messageService = context.HttpContext.RequestServices.GetService<IMessageService>();

                var errors = context.ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                    );

                var title = messageService?.GetMessage(MessageKeys.Validation.ValidationErrorsTitle)
                           ?? "One or more validation errors occurred.";
                var message = messageService?.GetMessage(MessageKeys.Validation.ValidationErrorsMessage)
                             ?? "One or more validation errors occurred.";

                var problemDetails = new ValidationProblemDetails(errors)
                {
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                    Title = title,
                    Status = 400,
                    Instance = context.HttpContext.Request.Path,
                    Extensions =
                    {
                        ["traceId"] = context.HttpContext.TraceIdentifier,
                        ["isSuccess"] = false,
                        ["message"] = message
                    }
                };

                return new BadRequestObjectResult(problemDetails);
            };
        });

        return services;
    }
}
