using BffGateway.Application.Commands.Auth.Login;
using BffGateway.Application.Common.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace BffGateway.Application.Tests.Commands.Auth.Login;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator;

    public LoginCommandValidatorTests()
    {
        var mockMessageService = new Mock<BffGateway.Application.Abstractions.Services.IMessageService>();
        mockMessageService.Setup(x => x.GetMessage(It.IsAny<string>())).Returns((string key) => key);
        _validator = new LoginCommandValidator(mockMessageService.Object);
    }

    [Fact]
    public void Validate_ValidCommand_ShouldPass()
    {
        // Arrange
        var command = new LoginCommand("testuser", "Password123", SimulationScenario.None);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_EmptyUsername_ShouldFail(string username)
    {
        // Arrange
        var command = new LoginCommand(username, "password123", SimulationScenario.None);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginCommand.Username));
    }

    [Fact]
    public void Validate_UsernameTooLong_ShouldFail()
    {
        // Arrange
        var longUsername = new string('a', 101); // 101 characters, exceeds 100 limit
        var command = new LoginCommand(longUsername, "password123", SimulationScenario.None);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginCommand.Username));
    }

    [Fact]
    public void Validate_UsernameAtMaxLength_ShouldPass()
    {
        // Arrange
        var maxLengthUsername = new string('a', 100); // Exactly 100 characters
        var command = new LoginCommand(maxLengthUsername, "Password123", SimulationScenario.None);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_EmptyPassword_ShouldFail(string password)
    {
        // Arrange
        var command = new LoginCommand("testuser", password, SimulationScenario.None);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginCommand.Password));
    }

    [Theory]
    [InlineData("short1A")] // 7 chars
    public void Validate_PasswordTooShort_ShouldFail(string password)
    {
        // Arrange
        var command = new LoginCommand("testuser", password, SimulationScenario.None);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginCommand.Password));
    }

    [Fact]
    public void Validate_PasswordMissingUppercase_ShouldFail()
    {
        // Arrange
        var command = new LoginCommand("testuser", "password123", SimulationScenario.None);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginCommand.Password));
    }

    [Fact]
    public void Validate_PasswordMissingLowercase_ShouldFail()
    {
        // Arrange
        var command = new LoginCommand("testuser", "PASSWORD123", SimulationScenario.None);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginCommand.Password));
    }

    [Fact]
    public void Validate_PasswordMissingDigit_ShouldFail()
    {
        // Arrange
        var command = new LoginCommand("testuser", "Password", SimulationScenario.None);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginCommand.Password));
    }

    [Fact]
    public void Validate_BothUsernameAndPasswordInvalid_ShouldReturnBothErrors()
    {
        // Arrange
        var command = new LoginCommand("", "", SimulationScenario.None);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterOrEqualTo(2);
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginCommand.Username));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginCommand.Password));
    }

    [Theory]
    [InlineData(SimulationScenario.None)]
    [InlineData(SimulationScenario.Fail)]
    [InlineData(SimulationScenario.Timeout)]
    [InlineData(SimulationScenario.LimitExceeded)]
    public void Validate_AllSimulationScenarios_ShouldPass(SimulationScenario scenario)
    {
        // Arrange
        var command = new LoginCommand("testuser", "Password123", scenario);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
