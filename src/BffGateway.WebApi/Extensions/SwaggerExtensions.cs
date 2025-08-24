using BffGateway.WebApi.Swagger;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc;

namespace BffGateway.WebApi.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddCustomSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            // Get API version descriptions from ApiExplorer
            var serviceProvider = services.BuildServiceProvider();
            var apiVersionDescriptionProvider = serviceProvider.GetRequiredService<IApiVersionDescriptionProvider>();

            // Create a Swagger document for each discovered API version
            foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
            {
                c.SwaggerDoc(description.GroupName, new()
                {
                    Title = "BFF Gateway API",
                    Version = description.ApiVersion.ToString(),
                    Description = description.IsDeprecated ? " - DEPRECATED" : ""
                });
            }

            // Configure enums to show as strings in Swagger
            c.SchemaFilter<EnumSchemaFilter>();

            // Add Accept-Language header to all operations for localization testing
            c.OperationFilter<AcceptLanguageOperationFilter>();

            // Include actions based on API version group
            c.DocInclusionPredicate((docName, apiDesc) =>
            {
                // Use the GroupName from API description which is set by ApiExplorer
                return apiDesc.GroupName?.Equals(docName, StringComparison.OrdinalIgnoreCase) == true;
            });
        });

        return services;
    }

    public static WebApplication UseCustomSwagger(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                var apiVersionDescriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

                // Add endpoints for each API version, with latest version first (as default)
                var descriptions = apiVersionDescriptionProvider.ApiVersionDescriptions
                    .OrderByDescending(desc => desc.ApiVersion)
                    .ToList();

                foreach (var description in descriptions)
                {
                    var url = $"/swagger/{description.GroupName}/swagger.json";
                    var name = $"BFF Gateway API {description.ApiVersion}";

                    if (description.IsDeprecated)
                        name += " (deprecated)";

                    c.SwaggerEndpoint(url, name);
                }

                c.DocumentTitle = "BFF Gateway API - Multi-Language Support";

                // Set the default version to show (latest version will be first due to ordering)
                if (descriptions.Any())
                {
                    c.DefaultModelsExpandDepth(-1); // Hide models section by default
                }
            });
        }

        return app;
    }
}
