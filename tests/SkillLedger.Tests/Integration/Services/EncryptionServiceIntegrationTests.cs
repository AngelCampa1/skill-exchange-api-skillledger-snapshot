using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkillLedger.Infrastructure.Configuration;
using SkillLedger.Infrastructure.Services;
using SkillLedger.Tests.Infrastructure;
using SkillLedger.Tests.Mocks;
using Xunit;

namespace SkillLedger.Tests.Integration.Services;

/// <summary>
/// Integration tests for EncryptionService using real AES-256 encryption
/// Following anti-mocking pattern: Only mock external Azure Key Vault service
/// </summary>
[IntegrationTest]
public class EncryptionServiceIntegrationTests : IDisposable
{
    private readonly EncryptionService _encryptionService;
    private readonly MockAzureKeyVaultService _keyVaultService;
    private readonly ILogger<EncryptionService> _logger;
    private readonly EncryptionConfiguration _config;

    public EncryptionServiceIntegrationTests()
    {
        // Mock only external Azure Key Vault service
        _keyVaultService = new MockAzureKeyVaultService();

        // Real configuration
        _config = new EncryptionConfiguration
        {
            KeyVaultEndpoint = new Uri("https://test-keyvault.vault.azure.net/"),
            MasterKeyName = "skill-ledger-master-key",
            SsnEncryptionKeyName = "skill-ledger-ssn-key",
            KeySizeInBits = 256,
            UseHsm = true,
            KeyCacheDuration = TimeSpan.FromHours(1),
            MaxOperationsBeforeRotation = 1_000_000,
            EnableKeyRotation = true
        };

        _logger = LoggerFactory
            .Create(builder => builder.AddConsole())
            .CreateLogger<EncryptionService>();

        // Create REAL EncryptionService with real AES-256 encryption
        _encryptionService = new EncryptionService(
            _logger,
            _keyVaultService,
            Options.Create(_config));
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    [Fact]
    public async Task EncryptAsync_DecryptAsync_RoundTrip_ShouldReturnOriginalText()
    {
        // Arrange
        const string plainText = "Sensitive PII Data: John Doe, SSN 123-45-6789";

        // Act
        var encrypted = await _encryptionService.EncryptAsync(plainText);
        var decrypted = await _encryptionService.DecryptAsync(encrypted);

        // Assert
        decrypted.Should().Be(plainText);
        encrypted.Should().NotBe(plainText);
        encrypted.Should().NotBeNullOrEmpty();

        // Verify Key Vault was called
        _keyVaultService.OperationLog.Should().Contain("GetDataEncryptionKey");
    }

    [Fact]
    public async Task EncryptAsync_SamePlainText_ShouldProduceDifferentCiphertext()
    {
        // Arrange
        const string plainText = "Secret Information";

        // Act - Encrypt same text twice
        var encrypted1 = await _encryptionService.EncryptAsync(plainText);
        var encrypted2 = await _encryptionService.EncryptAsync(plainText);

        // Assert - Should produce different ciphertext due to random IV
        encrypted1.Should().NotBe(encrypted2);

        // But both should decrypt to original
        var decrypted1 = await _encryptionService.DecryptAsync(encrypted1);
        var decrypted2 = await _encryptionService.DecryptAsync(encrypted2);

        decrypted1.Should().Be(plainText);
        decrypted2.Should().Be(plainText);
    }

    [Fact]
    public async Task EncryptAsync_EmptyString_ShouldReturnEmptyString()
    {
        // Arrange
        var plainText = string.Empty;

        // Act
        var encrypted = await _encryptionService.EncryptAsync(plainText);

        // Assert
        encrypted.Should().BeEmpty();
    }

    [Fact]
    public async Task EncryptAsync_LongText_ShouldEncryptAndDecryptCorrectly()
    {
        // Arrange
        var plainText = new string('A', 10000); // 10KB of text

        // Act
        var encrypted = await _encryptionService.EncryptAsync(plainText);
        var decrypted = await _encryptionService.DecryptAsync(encrypted);

        // Assert
        decrypted.Should().Be(plainText);
        encrypted.Length.Should().BeGreaterThan(plainText.Length); // Overhead from IV and encoding
    }

    [Fact]
    public async Task EncryptAsync_UnicodeCharacters_ShouldPreserveContent()
    {
        // Arrange
        const string plainText = "Unicode: 你好世界 🌍 Émojis: 😀🎉 Math: ∑∫∂";

        // Act
        var encrypted = await _encryptionService.EncryptAsync(plainText);
        var decrypted = await _encryptionService.DecryptAsync(encrypted);

        // Assert
        decrypted.Should().Be(plainText);
    }

    [Fact]
    public async Task EncryptSsnAsync_SameInput_ShouldProduceSameOutput()
    {
        // Arrange - Deterministic encryption for database queries
        const string ssn = "123-45-6789";

        // Act - Encrypt same SSN twice
        var encrypted1 = await _encryptionService.EncryptSsnAsync(ssn);
        var encrypted2 = await _encryptionService.EncryptSsnAsync(ssn);

        // Assert - Should produce identical ciphertext for database queries
        encrypted1.Should().Be(encrypted2);
        encrypted1.Should().NotBe(ssn);

        // Verify Key Vault was called
        _keyVaultService.OperationLog.Should().Contain("GetSsnEncryptionKey");
    }

    [Fact]
    public async Task EncryptSsnAsync_DifferentInputs_ShouldProduceDifferentOutputs()
    {
        // Arrange
        const string ssn1 = "123-45-6789";
        const string ssn2 = "987-65-4321";

        // Act
        var encrypted1 = await _encryptionService.EncryptSsnAsync(ssn1);
        var encrypted2 = await _encryptionService.EncryptSsnAsync(ssn2);

        // Assert
        encrypted1.Should().NotBe(encrypted2);
    }

    [Fact]
    public async Task EncryptSsnAsync_EmptyString_ShouldReturnEmptyString()
    {
        // Arrange
        var ssn = string.Empty;

        // Act
        var encrypted = await _encryptionService.EncryptSsnAsync(ssn);

        // Assert
        encrypted.Should().BeEmpty();
    }

    [Fact]
    public void HashPii_SameInput_ShouldProduceDifferentHashes()
    {
        // Arrange - PBKDF2 with random salt
        const string piiData = "john.doe@example.com";

        // Act
        var hash1 = _encryptionService.HashPii(piiData);
        var hash2 = _encryptionService.HashPii(piiData);

        // Assert - Different hashes due to random salt
        hash1.Should().NotBe(hash2);
        hash1.Should().NotBeNullOrEmpty();
        hash2.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void HashPii_EmptyString_ShouldReturnEmptyString()
    {
        // Arrange
        var data = string.Empty;

        // Act
        var hash = _encryptionService.HashPii(data);

        // Assert
        hash.Should().BeEmpty();
    }

    [Fact]
    public void HashPii_DifferentInputs_ShouldProduceDifferentHashes()
    {
        // Arrange
        const string data1 = "user1@example.com";
        const string data2 = "user2@example.com";

        // Act
        var hash1 = _encryptionService.HashPii(data1);
        var hash2 = _encryptionService.HashPii(data2);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void GenerateSecureToken_DefaultLength_ShouldGenerateToken()
    {
        // Act
        var token = _encryptionService.GenerateSecureToken();

        // Assert
        token.Should().NotBeNullOrEmpty();
        token.Should().MatchRegex("^[A-Za-z0-9_-]+$"); // URL-safe base64
        token.Should().NotContain("+");
        token.Should().NotContain("/");
        token.Should().NotContain("=");
    }

    [Fact]
    public void GenerateSecureToken_CustomLength_ShouldGenerateCorrectLength()
    {
        // Arrange
        const int length = 64;

        // Act
        var token = _encryptionService.GenerateSecureToken(length);

        // Assert
        token.Should().NotBeNullOrEmpty();
        // Base64 encoding increases length, but token should be substantial
        token.Length.Should().BeGreaterThan(length);
    }

    [Fact]
    public void GenerateSecureToken_MultipleCalls_ShouldGenerateUniqueTokens()
    {
        // Act
        var token1 = _encryptionService.GenerateSecureToken();
        var token2 = _encryptionService.GenerateSecureToken();
        var token3 = _encryptionService.GenerateSecureToken();

        // Assert - All tokens should be unique
        token1.Should().NotBe(token2);
        token1.Should().NotBe(token3);
        token2.Should().NotBe(token3);
    }

    [Fact]
    public async Task DecryptAsync_InvalidBase64_ShouldThrowException()
    {
        // Arrange
        const string invalidBase64 = "not-valid-base64!!!";

        // Act & Assert
        await _encryptionService
            .Invoking(s => s.DecryptAsync(invalidBase64))
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Decryption failed");
    }

    [Fact]
    public async Task DecryptAsync_TamperedCiphertext_ShouldThrowException()
    {
        // Arrange
        const string plainText = "Secret Data";
        var encrypted = await _encryptionService.EncryptAsync(plainText);

        // Tamper with ciphertext by modifying a character
        var tamperedEncrypted = encrypted[..^1] + (encrypted[^1] == 'A' ? 'B' : 'A');

        // Act & Assert
        await _encryptionService
            .Invoking(s => s.DecryptAsync(tamperedEncrypted))
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Decryption failed");
    }

    [Fact]
    public async Task EncryptionService_MultipleOperations_ShouldUseKeyVaultCorrectly()
    {
        // Arrange
        _keyVaultService.ClearOperationLog();

        // Act - Multiple different operations
        await _encryptionService.EncryptAsync("Test 1");
        await _encryptionService.EncryptAsync("Test 2");
        await _encryptionService.EncryptSsnAsync("123-45-6789");
        var token = _encryptionService.GenerateSecureToken();
        var hash = _encryptionService.HashPii("test@example.com");

        // Assert - Verify Key Vault interactions
        _keyVaultService.OperationLog.Should().Contain("GetDataEncryptionKey");
        _keyVaultService.OperationLog.Should().Contain("GetSsnEncryptionKey");

        // Token and hash don't use Key Vault
        token.Should().NotBeNullOrEmpty();
        hash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task EncryptAsync_SpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange - Test special characters that might cause encoding issues
        const string plainText = @"Special: !@#$%^&*()_+{}|:<>?`~-=[]\;',./";

        // Act
        var encrypted = await _encryptionService.EncryptAsync(plainText);
        var decrypted = await _encryptionService.DecryptAsync(encrypted);

        // Assert
        decrypted.Should().Be(plainText);
    }

    [Fact]
    public async Task EncryptAsync_WhitespaceOnly_ShouldEncryptCorrectly()
    {
        // Arrange
        const string plainText = "   \t\n\r  ";

        // Act
        var encrypted = await _encryptionService.EncryptAsync(plainText);
        var decrypted = await _encryptionService.DecryptAsync(encrypted);

        // Assert
        decrypted.Should().Be(plainText);
    }

    [Fact]
    public async Task EncryptAsync_NullInput_ShouldReturnEmptyString()
    {
        // Arrange
        string? plainText = null;

        // Act
        var encrypted = await _encryptionService.EncryptAsync(plainText!);

        // Assert
        encrypted.Should().BeEmpty("null input should return empty string");
    }

    [Fact]
    public async Task DecryptAsync_NullInput_ShouldReturnEmptyString()
    {
        // Arrange
        string? encryptedText = null;

        // Act
        var decrypted = await _encryptionService.DecryptAsync(encryptedText!);

        // Assert
        decrypted.Should().BeEmpty("null input should return empty string");
    }

    [Fact]
    public async Task DecryptAsync_EmptyString_ShouldReturnEmptyString()
    {
        // Arrange
        var encryptedText = string.Empty;

        // Act
        var decrypted = await _encryptionService.DecryptAsync(encryptedText);

        // Assert
        decrypted.Should().BeEmpty();
    }

    [Fact]
    public async Task EncryptAsync_MultilineText_ShouldPreserveFormatting()
    {
        // Arrange - Test PII data with multiple lines
        var plainText = @"Name: John Doe
Address: 123 Main St
City: Springfield
SSN: 123-45-6789
Phone: (555) 123-4567";

        // Act
        var encrypted = await _encryptionService.EncryptAsync(plainText);
        var decrypted = await _encryptionService.DecryptAsync(encrypted);

        // Assert
        decrypted.Should().Be(plainText, "multiline formatting should be preserved");
    }

    [Fact]
    public async Task EncryptSsnAsync_NullInput_ShouldReturnEmptyString()
    {
        // Arrange
        string? ssn = null;

        // Act
        var encrypted = await _encryptionService.EncryptSsnAsync(ssn!);

        // Assert
        encrypted.Should().BeEmpty("null SSN should return empty string");
    }

    [Fact]
    public void HashPii_NullInput_ShouldReturnEmptyString()
    {
        // Arrange
        string? data = null;

        // Act
        var hash = _encryptionService.HashPii(data!);

        // Assert
        hash.Should().BeEmpty("null input should return empty string");
    }

    [Fact]
    public void HashPii_VeryLongInput_ShouldHashSuccessfully()
    {
        // Arrange - Test PBKDF2 with large PII data (50KB)
        var longData = new string('X', 50000);

        // Act
        var hash = _encryptionService.HashPii(longData);

        // Assert
        hash.Should().NotBeNullOrEmpty("should hash large data successfully");
        hash.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GenerateSecureToken_VeryShortLength_ShouldGenerateToken()
    {
        // Arrange
        const int length = 8;

        // Act
        var token = _encryptionService.GenerateSecureToken(length);

        // Assert
        token.Should().NotBeNullOrEmpty();
        token.Should().MatchRegex("^[A-Za-z0-9_-]+$", "should be URL-safe base64");
    }

    [Fact]
    public async Task EncryptionService_ConcurrentOperations_ShouldHandleCorrectly()
    {
        // Arrange
        var tasks = new List<Task<string>>();
        const int concurrentOps = 10;

        // Act - Run multiple encryption operations concurrently
        for (int i = 0; i < concurrentOps; i++)
        {
            var plainText = $"Concurrent Test {i}";
            tasks.Add(_encryptionService.EncryptAsync(plainText));
        }

        var results = await Task.WhenAll(tasks);

        // Assert - All operations should succeed
        results.Should().HaveCount(concurrentOps);
        results.Should().OnlyContain(r => !string.IsNullOrEmpty(r));
        results.Should().OnlyHaveUniqueItems("each encryption should produce unique ciphertext");
    }

    [Fact]
    public async Task EncryptAsync_VeryShortText_ShouldEncryptCorrectly()
    {
        // Arrange
        const string plainText = "X";

        // Act
        var encrypted = await _encryptionService.EncryptAsync(plainText);
        var decrypted = await _encryptionService.DecryptAsync(encrypted);

        // Assert
        decrypted.Should().Be(plainText);
        encrypted.Should().NotBe(plainText);
    }
}
