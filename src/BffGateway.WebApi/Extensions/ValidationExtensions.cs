using Microsoft.AspNetCore.Mvc;

namespace BffGateway.WebApi.Extensions;

public static class ValidationExtensions
{
    public static IServiceCollection AddCustomValidation(this IServiceCollection services)
    {
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                    );

                var problemDetails = new ValidationProblemDetails(errors)
                {
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                    Title = "One or more validation errors occurred.",
                    Status = 400,
                    Instance = context.HttpContext.Request.Path,
                    Extensions =
                    {
                        ["traceId"] = context.HttpContext.TraceIdentifier,
                        ["isSuccess"] = false,
                        ["message"] = "One or more validation errors occurred."
                    }
                };

                return new BadRequestObjectResult(problemDetails);
            };
        });

        return services;
    }
}
