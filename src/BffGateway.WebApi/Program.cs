using BffGateway.WebApi.Extensions;
using Serilog;
using Serilog.Sinks.OpenTelemetry;
using System.Collections.Generic;

var builder = WebApplication.CreateBuilder(args);


// // Configure Serilog
// Log.Logger = new LoggerConfiguration()
//     .ReadFrom.Configuration(builder.Configuration)
//     .Enrich.FromLogContext()
//     .CreateLogger();

// Configure Serilog with OTLP sink using Observability settings
var otlpEndpoint = builder.Configuration.GetValue<string>("Observability:Otlp:Endpoint") ?? "http://localhost:18889";
var otlpProtocolSetting = builder.Configuration.GetValue<string>("Observability:Otlp:Protocol") ?? "Grpc";
var otlpProtocol = string.Equals(otlpProtocolSetting, "HttpProtobuf", StringComparison.OrdinalIgnoreCase)
    ? OtlpProtocol.HttpProtobuf
    : OtlpProtocol.Grpc;

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.OpenTelemetry(options =>
    {
        options.Endpoint = otlpEndpoint;
        options.Protocol = otlpProtocol;
        options.ResourceAttributes = new Dictionary<string, object>
        {
            ["service.name"] = "BffGateway"
        };
    })
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
// app.UseStaticFiles(); // Enable serving static files from wwwroot
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
