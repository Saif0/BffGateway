using BffGateway.Infrastructure;
using BffGateway.WebApi.Exceptions;
using FluentValidation;
using MediatR;
using System.Reflection;
using System.Text.Json.Serialization;
using BffGateway.Application.Common.Behaviors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;

namespace BffGateway.WebApi.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add controllers with JSON configuration
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                // Configure enums to serialize as strings instead of numbers
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        // Add API versioning with proper configuration
        services.AddApiVersioning(options =>
        {
            // Set default version to the latest (v2.0)
            options.DefaultApiVersion = new ApiVersion(2, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;

            // Configure how API versions are read from requests
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader() // Read from URL segment (e.g., /v2/auth/login)
                                                 // new HeaderApiVersionReader("X-Api-Version"), // Read from header
                                                 // new QueryStringApiVersionReader("version") // Read from query string
            );
        });

        // Add API Explorer for Swagger versioning support
        services.AddVersionedApiExplorer(options =>
        {
            // Automatically substitute version in controller names
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        // Add MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        // Add Application layer
        var applicationAssembly = Assembly.Load("BffGateway.Application");
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(applicationAssembly));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // Add FluentValidation with dependency injection support - explicitly register with dependencies
        services.AddValidatorsFromAssembly(applicationAssembly, ServiceLifetime.Scoped, includeInternalTypes: false);

        // Add Infrastructure layer
        services.AddInfrastructure(configuration);

        // Add correlation and context services
        services.AddHttpContextAccessor();

        // Add health checks
        services.AddBffHealthChecks();

        // Add ProblemDetails and Global Exception Handler
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        return services;
    }
}