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

app.UseAuthorization();
app.MapControllers();

// Lightweight ping endpoint for health checks
app.MapGet("/api/ping", () => Results.Ok(new { ok = true, ts = DateTime.UtcNow }))
   .WithName("Ping")
   .WithOpenApi();

app.Run();
