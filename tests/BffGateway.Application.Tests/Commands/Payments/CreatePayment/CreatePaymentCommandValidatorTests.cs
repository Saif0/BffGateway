using BffGateway.Application.Commands.Payments.CreatePayment;
using BffGateway.Application.Common.Enums;
using FluentAssertions;
using Xunit;

namespace BffGateway.Application.Tests.Commands.Payments.CreatePayment;

public class CreatePaymentCommandValidatorTests
{
    private readonly CreatePaymentCommandValidator _validator;

    public CreatePaymentCommandValidatorTests()
    {
        _validator = new CreatePaymentCommandValidator();
    }

    [Fact]
    public void Validate_ValidCommand_ShouldPass()
    {
        // Arrange
        var command = new CreatePaymentCommand(100.50m, "USD", "ACC123456", SimulationScenario.None);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.50)]
    public void Validate_AmountZeroOrNegative_ShouldFail(decimal amount)
    {
        // Arrange
        var command = new CreatePaymentCommand(amount, "USD", "ACC123456", SimulationScenario.None);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePaymentCommand.Amount));
    }

    [Fact]
    public void Validate_AmountTooHigh_ShouldFail()
    {
        // Arrange
        var command = new CreatePaymentCommand(1000001m, "USD", "ACC123456", SimulationScenario.None);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePaymentCommand.Amount));
    }

    [Fact]
    public void Validate_AmountAtMaxValue_ShouldPass()
    {
        // Arrange
        var command = new CreatePaymentCommand(1000000m, "USD", "ACC123456", SimulationScenario.None);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("GBP")]
    [InlineData("CAD")]
    [InlineData("AUD")]
    public void Validate_SupportedCurrencies_ShouldPass(string currency)
    {
        // Arrange
        var command = new CreatePaymentCommand(100m, currency, "ACC123456", SimulationScenario.None);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("JPY")]
    [InlineData("BTC")]
    [InlineData("INVALID")]
    public void Validate_InvalidCurrency_ShouldFail(string currency)
    {
        // Arrange
        var command = new CreatePaymentCommand(100m, currency, "ACC123456", SimulationScenario.None);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePaymentCommand.Currency));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_EmptyDestinationAccount_ShouldFail(string destinationAccount)
    {
        // Arrange
        var command = new CreatePaymentCommand(100m, "USD", destinationAccount, SimulationScenario.None);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePaymentCommand.DestinationAccount));
    }

    [Fact]
    public void Validate_DestinationAccountTooLong_ShouldFail()
    {
        // Arrange
        var longAccount = new string('A', 51); // 51 characters, exceeds 50 limit
        var command = new CreatePaymentCommand(100m, "USD", longAccount, SimulationScenario.None);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePaymentCommand.DestinationAccount));
    }

    [Fact]
    public void Validate_DestinationAccountAtMaxLength_ShouldPass()
    {
        // Arrange
        var maxLengthAccount = new string('A', 50); // Exactly 50 characters
        var command = new CreatePaymentCommand(100m, "USD", maxLengthAccount, SimulationScenario.None);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData(SimulationScenario.None)]
    [InlineData(SimulationScenario.Fail)]
    [InlineData(SimulationScenario.Timeout)]
    [InlineData(SimulationScenario.LimitExceeded)]
    public void Validate_AllSimulationScenarios_ShouldPass(SimulationScenario scenario)
    {
        // Arrange
        var command = new CreatePaymentCommand(100m, "USD", "ACC123456", scenario);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_MultipleValidationErrors_ShouldReturnAllErrors()
    {
        // Arrange
        var command = new CreatePaymentCommand(-100m, "", "", SimulationScenario.None);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterOrEqualTo(3);
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePaymentCommand.Amount));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePaymentCommand.Currency));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePaymentCommand.DestinationAccount));
    }

    [Theory]
    [InlineData("usd")] // lowercase
    [InlineData("Usd")] // mixed case
    [InlineData("USD")] // uppercase
    public void Validate_CurrencyCaseInsensitive_ShouldPass(string currency)
    {
        // Arrange
        var command = new CreatePaymentCommand(100m, currency, "ACC123456", SimulationScenario.None);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
