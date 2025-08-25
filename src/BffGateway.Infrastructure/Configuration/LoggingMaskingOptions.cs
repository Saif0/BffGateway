namespace BffGateway.Infrastructure.Configuration;

public class LoggingMaskingOptions
{
    public const string SectionName = "LoggingMasking";

    public List<string> SensitiveHeaders { get; init; } = new()
    {
        "Authorization", "Cookie", "Set-Cookie", "X-API-Key",
        "Authentication", "Proxy-Authorization", "WWW-Authenticate"
    };

    public List<string> SensitiveBodyFields { get; init; } = new()
    {
        "password", "pwd", "token", "secret", "key", "authorization",
        "cardNumber", "cvv", "pin", "ssn", "creditCard"
    };

    public int MaxBodySize { get; init; } = 8192; // bytes
}


