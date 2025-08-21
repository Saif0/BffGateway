using BffGateway.Infrastructure;
using BffGateway.WebApi.HealthChecks;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Reflection;
using BffGateway.WebApi.Middleware;
using BffGateway.WebApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();

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
    c.DocInclusionPredicate((docName, apiDesc) =>
    {
        var groupName = apiDesc.GroupName ?? apiDesc.ActionDescriptor.DisplayName;
        return string.Equals(docName, groupName, StringComparison.OrdinalIgnoreCase);
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BFF Gateway API V1");
        c.SwaggerEndpoint("/swagger/v2/swagger.json", "BFF Gateway API V2");
    });
}

app.UseSerilogRequestLogging();

app.UseRouting();

// Correlation ID middleware
app.UseMiddleware<CorrelationIdMiddleware>();

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
