using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BffGateway.WebApi.Swagger;

/// <summary>
/// Adds Accept-Language header parameter to all Swagger operations for language selection
/// </summary>
public class AcceptLanguageOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= new List<OpenApiParameter>();

        // Add Accept-Language header parameter
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "Accept-Language",
            In = ParameterLocation.Header,
            Required = false,
            Description = "Language preference for response messages (en = English, ar = Arabic)",
            Schema = new OpenApiSchema
            {
                Type = "string",
                Default = new Microsoft.OpenApi.Any.OpenApiString("en"),
                Enum = new List<Microsoft.OpenApi.Any.IOpenApiAny>
                {
                    new Microsoft.OpenApi.Any.OpenApiString("en"),
                    new Microsoft.OpenApi.Any.OpenApiString("ar")
                }
            },
            Example = new Microsoft.OpenApi.Any.OpenApiString("en")
        });
    }
}
