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

// Configure Serilog enable/disable and OTLP sink using Observability settings
var enableSerilog = builder.Configuration.GetValue<bool>("Observability:EnableSerilog");
if (enableSerilog)
{
    var enableSerilogOtlpSink = builder.Configuration.GetValue<bool>("Observability:EnableSerilogOtlpSink");
    OtlpProtocol? otlpProtocol = null;
    string? otlpEndpoint = null;

    if (enableSerilogOtlpSink)
    {
        otlpEndpoint = builder.Configuration.GetValue<string>("Observability:Otlp:Endpoint");
        if (string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            throw new InvalidOperationException("Missing required configuration 'Observability:Otlp:Endpoint' when Serilog OTLP sink is enabled.");
        }

        var otlpProtocolSetting = builder.Configuration.GetValue<string>("Observability:Otlp:Protocol");
        if (string.IsNullOrWhiteSpace(otlpProtocolSetting))
        {
            throw new InvalidOperationException("Missing required configuration 'Observability:Otlp:Protocol' when Serilog OTLP sink is enabled. Allowed values: 'Grpc' or 'HttpProtobuf'.");
        }

        otlpProtocol = string.Equals(otlpProtocolSetting, "HttpProtobuf", StringComparison.OrdinalIgnoreCase)
            ? OtlpProtocol.HttpProtobuf
            : string.Equals(otlpProtocolSetting, "Grpc", StringComparison.OrdinalIgnoreCase)
                ? OtlpProtocol.Grpc
                : throw new InvalidOperationException("Invalid value for 'Observability:Otlp:Protocol'. Allowed values: 'Grpc' or 'HttpProtobuf'.");
    }

    var loggerConfiguration = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext();

    if (enableSerilogOtlpSink)
    {
        loggerConfiguration = loggerConfiguration.WriteTo.OpenTelemetry(options =>
        {
            options.Endpoint = otlpEndpoint!;
            options.Protocol = otlpProtocol!.Value;
            options.ResourceAttributes = new Dictionary<string, object>
            {
                ["service.name"] = "BffGateway"
            };
        });
    }

    Log.Logger = loggerConfiguration.CreateLogger();
    builder.Host.UseSerilog();
}

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
