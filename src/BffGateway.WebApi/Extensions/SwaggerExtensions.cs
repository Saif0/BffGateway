using BffGateway.WebApi.Swagger;

namespace BffGateway.WebApi.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddCustomSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = "BFF Gateway API", Version = "v1" });
            c.SwaggerDoc("v2", new() { Title = "BFF Gateway API", Version = "v2" });

            // Configure enums to show as strings in Swagger
            c.SchemaFilter<EnumSchemaFilter>();

            // Add Accept-Language header to all operations for localization testing
            c.OperationFilter<AcceptLanguageOperationFilter>();

            // Hide obsolete (deprecated) actions from Swagger
            // c.IgnoreObsoleteActions();
            c.DocInclusionPredicate((docName, apiDesc) =>
            {
                var groupName = apiDesc.GroupName ?? apiDesc.ActionDescriptor.DisplayName;
                return string.Equals(docName, groupName, StringComparison.OrdinalIgnoreCase);
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
    // Put v2 first to make it default/primary
    c.SwaggerEndpoint("/swagger/v2/swagger.json", "BFF Gateway API V2");
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "BFF Gateway API V1 (deprecated)");
    c.DocumentTitle = "BFF Gateway API - Multi-Language Support";
});
        }

        return app;
    }
}
