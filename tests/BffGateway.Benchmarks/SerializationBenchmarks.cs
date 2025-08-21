using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace BffGateway.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class SerializationBenchmarks
{
    private readonly LoginRequest _loginRequest;
    private readonly PaymentRequest _paymentRequest;
    private readonly JsonSerializerOptions _jsonOptions;
    private string _loginJson = string.Empty;
    private string _paymentJson = string.Empty;

    public SerializationBenchmarks()
    {
        _loginRequest = new LoginRequest("testuser", "password123");
        _paymentRequest = new PaymentRequest(100.50m, "USD", "ACC123456");
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    [GlobalSetup]
    public void Setup()
    {
        _loginJson = JsonSerializer.Serialize(_loginRequest, _jsonOptions);
        _paymentJson = JsonSerializer.Serialize(_paymentRequest, _jsonOptions);
    }

    [Benchmark]
    public string SerializeLoginRequest()
    {
        return JsonSerializer.Serialize(_loginRequest, _jsonOptions);
    }

    [Benchmark]
    public LoginRequest DeserializeLoginRequest()
    {
        return JsonSerializer.Deserialize<LoginRequest>(_loginJson, _jsonOptions)!;
    }

    [Benchmark]
    public string SerializePaymentRequest()
    {
        return JsonSerializer.Serialize(_paymentRequest, _jsonOptions);
    }

    [Benchmark]
    public PaymentRequest DeserializePaymentRequest()
    {
        return JsonSerializer.Deserialize<PaymentRequest>(_paymentJson, _jsonOptions)!;
    }
}

public record LoginRequest(string Username, string Password);
public record PaymentRequest(decimal Amount, string Currency, string DestinationAccount);
