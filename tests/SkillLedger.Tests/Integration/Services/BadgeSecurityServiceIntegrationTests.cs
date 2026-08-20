using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Infrastructure.Configuration;
using SkillLedger.Infrastructure.Services;
using SkillLedger.Tests.Infrastructure;
using Xunit;

namespace SkillLedger.Tests.Integration.Services;

/// <summary>
/// Integration tests for BadgeSecurityService - BADGE SECURITY AND INTEGRITY.
///
/// Pattern (per TDD_GUIDE.md):
/// - No external dependencies to mock
/// - Tests cryptographic operations: hashing, encryption, verification
/// - Validates badge integrity and security features
///
/// Max mocked external dependencies: 0
/// </summary>
[IntegrationTest]
public class BadgeSecurityServiceIntegrationTests
{
    private readonly BadgeSecurityService _service;
    private readonly BadgeSecurityConfiguration _config;

    // Test data
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _badgeId = Guid.NewGuid();

    public BadgeSecurityServiceIntegrationTests()
    {
        _config = new BadgeSecurityConfiguration
        {
            SecretKey = "TestSecretKey12345678901234567890", // 32+ chars for testing
            VerificationCodeExpiryHours = 24
        };

        var logger = new LoggerFactory().CreateLogger<BadgeSecurityService>();
        var options = Options.Create(_config);

        _service = new BadgeSecurityService(logger, options);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_MissingSecretKey_ThrowsException()
    {
        // Arrange
        var invalidConfig = new BadgeSecurityConfiguration
        {
            SecretKey = "", // Empty secret key
            VerificationCodeExpiryHours = 24
        };
        var logger = new LoggerFactory().CreateLogger<BadgeSecurityService>();

        // Act & Assert
        var action = () => new BadgeSecurityService(logger, Options.Create(invalidConfig));
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*secret key*not configured*");
    }

    [Fact]
    public void Constructor_WhitespaceSecretKey_ThrowsException()
    {
        // Arrange
        var invalidConfig = new BadgeSecurityConfiguration
        {
            SecretKey = "   ", // Whitespace only
            VerificationCodeExpiryHours = 24
        };
        var logger = new LoggerFactory().CreateLogger<BadgeSecurityService>();

        // Act & Assert
        var action = () => new BadgeSecurityService(logger, Options.Create(invalidConfig));
        action.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region GenerateBadgeHashAsync Tests

    [Fact]
    public async Task GenerateBadgeHashAsync_ValidBadge_ReturnsHash()
    {
        // Arrange
        var badge = CreateTestBadge();

        // Act
        var hash = await _service.GenerateBadgeHashAsync(badge);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().HaveLength(44); // Base64 of SHA256 is 44 chars
    }

    [Fact]
    public async Task GenerateBadgeHashAsync_SameBadge_ReturnsSameHash()
    {
        // Arrange
        var badge = CreateTestBadge();

        // Act
        var hash1 = await _service.GenerateBadgeHashAsync(badge);
        var hash2 = await _service.GenerateBadgeHashAsync(badge);

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public async Task GenerateBadgeHashAsync_DifferentUserId_ReturnsDifferentHash()
    {
        // Arrange
        var badge1 = CreateTestBadge();
        var badge2 = CreateTestBadge();
        badge2.UserId = Guid.NewGuid(); // Different user

        // Act
        var hash1 = await _service.GenerateBadgeHashAsync(badge1);
        var hash2 = await _service.GenerateBadgeHashAsync(badge2);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public async Task GenerateBadgeHashAsync_DifferentBadgeType_ReturnsDifferentHash()
    {
        // Arrange
        var badge1 = CreateTestBadge();
        var badge2 = CreateTestBadge();
        badge2.BadgeType = "DIFFERENT_TYPE";

        // Act
        var hash1 = await _service.GenerateBadgeHashAsync(badge1);
        var hash2 = await _service.GenerateBadgeHashAsync(badge2);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public async Task GenerateBadgeHashAsync_DifferentEarnedAt_ReturnsDifferentHash()
    {
        // Arrange
        var badge1 = CreateTestBadge();
        var badge2 = CreateTestBadge();
        badge2.EarnedAt = badge1.EarnedAt.AddSeconds(1); // 1 second difference

        // Act
        var hash1 = await _service.GenerateBadgeHashAsync(badge1);
        var hash2 = await _service.GenerateBadgeHashAsync(badge2);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public async Task GenerateBadgeHashAsync_DifferentVerificationEvidence_ReturnsDifferentHash()
    {
        // Arrange
        var badge1 = CreateTestBadge();
        badge1.VerificationEvidence = "Evidence A";
        var badge2 = CreateTestBadge();
        badge2.VerificationEvidence = "Evidence B";

        // Act
        var hash1 = await _service.GenerateBadgeHashAsync(badge1);
        var hash2 = await _service.GenerateBadgeHashAsync(badge2);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public async Task GenerateBadgeHashAsync_NullVerificationEvidence_HandlesCorrectly()
    {
        // Arrange
        var badge = CreateTestBadge();
        badge.VerificationEvidence = null;

        // Act
        var hash = await _service.GenerateBadgeHashAsync(badge);

        // Assert
        hash.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region ValidateBadgeIntegrityAsync Tests

    [Fact]
    public async Task ValidateBadgeIntegrityAsync_ValidHash_ReturnsTrue()
    {
        // Arrange
        var badge = CreateTestBadge();
        badge.IntegrityHash = await _service.GenerateBadgeHashAsync(badge);

        // Act
        var isValid = await _service.ValidateBadgeIntegrityAsync(badge);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateBadgeIntegrityAsync_TamperedBadge_ReturnsFalse()
    {
        // Arrange
        var badge = CreateTestBadge();
        badge.IntegrityHash = await _service.GenerateBadgeHashAsync(badge);

        // Tamper with the badge after hash was generated
        badge.BadgeType = "TAMPERED_TYPE";

        // Act
        var isValid = await _service.ValidateBadgeIntegrityAsync(badge);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateBadgeIntegrityAsync_NullHash_ReturnsFalse()
    {
        // Arrange
        var badge = CreateTestBadge();
        badge.IntegrityHash = null;

        // Act
        var isValid = await _service.ValidateBadgeIntegrityAsync(badge);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateBadgeIntegrityAsync_EmptyHash_ReturnsFalse()
    {
        // Arrange
        var badge = CreateTestBadge();
        badge.IntegrityHash = "";

        // Act
        var isValid = await _service.ValidateBadgeIntegrityAsync(badge);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateBadgeIntegrityAsync_InvalidHash_ReturnsFalse()
    {
        // Arrange
        var badge = CreateTestBadge();
        badge.IntegrityHash = "InvalidHashValue123";

        // Act
        var isValid = await _service.ValidateBadgeIntegrityAsync(badge);

        // Assert
        isValid.Should().BeFalse();
    }

    #endregion

    #region GenerateVerificationCodeAsync Tests

    [Fact]
    public async Task GenerateVerificationCodeAsync_ValidInput_ReturnsCode()
    {
        // Act
        var code = await _service.GenerateVerificationCodeAsync(_badgeId, _userId);

        // Assert
        code.Should().NotBeNullOrEmpty();
        code.Should().Contain("-"); // Code format includes timestamp separator
    }

    [Fact]
    public async Task GenerateVerificationCodeAsync_ContainsTimestamp()
    {
        // Act
        var code = await _service.GenerateVerificationCodeAsync(_badgeId, _userId);

        // Assert
        var parts = code.Split('-');
        parts.Should().HaveCount(2);
        long.TryParse(parts[1], out var timestamp).Should().BeTrue();
        timestamp.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GenerateVerificationCodeAsync_DifferentBadgeId_ReturnsDifferentCode()
    {
        // Act
        var code1 = await _service.GenerateVerificationCodeAsync(Guid.NewGuid(), _userId);
        var code2 = await _service.GenerateVerificationCodeAsync(Guid.NewGuid(), _userId);

        // Assert
        var hash1 = code1.Split('-')[0];
        var hash2 = code2.Split('-')[0];
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public async Task GenerateVerificationCodeAsync_DifferentUserId_ReturnsDifferentCode()
    {
        // Act
        var code1 = await _service.GenerateVerificationCodeAsync(_badgeId, Guid.NewGuid());
        var code2 = await _service.GenerateVerificationCodeAsync(_badgeId, Guid.NewGuid());

        // Assert
        var hash1 = code1.Split('-')[0];
        var hash2 = code2.Split('-')[0];
        hash1.Should().NotBe(hash2);
    }

    #endregion

    #region VerifyBadgeCodeAsync Tests

    [Fact]
    public async Task VerifyBadgeCodeAsync_ValidCode_ReturnsTrue()
    {
        // Arrange
        var code = await _service.GenerateVerificationCodeAsync(_badgeId, _userId);

        // Act
        var isValid = await _service.VerifyBadgeCodeAsync(_badgeId, code);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyBadgeCodeAsync_NullCode_ReturnsFalse()
    {
        // Act
        var isValid = await _service.VerifyBadgeCodeAsync(_badgeId, null!);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyBadgeCodeAsync_EmptyCode_ReturnsFalse()
    {
        // Act
        var isValid = await _service.VerifyBadgeCodeAsync(_badgeId, "");

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyBadgeCodeAsync_MalformedCode_ReturnsFalse()
    {
        // Act
        var isValid = await _service.VerifyBadgeCodeAsync(_badgeId, "no-separator-here");

        // Assert - The code has separator but second part is not a timestamp
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyBadgeCodeAsync_InvalidTimestamp_ReturnsFalse()
    {
        // Act
        var isValid = await _service.VerifyBadgeCodeAsync(_badgeId, "hash-notanumber");

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyBadgeCodeAsync_ExpiredCode_ReturnsFalse()
    {
        // Arrange - Create a code with an old timestamp (25 hours ago, beyond 24 hour expiry)
        var oldTimestamp = DateTimeOffset.UtcNow.AddHours(-25).ToUnixTimeSeconds();
        var expiredCode = $"somehash-{oldTimestamp}";

        // Act
        var isValid = await _service.VerifyBadgeCodeAsync(_badgeId, expiredCode);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyBadgeCodeAsync_ArbitraryRecentCode_ReturnsFalse()
    {
        // Arrange - Create a code with a recent timestamp (1 hour ago)
        var recentTimestamp = DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds();
        var recentCode = $"somehash-{recentTimestamp}";

        // Act
        var isValid = await _service.VerifyBadgeCodeAsync(_badgeId, recentCode);

        // Assert
        isValid.Should().BeFalse();
    }

    #endregion

    #region EncryptBadgeDataAsync Tests

    [Fact]
    public async Task EncryptBadgeDataAsync_ValidData_ReturnsEncryptedString()
    {
        // Arrange
        var data = "Sensitive badge verification data";

        // Act
        var encrypted = await _service.EncryptBadgeDataAsync(data);

        // Assert
        encrypted.Should().NotBeNullOrEmpty();
        encrypted.Should().NotBe(data);
    }

    [Fact]
    public async Task EncryptBadgeDataAsync_EmptyData_ReturnsEmpty()
    {
        // Act
        var encrypted = await _service.EncryptBadgeDataAsync("");

        // Assert
        encrypted.Should().BeEmpty();
    }

    [Fact]
    public async Task EncryptBadgeDataAsync_NullData_ReturnsEmpty()
    {
        // Act
        var encrypted = await _service.EncryptBadgeDataAsync(null!);

        // Assert
        encrypted.Should().BeEmpty();
    }

    [Fact]
    public async Task EncryptBadgeDataAsync_SameDataTwice_ReturnsDifferentCiphertext()
    {
        // Arrange
        var data = "Same data for both encryptions";

        // Act
        var encrypted1 = await _service.EncryptBadgeDataAsync(data);
        var encrypted2 = await _service.EncryptBadgeDataAsync(data);

        // Assert - Should be different due to random IV
        encrypted1.Should().NotBe(encrypted2);
    }

    [Fact]
    public async Task EncryptBadgeDataAsync_LongData_HandlesCorrectly()
    {
        // Arrange
        var longData = new string('x', 10000); // 10KB of data

        // Act
        var encrypted = await _service.EncryptBadgeDataAsync(longData);

        // Assert
        encrypted.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task EncryptBadgeDataAsync_SpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var specialData = "Data with special chars: <>&\"' and unicode: \u00E9\u00F1\u00FC\u4E2D\u6587";

        // Act
        var encrypted = await _service.EncryptBadgeDataAsync(specialData);

        // Assert
        encrypted.Should().NotBeNullOrEmpty();
        encrypted.Should().NotBe(specialData);
    }

    #endregion

    #region DecryptBadgeDataAsync Tests

    [Fact]
    public async Task DecryptBadgeDataAsync_ValidEncryptedData_ReturnsOriginal()
    {
        // Arrange
        var originalData = "This is the original badge data";
        var encrypted = await _service.EncryptBadgeDataAsync(originalData);

        // Act
        var decrypted = await _service.DecryptBadgeDataAsync(encrypted);

        // Assert
        decrypted.Should().Be(originalData);
    }

    [Fact]
    public async Task DecryptBadgeDataAsync_EmptyData_ReturnsEmpty()
    {
        // Act
        var decrypted = await _service.DecryptBadgeDataAsync("");

        // Assert
        decrypted.Should().BeEmpty();
    }

    [Fact]
    public async Task DecryptBadgeDataAsync_NullData_ReturnsEmpty()
    {
        // Act
        var decrypted = await _service.DecryptBadgeDataAsync(null!);

        // Assert
        decrypted.Should().BeEmpty();
    }

    [Fact]
    public async Task DecryptBadgeDataAsync_InvalidBase64_ThrowsException()
    {
        // Act & Assert
        var action = async () => await _service.DecryptBadgeDataAsync("not-valid-base64!!!");
        await action.Should().ThrowAsync<FormatException>();
    }

    [Fact]
    public async Task DecryptBadgeDataAsync_TamperedData_ThrowsException()
    {
        // Arrange
        var originalData = "Original data";
        var encrypted = await _service.EncryptBadgeDataAsync(originalData);

        // Tamper with the encrypted data (change some characters)
        var bytes = Convert.FromBase64String(encrypted);
        bytes[bytes.Length / 2] ^= 0xFF; // Flip bits in the middle
        var tampered = Convert.ToBase64String(bytes);

        // Act & Assert
        var action = async () => await _service.DecryptBadgeDataAsync(tampered);
        await action.Should().ThrowAsync<Exception>(); // CryptographicException
    }

    [Fact]
    public async Task DecryptBadgeDataAsync_SpecialCharacters_RoundTripsCorrectly()
    {
        // Arrange
        var specialData = "Badge data: <test> & \"quotes\" 'apostrophes' \u00E9\u00F1\u00FC";
        var encrypted = await _service.EncryptBadgeDataAsync(specialData);

        // Act
        var decrypted = await _service.DecryptBadgeDataAsync(encrypted);

        // Assert
        decrypted.Should().Be(specialData);
    }

    [Fact]
    public async Task DecryptBadgeDataAsync_LongData_RoundTripsCorrectly()
    {
        // Arrange
        var longData = new string('A', 5000) + "middle" + new string('Z', 5000);
        var encrypted = await _service.EncryptBadgeDataAsync(longData);

        // Act
        var decrypted = await _service.DecryptBadgeDataAsync(encrypted);

        // Assert
        decrypted.Should().Be(longData);
    }

    #endregion

    #region Integration Scenarios

    [Fact]
    public async Task EndToEnd_BadgeCreationAndValidation_WorksCorrectly()
    {
        // Arrange - Create a new badge
        var badge = new UserBadge
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            BadgeType = "VERIFIED_IDENTITY",
            BadgeName = "Verified Identity",
            BadgeDescription = "User has completed identity verification",
            Category = BadgeCategory.Trust,
            EarnedAt = DateTime.UtcNow,
            VerificationLevel = VerificationLevel.External,
            VerificationEvidence = "Document ID: DOC-123456"
        };

        // Act - Generate integrity hash
        badge.IntegrityHash = await _service.GenerateBadgeHashAsync(badge);

        // Assert - Hash was generated
        badge.IntegrityHash.Should().NotBeNullOrEmpty();

        // Act - Validate integrity
        var isValid = await _service.ValidateBadgeIntegrityAsync(badge);

        // Assert - Badge is valid
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task EndToEnd_BadgeVerificationCode_WorksCorrectly()
    {
        // Arrange
        var badge = CreateTestBadge();

        // Act - Generate verification code
        var code = await _service.GenerateVerificationCodeAsync(badge.Id, badge.UserId);

        // Assert
        code.Should().NotBeNullOrEmpty();

        // Act - Verify the code
        var isValid = await _service.VerifyBadgeCodeAsync(badge.Id, code);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task EndToEnd_EncryptDecryptSensitiveEvidence_WorksCorrectly()
    {
        // Arrange - Badge with sensitive evidence
        var sensitiveEvidence = @"{
            ""documentType"": ""passport"",
            ""documentNumber"": ""AB123456"",
            ""issuingCountry"": ""US"",
            ""verifiedAt"": ""2024-01-15T10:30:00Z""
        }";

        // Act - Encrypt the evidence
        var encrypted = await _service.EncryptBadgeDataAsync(sensitiveEvidence);

        // Store encrypted evidence in badge
        var badge = CreateTestBadge();
        badge.VerificationEvidence = encrypted;

        // Act - Decrypt when needed
        var decrypted = await _service.DecryptBadgeDataAsync(badge.VerificationEvidence);

        // Assert
        decrypted.Should().Be(sensitiveEvidence);
    }

    [Fact]
    public async Task SecurityScenario_DifferentSecretKeys_ProduceDifferentHashes()
    {
        // Arrange - Create two services with different keys
        var config1 = new BadgeSecurityConfiguration
        {
            SecretKey = "SecretKeyVersion1_ForTesting",
            VerificationCodeExpiryHours = 24
        };
        var config2 = new BadgeSecurityConfiguration
        {
            SecretKey = "SecretKeyVersion2_ForTesting",
            VerificationCodeExpiryHours = 24
        };

        var logger = new LoggerFactory().CreateLogger<BadgeSecurityService>();
        var service1 = new BadgeSecurityService(logger, Options.Create(config1));
        var service2 = new BadgeSecurityService(logger, Options.Create(config2));

        var badge = CreateTestBadge();

        // Act
        var hash1 = await service1.GenerateBadgeHashAsync(badge);
        var hash2 = await service2.GenerateBadgeHashAsync(badge);

        // Assert - Different keys should produce different hashes
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public async Task SecurityScenario_CannotDecryptWithDifferentKey()
    {
        // Arrange - Create two services with different keys
        var config1 = new BadgeSecurityConfiguration
        {
            SecretKey = "OriginalSecretKeyForEncryption",
            VerificationCodeExpiryHours = 24
        };
        var config2 = new BadgeSecurityConfiguration
        {
            SecretKey = "DifferentSecretKeyForDecryption",
            VerificationCodeExpiryHours = 24
        };

        var logger = new LoggerFactory().CreateLogger<BadgeSecurityService>();
        var service1 = new BadgeSecurityService(logger, Options.Create(config1));
        var service2 = new BadgeSecurityService(logger, Options.Create(config2));

        var data = "Sensitive data encrypted with key 1";

        // Act - Encrypt with service 1
        var encrypted = await service1.EncryptBadgeDataAsync(data);

        // Assert - Cannot decrypt with service 2 (different key)
        var action = async () => await service2.DecryptBadgeDataAsync(encrypted);
        await action.Should().ThrowAsync<Exception>(); // CryptographicException
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task GenerateBadgeHashAsync_EmptyBadgeType_HandlesCorrectly()
    {
        // Arrange
        var badge = CreateTestBadge();
        badge.BadgeType = "";

        // Act
        var hash = await _service.GenerateBadgeHashAsync(badge);

        // Assert
        hash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateBadgeHashAsync_EmptyGuidUserId_HandlesCorrectly()
    {
        // Arrange
        var badge = CreateTestBadge();
        badge.UserId = Guid.Empty;

        // Act
        var hash = await _service.GenerateBadgeHashAsync(badge);

        // Assert
        hash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateBadgeHashAsync_MinDateEarnedAt_HandlesCorrectly()
    {
        // Arrange
        var badge = CreateTestBadge();
        badge.EarnedAt = DateTime.MinValue;

        // Act
        var hash = await _service.GenerateBadgeHashAsync(badge);

        // Assert
        hash.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Helper Methods

    private UserBadge CreateTestBadge()
    {
        return new UserBadge
        {
            Id = _badgeId,
            UserId = _userId,
            BadgeType = "HIGH_PERFORMER",
            BadgeName = "High Performer",
            BadgeDescription = "Awarded for excellent performance",
            Category = BadgeCategory.Achievement,
            EarnedAt = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            VerificationLevel = VerificationLevel.Automatic,
            VerificationEvidence = "Project completions: 10",
            IsActive = true
        };
    }

    #endregion
}
