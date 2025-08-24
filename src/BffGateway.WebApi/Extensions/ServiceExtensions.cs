using BffGateway.Infrastructure;
using BffGateway.WebApi.Exceptions;
using FluentValidation;
using MediatR;
using System.Reflection;
using System.Text.Json.Serialization;
using BffGateway.Application.Common.Behaviors;

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

        // Add API versioning
        services.AddApiVersioning();

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