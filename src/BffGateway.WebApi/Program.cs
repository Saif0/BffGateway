using BffGateway.Infrastructure;
using BffGateway.WebApi.Swagger;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Reflection;
using BffGateway.WebApi.Middleware;
using BffGateway.WebApi.Extensions;
using BffGateway.WebApi.Handlers;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Configure enums to serialize as strings instead of numbers
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Add API versioning (simplified approach for .NET 8+)
builder.Services.AddApiVersioning();

// Add MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

// Add Application layer
var applicationAssembly = Assembly.Load("BffGateway.Application");
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(applicationAssembly));

// Add FluentValidation
builder.Services.AddValidatorsFromAssembly(applicationAssembly);

// Add Infrastructure layer
builder.Services.AddInfrastructure(builder.Configuration);

// Correlation and context services
builder.Services.AddHttpContextAccessor();

// Add health checks
builder.Services.AddBffHealthChecks();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "BFF Gateway API", Version = "v1" });
    c.SwaggerDoc("v2", new() { Title = "BFF Gateway API", Version = "v2" });

    // Configure enums to show as strings in Swagger
    c.SchemaFilter<EnumSchemaFilter>();

    // Hide obsolete (deprecated) actions from Swagger
    // c.IgnoreObsoleteActions();
    c.DocInclusionPredicate((docName, apiDesc) =>
    {
        var groupName = apiDesc.GroupName ?? apiDesc.ActionDescriptor.DisplayName;
        return string.Equals(docName, groupName, StringComparison.OrdinalIgnoreCase);
    });
});

// ProblemDetails and Global Exception Handler
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// OpenTelemetry (conditionally enabled)
var enableOtel = builder.Configuration.GetValue<bool>("Observability:EnableOpenTelemetry");
if (enableOtel)
{
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService("BffGateway"))
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddConsoleExporter())
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            .AddConsoleExporter());
}

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        // Put v2 first to make it default/primary
        c.SwaggerEndpoint("/swagger/v2/swagger.json", "BFF Gateway API V2");
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BFF Gateway API V1 (deprecated)");
        c.DocumentTitle = "BFF Gateway API";
    });
}

app.UseExceptionHandler();

app.UseSerilogRequestLogging(opts =>
{
    opts.EnrichDiagnosticContext = (ctx, http) =>
    {
        ctx.Set("CorrelationId", http.Request.Headers["X-Correlation-ID"].ToString());
        ctx.Set("RequestHost", http.Request.Host.Value);
        ctx.Set("RequestPath", http.Request.Path);
    };
});

app.UseRouting();

// Correlation ID middleware
app.UseMiddleware<CorrelationIdMiddleware>();

// Deprecation headers for v1 endpoints
app.UseMiddleware<DeprecationHeadersMiddleware>();

// Health check endpoints
app.MapBffHealthChecks();

app.MapControllers();

try
{
    Log.Information("Starting BFF Gateway");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
