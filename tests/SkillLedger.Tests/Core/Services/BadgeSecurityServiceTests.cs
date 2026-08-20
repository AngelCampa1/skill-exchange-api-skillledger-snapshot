using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Infrastructure.Configuration;
using SkillLedger.Infrastructure.Services;
using Xunit;

namespace SkillLedger.Tests.Core.Services;

public class BadgeSecurityServiceTests
{
    private readonly BadgeSecurityService _securityService;
    private readonly Mock<ILogger<BadgeSecurityService>> _mockLogger;
    private readonly IOptions<BadgeSecurityConfiguration> _config;

    public BadgeSecurityServiceTests()
    {
        _mockLogger = new Mock<ILogger<BadgeSecurityService>>();
        // BUG-NEW-007 FIX: Provide configuration for BadgeSecurityService
        _config = Options.Create(new BadgeSecurityConfiguration
        {
            SecretKey = "TestSecretKey_ForUnitTests_2024",
            VerificationCodeExpiryHours = 24
        });
        _securityService = new BadgeSecurityService(_mockLogger.Object, _config);
    }

    [Fact]
    public async Task GenerateBadgeHashAsync_ValidBadge_ReturnsConsistentHash()
    {
        // Arrange
        var badge = CreateTestBadge();

        // Act
        var hash1 = await _securityService.GenerateBadgeHashAsync(badge);
        var hash2 = await _securityService.GenerateBadgeHashAsync(badge);

        // Assert
        Assert.NotNull(hash1);
        Assert.NotEmpty(hash1);
        Assert.Equal(hash1, hash2); // Same badge should produce same hash
    }

    [Fact]
    public async Task GenerateBadgeHashAsync_DifferentBadges_ReturnsDifferentHashes()
    {
        // Arrange
        var badge1 = CreateTestBadge();
        var badge2 = CreateTestBadge();
        badge2.UserId = Guid.NewGuid(); // Different user

        // Act
        var hash1 = await _securityService.GenerateBadgeHashAsync(badge1);
        var hash2 = await _securityService.GenerateBadgeHashAsync(badge2);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public async Task ValidateBadgeIntegrityAsync_ValidHash_ReturnsTrue()
    {
        // Arrange
        var badge = CreateTestBadge();
        badge.IntegrityHash = await _securityService.GenerateBadgeHashAsync(badge);

        // Act
        var isValid = await _securityService.ValidateBadgeIntegrityAsync(badge);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public async Task ValidateBadgeIntegrityAsync_InvalidHash_ReturnsFalse()
    {
        // Arrange
        var badge = CreateTestBadge();
        badge.IntegrityHash = "invalid-hash";

        // Act
        var isValid = await _securityService.ValidateBadgeIntegrityAsync(badge);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task ValidateBadgeIntegrityAsync_NoHash_ReturnsFalse()
    {
        // Arrange
        var badge = CreateTestBadge();
        badge.IntegrityHash = null;

        // Act
        var isValid = await _securityService.ValidateBadgeIntegrityAsync(badge);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task ValidateBadgeIntegrityAsync_TamperedBadge_ReturnsFalse()
    {
        // Arrange
        var badge = CreateTestBadge();
        badge.IntegrityHash = await _securityService.GenerateBadgeHashAsync(badge);

        // Tamper with the badge
        badge.BadgeType = "TAMPERED_BADGE";

        // Act
        var isValid = await _securityService.ValidateBadgeIntegrityAsync(badge);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task GenerateVerificationCodeAsync_ValidInput_ReturnsFormattedCode()
    {
        // Arrange
        var badgeId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Act
        var code = await _securityService.GenerateVerificationCodeAsync(badgeId, userId);

        // Assert
        Assert.NotNull(code);
        Assert.Contains("-", code); // Should contain timestamp separator
        Assert.True(code.Length > 10); // Should be reasonable length
    }

    [Fact]
    public async Task GenerateVerificationCodeAsync_DifferentInputs_GeneratesDifferentCodes()
    {
        // Arrange
        var badgeId1 = Guid.NewGuid();
        var badgeId2 = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Act
        var code1 = await _securityService.GenerateVerificationCodeAsync(badgeId1, userId);
        var code2 = await _securityService.GenerateVerificationCodeAsync(badgeId2, userId);

        // Assert
        Assert.NotEqual(code1, code2);
    }

    [Fact]
    public async Task VerifyBadgeCodeAsync_ValidRecentCode_ReturnsTrue()
    {
        // Arrange
        var badgeId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var code = await _securityService.GenerateVerificationCodeAsync(badgeId, userId);

        // Act
        var isValid = await _securityService.VerifyBadgeCodeAsync(badgeId, code);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public async Task VerifyBadgeCodeAsync_ArbitraryHashWithCurrentTimestamp_ReturnsFalse()
    {
        // Arrange
        var badgeId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var forgedCode = $"anything-{timestamp}";

        // Act
        var isValid = await _securityService.VerifyBadgeCodeAsync(badgeId, forgedCode);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task VerifyBadgeCodeAsync_InvalidFormat_ReturnsFalse()
    {
        // Arrange
        var badgeId = Guid.NewGuid();
        var invalidCode = "invalid-format";

        // Act
        var isValid = await _securityService.VerifyBadgeCodeAsync(badgeId, invalidCode);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task VerifyBadgeCodeAsync_EmptyCode_ReturnsFalse()
    {
        // Arrange
        var badgeId = Guid.NewGuid();

        // Act
        var isValid1 = await _securityService.VerifyBadgeCodeAsync(badgeId, "");
        var isValid2 = await _securityService.VerifyBadgeCodeAsync(badgeId, null!);

        // Assert
        Assert.False(isValid1);
        Assert.False(isValid2);
    }

    [Fact]
    public async Task EncryptBadgeDataAsync_ValidData_ReturnsEncryptedString()
    {
        // Arrange
        var testData = "sensitive badge information";

        // Act
        var encrypted = await _securityService.EncryptBadgeDataAsync(testData);

        // Assert
        Assert.NotNull(encrypted);
        Assert.NotEmpty(encrypted);
        Assert.NotEqual(testData, encrypted);
    }

    [Fact]
    public async Task DecryptBadgeDataAsync_EncryptedData_ReturnsOriginalData()
    {
        // Arrange
        var originalData = "sensitive badge information";
        var encrypted = await _securityService.EncryptBadgeDataAsync(originalData);

        // Act
        var decrypted = await _securityService.DecryptBadgeDataAsync(encrypted);

        // Assert
        Assert.Equal(originalData, decrypted);
    }

    [Fact]
    public async Task EncryptDecryptBadgeDataAsync_EmptyString_HandlesGracefully()
    {
        // Arrange
        var emptyData = "";

        // Act
        var encrypted = await _securityService.EncryptBadgeDataAsync(emptyData);
        var decrypted = await _securityService.DecryptBadgeDataAsync(encrypted);

        // Assert
        Assert.Equal(emptyData, decrypted);
    }

    [Fact]
    public async Task EncryptDecryptBadgeDataAsync_JsonData_PreservesStructure()
    {
        // Arrange
        var jsonData = "{\"documentType\":\"passport\",\"verified\":true,\"timestamp\":\"2024-01-01T12:00:00Z\"}";

        // Act
        var encrypted = await _securityService.EncryptBadgeDataAsync(jsonData);
        var decrypted = await _securityService.DecryptBadgeDataAsync(encrypted);

        // Assert
        Assert.Equal(jsonData, decrypted);
    }

    [Fact]
    public async Task DecryptBadgeDataAsync_InvalidEncryptedData_ThrowsException()
    {
        // Arrange
        var invalidEncrypted = "not-encrypted-data";

        // Act & Assert
        await Assert.ThrowsAsync<FormatException>(() =>
            _securityService.DecryptBadgeDataAsync(invalidEncrypted));
    }

    private UserBadge CreateTestBadge()
    {
        return new UserBadge
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            BadgeType = "TEST_BADGE",
            BadgeName = "Test Badge",
            BadgeDescription = "A test badge for unit testing",
            Category = BadgeCategory.Performance,
            EarnedAt = DateTime.UtcNow,
            IsActive = true,
            VerificationLevel = VerificationLevel.Automatic,
            VerificationEvidence = "{\"test\":\"evidence\"}"
        };
    }
}
