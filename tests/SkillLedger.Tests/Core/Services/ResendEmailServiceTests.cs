using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Resend;
using SkillLedger.Infrastructure.Services;
using Xunit;

namespace SkillLedger.Tests.Core.Services;

public class ResendEmailServiceTests : IDisposable
{
    private readonly Mock<IResend> _mockResend;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<ResendEmailService>> _mockLogger;

    public ResendEmailServiceTests()
    {
        _mockResend = new Mock<IResend>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<ResendEmailService>>();

        // Setup default valid configuration
        _mockConfiguration.Setup(x => x["EmailSettings:FromEmail"]).Returns("test@skillledger.app");
        _mockConfiguration.Setup(x => x["EmailSettings:FromDisplayName"]).Returns("SkillLedger Test");
    }

    [Fact]
    public void Constructor_WithNullResendClient_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new ResendEmailService(null!, _mockConfiguration.Object, _mockLogger.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("resend");
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new ResendEmailService(_mockResend.Object, null!, _mockLogger.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("configuration");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new ResendEmailService(_mockResend.Object, _mockConfiguration.Object, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithMissingFromEmail_ThrowsInvalidOperationException()
    {
        // Arrange
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(x => x["EmailSettings:FromEmail"]).Returns((string?)null);

        // Act & Assert
        var act = () => new ResendEmailService(_mockResend.Object, mockConfig.Object, _mockLogger.Object);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("EmailSettings:FromEmail not configured");
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_WithValidInput_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();
        _mockResend.Setup(x => x.EmailSendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(default(ResendResponse<Guid>)!);

        // Act
        var result = await service.SendWelcomeEmailAsync("user@example.com", "John Doe");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_WithNullEmail_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.SendWelcomeEmailAsync(null!, "John Doe");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_WithEmptyEmail_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.SendWelcomeEmailAsync("", "John Doe");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_WhenResendThrowsException_ReturnsFalseAndLogs()
    {
        // Arrange
        var service = CreateService();
        _mockResend.Setup(x => x.EmailSendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Resend API error"));

        // Act
        var result = await service.SendWelcomeEmailAsync("user@example.com", "John Doe");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_WithValidInput_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();
        _mockResend.Setup(x => x.EmailSendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(default(ResendResponse<Guid>)!);

        // Act
        var result = await service.SendPasswordResetEmailAsync("user@example.com", "John Doe", "reset-token-123", "https://skillledger.app");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_WithNullToken_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.SendPasswordResetEmailAsync("user@example.com", "John Doe", null!, "https://skillledger.app");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_WhenResendThrowsException_ReturnsFalseAndLogs()
    {
        // Arrange
        var service = CreateService();
        _mockResend.Setup(x => x.EmailSendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Resend API error"));

        // Act
        var result = await service.SendPasswordResetEmailAsync("user@example.com", "John Doe", "reset-token", "https://skillledger.app");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendEmailAsync_WithValidInput_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();
        _mockResend.Setup(x => x.EmailSendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(default(ResendResponse<Guid>)!);

        // Act
        var result = await service.SendEmailAsync("user@example.com", "Test Subject", "Test message content");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SendEmailAsync_WithEmptySubject_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.SendEmailAsync("user@example.com", "", "Test message");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendEmailAsync_WithEmptyMessage_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.SendEmailAsync("user@example.com", "Test Subject", "");

        // Assert
        result.Should().BeFalse();
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    private ResendEmailService CreateService()
    {
        return new ResendEmailService(_mockResend.Object, _mockConfiguration.Object, _mockLogger.Object);
    }
}
