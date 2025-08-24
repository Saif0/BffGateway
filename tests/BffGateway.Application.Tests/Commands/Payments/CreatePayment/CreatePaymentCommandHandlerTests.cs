using BffGateway.Application.Abstractions.Providers;
using BffGateway.Application.Abstractions.Services;
using BffGateway.Application.Commands.Payments.CreatePayment;
using BffGateway.Application.Common.DTOs.Payment;
using BffGateway.Application.Common.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BffGateway.Application.Tests.Commands.Payments.CreatePayment;

public class CreatePaymentCommandHandlerTests
{
    private readonly Mock<IProviderClient> _mockProviderClient;
    private readonly Mock<ILogger<CreatePaymentCommandHandler>> _mockLogger;
    private readonly Mock<IMessageService> _mockMessageService;
    private readonly CreatePaymentCommandHandler _handler;

    public CreatePaymentCommandHandlerTests()
    {
        _mockProviderClient = new Mock<IProviderClient>();
        _mockLogger = new Mock<ILogger<CreatePaymentCommandHandler>>();
        _mockMessageService = new Mock<IMessageService>();

        // Setup default message service responses
        _mockMessageService.Setup(x => x.GetMessage(It.IsAny<string>()))
            .Returns("Test message");

        _handler = new CreatePaymentCommandHandler(
            _mockProviderClient.Object,
            _mockLogger.Object,
            _mockMessageService.Object);
    }

    [Fact]
    public async Task Handle_SuccessfulPayment_ReturnsSuccessResponse()
    {
        // Arrange
        var command = new CreatePaymentCommand(100.50m, "USD", "ACC123456", SimulationScenario.None);
        var providerResponse = new ProviderPaymentResponse(
            Success: true,
            TransactionId: "TXN123",
            ProviderRef: "REF456",
            ProcessedAt: DateTime.UtcNow,
            StatusCode: 200);

        _mockProviderClient
            .Setup(x => x.ProcessPaymentAsync(It.IsAny<ProviderPaymentRequest>(), It.IsAny<SimulationScenario>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(providerResponse);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.PaymentId.Should().Be("TXN123");
        result.ProviderReference.Should().Be("REF456");
        result.ProcessedAt.Should().NotBeNull();
        result.UpstreamStatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Handle_FailedPayment_ReturnsFailureResponse()
    {
        // Arrange
        var command = new CreatePaymentCommand(100.50m, "USD", "ACC123456", SimulationScenario.None);
        var providerResponse = new ProviderPaymentResponse(
            Success: false,
            TransactionId: "",
            ProviderRef: "",
            ProcessedAt: DateTime.UtcNow,
            StatusCode: 400);

        _mockProviderClient
            .Setup(x => x.ProcessPaymentAsync(It.IsAny<ProviderPaymentRequest>(), It.IsAny<SimulationScenario>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(providerResponse);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.PaymentId.Should().BeNull();
        result.ProviderReference.Should().BeNull();
        result.ProcessedAt.Should().BeNull();
        result.UpstreamStatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_ProviderClientThrowsException_ReturnsErrorResponse()
    {
        // Arrange
        var command = new CreatePaymentCommand(100.50m, "USD", "ACC123456", SimulationScenario.None);

        _mockProviderClient
            .Setup(x => x.ProcessPaymentAsync(It.IsAny<ProviderPaymentRequest>(), It.IsAny<SimulationScenario>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Provider error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.PaymentId.Should().BeNull();
        result.ProviderReference.Should().BeNull();
        result.ProcessedAt.Should().BeNull();
        result.UpstreamStatusCode.Should().Be(500);
    }

    [Fact]
    public async Task Handle_CorrectProviderRequestIsSent()
    {
        // Arrange
        var command = new CreatePaymentCommand(250.75m, "EUR", "ACCOUNT789", SimulationScenario.Timeout);
        var providerResponse = new ProviderPaymentResponse(true, "TXN", "REF", DateTime.UtcNow, 200);

        _mockProviderClient
            .Setup(x => x.ProcessPaymentAsync(It.IsAny<ProviderPaymentRequest>(), It.IsAny<SimulationScenario>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(providerResponse);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockProviderClient.Verify(x => x.ProcessPaymentAsync(
            It.Is<ProviderPaymentRequest>(r =>
                r.Total == 250.75m &&
                r.Curr == "EUR" &&
                r.Dest == "ACCOUNT789"),
            SimulationScenario.Timeout,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(SimulationScenario.None)]
    [InlineData(SimulationScenario.Fail)]
    [InlineData(SimulationScenario.Timeout)]
    [InlineData(SimulationScenario.LimitExceeded)]
    public async Task Handle_AllSimulationScenarios_ArePassedToProvider(SimulationScenario scenario)
    {
        // Arrange
        var command = new CreatePaymentCommand(100m, "USD", "ACC123", scenario);
        var providerResponse = new ProviderPaymentResponse(true, "TXN", "REF", DateTime.UtcNow, 200);

        _mockProviderClient
            .Setup(x => x.ProcessPaymentAsync(It.IsAny<ProviderPaymentRequest>(), It.IsAny<SimulationScenario>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(providerResponse);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockProviderClient.Verify(x => x.ProcessPaymentAsync(
            It.IsAny<ProviderPaymentRequest>(),
            scenario,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_LogsInformationMessages()
    {
        // Arrange
        var command = new CreatePaymentCommand(100m, "USD", "ACC123", SimulationScenario.None);
        var providerResponse = new ProviderPaymentResponse(true, "TXN", "REF", DateTime.UtcNow, 200);

        _mockProviderClient
            .Setup(x => x.ProcessPaymentAsync(It.IsAny<ProviderPaymentRequest>(), It.IsAny<SimulationScenario>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(providerResponse);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Processing payment request")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}
