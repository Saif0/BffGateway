using BffGateway.WebApi.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddCustomValidation();
builder.Services.AddCustomSwagger();
builder.Services.AddCustomObservability(builder.Configuration);
builder.Services.AddCustomLocalization(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseStaticFiles(); // Enable serving static files from wwwroot
app.UseCustomSwagger();
app.UseCustomLocalization();
app.UseCustomMiddleware();

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
