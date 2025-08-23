namespace BffGateway.Application.Common.DTOs.Health;

public class HealthEntryDto
{
    public string Status { get; set; } = "Healthy";
    public string? Description { get; set; }
    public double DurationMs { get; set; }
}

public class HealthReportDto
{
    public string Status { get; set; } = "Healthy";
    public string? CorrelationId { get; set; }
    public double TotalDurationMs { get; set; }
    public Dictionary<string, HealthEntryDto> Entries { get; set; } = new();
}


