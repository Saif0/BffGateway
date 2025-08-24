# Unit Testing Guide

This document describes the unit testing strategy and implementation for the BFF Gateway project.

## 🚀 Quick Start

```bash
# Make test script executable (first time only)
chmod +x scripts/run-tests.sh

# Run all tests
./scripts/run-tests.sh

# Or run tests manually
dotnet test

# Run specific test project
dotnet test tests/BffGateway.Application.Tests/

# Run specific test class
dotnet test --filter "FullyQualifiedName~SimpleCalculatorTests"

# Run with detailed output
dotnet test --verbosity normal

# Run with code coverage (if enabled)
dotnet test --collect:"XPlat Code Coverage"
```

## Overview

The project follows best practices for unit testing with a focus on simplicity and maintainability:

- **xUnit** as the testing framework
- **Moq** for mocking dependencies
- **FluentAssertions** for readable assertions
- **Arrange-Act-Assert (AAA)** pattern
- **Clear test naming** that describes the scenario and expected outcome

## Test Project Structure

```
tests/
├── BffGateway.Application.Tests/     # Tests for Application layer
│   ├── Commands/
│   │   ├── Auth/Login/
│   │   │   ├── LoginCommandHandlerTests.cs
│   │   │   └── LoginCommandValidatorTests.cs
│   │   └── Payments/CreatePayment/
│   │       └── CreatePaymentCommandHandlerTests.cs
├── BffGateway.Infrastructure.Tests/  # Tests for Infrastructure layer
│   └── Providers/
│       └── ProviderClientFactoryTests.cs
├── BffGateway.WebApi.Tests/         # Tests for WebApi layer
│   └── Services/
│       └── MessageServiceTests.cs
└── Directory.Build.props            # Common test configuration
```

## Running Tests

### All Tests

```bash
# Run all tests in the solution
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Specific Projects

```bash
# Run Application tests only
dotnet test tests/BffGateway.Application.Tests/

# Run Infrastructure tests only
dotnet test tests/BffGateway.Infrastructure.Tests/

# Run WebApi tests only
dotnet test tests/BffGateway.WebApi.Tests/
```

### Specific Test Classes

```bash
# Run specific test class
dotnet test --filter "FullyQualifiedName~LoginCommandHandlerTests"

# Run specific test method
dotnet test --filter "FullyQualifiedName~LoginCommandHandlerTests.Handle_SuccessfulLogin_ReturnsSuccessResponse"
```

## Test Coverage

The tests cover the following key areas:

### Application Layer

- **Command Handlers**: Business logic, error handling, provider integration
- **Validators**: Input validation rules and error messages
- **DTOs**: Data transformation and mapping

### Infrastructure Layer

- **Provider Factories**: Client creation and provider selection
- **HTTP Clients**: Integration with external services (mocked)

### WebApi Layer

- **Services**: Message localization and service dependencies
- **Controllers**: HTTP request/response handling (integration tests)

## Testing Patterns and Examples

### 1. Command Handler Testing

```csharp
[Fact]
public async Task Handle_SuccessfulLogin_ReturnsSuccessResponse()
{
    // Arrange
    var command = new LoginCommand("testuser", "password", SimulationScenario.None);
    var providerResponse = new ProviderAuthResponse(true, "token", DateTime.UtcNow, 200);

    _mockProviderClient
        .Setup(x => x.AuthenticateAsync(It.IsAny<ProviderAuthRequest>(), command.Scenario, It.IsAny<CancellationToken>()))
        .ReturnsAsync(providerResponse);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.IsSuccess.Should().BeTrue();
    result.Jwt.Should().Be("token");
}
```

### 2. Validator Testing

```csharp
[Theory]
[InlineData("")]
[InlineData(" ")]
[InlineData(null)]
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
```

### 3. Service Testing

```csharp
[Fact]
public void GetMessage_WithValidKey_ReturnsLocalizedMessage()
{
    // Arrange
    var key = "test.key";
    var expectedValue = "Test Message";
    var mockLocalizedString = new LocalizedString(key, expectedValue);

    _mockLocalizer.Setup(l => l[key]).Returns(mockLocalizedString);

    // Act
    var result = _messageService.GetMessage(key);

    // Assert
    result.Should().Be(expectedValue);
}
```

## Best Practices

### 1. Test Naming

- Use descriptive names: `MethodName_Scenario_ExpectedBehavior`
- Examples: `Handle_SuccessfulLogin_ReturnsSuccessResponse`

### 2. Test Structure

- Follow **Arrange-Act-Assert (AAA)** pattern
- Keep tests focused on single behavior
- Use clear comments to separate sections

### 3. Mocking

- Mock external dependencies (HTTP clients, databases, etc.)
- Don't mock the class under test
- Use `Mock.Of<T>()` for simple mocks, `new Mock<T>()` for complex setups

### 4. Assertions

- Use FluentAssertions for readable assertions
- Test both positive and negative scenarios
- Verify mock interactions when relevant

### 5. Test Data

- Use `[Theory]` and `[InlineData]` for parameterized tests
- Create meaningful test data that represents real scenarios
- Use realistic values, not just dummy data

## Key Test Scenarios Covered

### LoginCommandHandler

- ✅ Successful authentication
- ✅ Failed authentication
- ✅ Exception handling
- ✅ Parameter passing to provider
- ✅ Different simulation scenarios

### CreatePaymentCommandHandler

- ✅ Successful payment processing
- ✅ Failed payment processing
- ✅ Exception handling
- ✅ Parameter validation
- ✅ Simulation scenarios

### LoginCommandValidator

- ✅ Valid input validation
- ✅ Empty/null username validation
- ✅ Username length validation
- ✅ Empty/null password validation
- ✅ Multiple validation errors

### MessageService

- ✅ Message retrieval with key
- ✅ Message formatting with parameters
- ✅ Localizer factory interaction
- ✅ Edge cases (empty/null keys)

### ProviderClientFactory

- ✅ MockProvider client creation
- ✅ Case-insensitive provider keys
- ✅ Default provider selection
- ✅ Unsupported provider handling
- ✅ Parameter validation

## Continuous Integration

Tests are designed to run in CI/CD pipelines with:

- Fast execution (all tests complete in under 30 seconds)
- No external dependencies
- Deterministic results
- Clear failure messages

## Future Enhancements

Potential areas for test expansion:

- Integration tests for controllers
- Performance tests for critical paths
- Contract tests for external provider APIs
- End-to-end tests for complete user journeys
