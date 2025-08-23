using System.Text;
using System.Linq;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;

static string Truncate(string value, int maxLength)
{
    if (string.IsNullOrEmpty(value)) return value ?? string.Empty;
    return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<MockProvider.LatencyOptions>(builder.Configuration.GetSection("Latency"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Centralized inbound request logging
app.Use(async (context, next) =>
{
    var request = context.Request;
    var sw = Stopwatch.StartNew();

    string headersText = string.Join(
        "; ",
        request.Headers.Select(h => $"{h.Key}: {Truncate(h.Value.ToString(), 512)}"));

    string bodyText = string.Empty;
    try
    {
        request.EnableBuffering();
        if (request.ContentLength.HasValue && request.ContentLength.Value > 0 && request.Body.CanRead)
        {
            request.Body.Position = 0;
            using var reader = new StreamReader(request.Body, Encoding.UTF8, false, 1024, true);
            bodyText = await reader.ReadToEndAsync();
            request.Body.Position = 0;
        }
    }
    catch { /* ignore body read errors for logging */ }

    app.Logger.LogInformation(
        "Inbound {Method} {Path}{Query} | Headers: {Headers} | Body: {Body}",
        request.Method,
        request.Path,
        request.QueryString,
        headersText,
        Truncate(bodyText, 4000));

    await next();
    sw.Stop();

    app.Logger.LogInformation(
        "Outbound {StatusCode} for {Method} {Path}{Query} in {ElapsedMs} ms",
        context.Response.StatusCode,
        request.Method,
        request.Path,
        request.QueryString,
        sw.ElapsedMilliseconds);
});

app.UseAuthorization();
app.MapControllers();

// Lightweight ping endpoint for health checks
app.MapGet("/api/ping", () => Results.Ok(new { ok = true, ts = DateTime.UtcNow }))
   .WithName("Ping")
   .WithOpenApi();

app.Run();
