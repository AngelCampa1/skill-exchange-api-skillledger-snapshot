using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Infrastructure.Services;
using Xunit;

namespace SkillLedger.Tests.Unit.Services;

/// <summary>
/// Unit tests for CreditTransferService constructor validation.
/// Tests configuration-level error paths that cannot be tested in integration tests.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Service", "CreditTransferService")]
public class CreditTransferServiceConstructorTests
{
    [Fact]
    public void Constructor_WithShortSecretKey_ShouldThrowInvalidOperationException()
    {
        // Arrange - Create configuration with insufficient key length (< 32 bytes)
        var configDict = new Dictionary<string, string?>
        {
            { "CreditTransfer:ReceiptSecretKey", "TooShortKey123" }  // Only 14 characters, < 32 required
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        var options = new DbContextOptionsBuilder<SkillLedgerDbContext>()
            .UseInMemoryDatabase(databaseName: $"ConstructorTest_{Guid.NewGuid()}")
            .Options;
        var context = new SkillLedgerDbContext(options);
        var mockWalletService = new Mock<ICreditWalletService>();
        var mockLogger = new Mock<ILogger<CreditTransferService>>();
        var mockAuditLog = new Mock<IAuditLogService>();
        var mockLockService = new Mock<IDistributedLockService>();

        // Act & Assert - Constructor should throw due to key length validation (covers lines 59-61)
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new CreditTransferService(
                context,
                mockWalletService.Object,
                mockAuditLog.Object,
                mockLockService.Object,
                configuration,
                mockLogger.Object)
        );

        exception.Message.Should().Contain("minimum security requirements");
    }

    [Fact]
    public void Constructor_WithValidSecretKey_ShouldSucceed()
    {
        // Arrange - Create configuration with valid key length (>= 32 bytes)
        var configDict = new Dictionary<string, string?>
        {
            { "CreditTransfer:ReceiptSecretKey", "ThisIsASecureKeyThatMeetsTheMinimumRequirementOf32Bytes!" }  // 58 characters
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        var options = new DbContextOptionsBuilder<SkillLedgerDbContext>()
            .UseInMemoryDatabase(databaseName: $"ConstructorTest_{Guid.NewGuid()}")
            .Options;
        var context = new SkillLedgerDbContext(options);
        var mockWalletService = new Mock<ICreditWalletService>();
        var mockLogger = new Mock<ILogger<CreditTransferService>>();
        var mockAuditLog = new Mock<IAuditLogService>();
        var mockLockService = new Mock<IDistributedLockService>();

        // Act - Constructor should succeed
        var service = new CreditTransferService(
            context,
            mockWalletService.Object,
            mockAuditLog.Object,
            mockLockService.Object,
            configuration,
            mockLogger.Object);

        // Assert
        service.Should().NotBeNull();
    }
}
