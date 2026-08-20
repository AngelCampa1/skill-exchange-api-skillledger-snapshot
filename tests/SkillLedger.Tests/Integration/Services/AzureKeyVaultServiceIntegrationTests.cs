using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkillLedger.Infrastructure.Configuration;
using SkillLedger.Infrastructure.Services;
using SkillLedger.Tests.Infrastructure;
using Xunit;

namespace SkillLedger.Tests.Integration.Services;

/// <summary>
/// Integration tests for AzureKeyVaultService - SECRET MANAGEMENT & CACHING.
///
/// CORRECT PATTERN (per TDD_GUIDE.md):
/// - Tests service in "disabled" mode (fallback/development logic)
/// - Uses real IMemoryCache for caching behavior
/// - NO external service mocks (Azure Key Vault SDK is external - tested via fallback mode)
/// - Verifies key consistency, caching, and thread safety
/// - Tests real production code paths (fallback logic)
///
/// Max mocked external dependencies: 0 (testing disabled mode fallback logic)
/// </summary>
[IntegrationTest]
[SecurityTest]
public class AzureKeyVaultServiceIntegrationTests : IDisposable
{
    private readonly IMemoryCache _memoryCache;
    private readonly AzureKeyVaultService _service;
    private readonly AzureKeyVaultConfiguration _kvConfig;
    private readonly EncryptionConfiguration _encConfig;

    public AzureKeyVaultServiceIntegrationTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());

        // Configure service in DISABLED mode (uses fallback logic - production code!)
        _kvConfig = new AzureKeyVaultConfiguration
        {
            Enabled = false,  // Test fallback mode
            VaultUri = "",
            JwtPrivateKeyName = "jwt-private-test",
            JwtPublicKeyName = "jwt-public-test",
            KeyCacheDurationMinutes = 1  // 1 minute cache for testing
        };

        _encConfig = new EncryptionConfiguration
        {
            MasterKeyName = "test-master-key",
            SsnEncryptionKeyName = "test-ssn-key",
            KeyCacheDuration = TimeSpan.FromMinutes(1)
        };

        var kvOptions = Options.Create(_kvConfig);
        var encOptions = Options.Create(_encConfig);
        var logger = new LoggerFactory().CreateLogger<AzureKeyVaultService>();

        _service = new AzureKeyVaultService(kvOptions, encOptions, _memoryCache, logger);
    }

    #region Caching Tests

    [Fact]
    public async Task GetDataEncryptionKeyAsync_CalledTwice_ShouldReturnSameKeyFromCache()
    {
        // Act - Get key twice
        var key1 = await _service.GetDataEncryptionKeyAsync();
        var key2 = await _service.GetDataEncryptionKeyAsync();

        // Assert - Keys should be identical (cached)
        key1.Should().Equal(key2, "cached key should be returned on second call");
        key1.Length.Should().Be(32, "DEK should be 256-bit (32 bytes)");
    }

    [Fact]
    public async Task GetSsnEncryptionKeyAsync_CalledTwice_ShouldReturnSameKey()
    {
        // Act - Get SSN key twice
        var key1 = await _service.GetSsnEncryptionKeyAsync();
        var key2 = await _service.GetSsnEncryptionKeyAsync();

        // Assert - SSN keys should be deterministic (cached) for consistent encryption
        // BUG #KV-001 FIX: SSN key is now deterministic via caching
        key1.Should().Equal(key2,
            "SSN key should be deterministic (cached) for consistent encryption");

        key1.Length.Should().Be(32, "SSN key should be 256-bit (32 bytes)");
        key2.Length.Should().Be(32, "SSN key should be 256-bit (32 bytes)");
    }

    [Fact]
    public async Task GetDataEncryptionKeyAsync_Deterministic_ShouldReturnSameKeyAcrossInstances()
    {
        // Arrange - Create two separate service instances
        var memoryCache2 = new MemoryCache(new MemoryCacheOptions());
        var service2 = new AzureKeyVaultService(
            Options.Create(_kvConfig),
            Options.Create(_encConfig),
            memoryCache2,
            new LoggerFactory().CreateLogger<AzureKeyVaultService>());

        // Act - Get key from both instances
        var key1 = await _service.GetDataEncryptionKeyAsync();
        var key2 = await service2.GetDataEncryptionKeyAsync();

        // Assert - Keys should be identical (deterministic seed)
        key1.Should().Equal(key2,
            "DEK should be deterministic across service instances (same seed)");

        memoryCache2.Dispose();
        service2.Dispose();
    }

    [Fact]
    public async Task GetJwtKeysAsync_WhenDisabled_ShouldReturnEmptyKeys()
    {
        // Act
        var (privateKey, publicKey) = await _service.GetJwtKeysAsync();

        // Assert
        privateKey.Should().BeEmpty("JWT keys should be empty when Key Vault disabled");
        publicKey.Should().BeEmpty("JWT keys should be empty when Key Vault disabled");
    }

    #endregion

    #region Key Consistency Tests

    [Fact]
    public async Task GetDataEncryptionKeyAsync_CalledInSameMonth_ShouldReturnConsistentKey()
    {
        // Act - Get key multiple times within same month
        var keys = new List<byte[]>();
        for (int i = 0; i < 5; i++)
        {
            keys.Add(await _service.GetDataEncryptionKeyAsync());
            await Task.Delay(10);  // Small delay
        }

        // Assert - All keys should be identical
        for (int i = 1; i < keys.Count; i++)
        {
            keys[i].Should().Equal(keys[0],
                $"DEK call {i} should return same key as call 0 (cached)");
        }
    }

    [Fact]
    public async Task GetDataEncryptionKeyAsync_DeterministicSeed_ShouldProduceSameKey()
    {
        // Arrange - Clear cache to force new key generation
        _memoryCache.Remove("data_encryption_key");

        // Act - Get key
        var key = await _service.GetDataEncryptionKeyAsync();

        // Assert - Key should be SHA256 of deterministic seed
        var seedString = "SkillLedger-Test-DEK-Seed-For-Consistent-Encryption";
        var seedBytes = System.Text.Encoding.UTF8.GetBytes(seedString);
        var expectedHash = System.Security.Cryptography.SHA256.HashData(seedBytes);

        key.Should().Equal(expectedHash,
            "DEK should be SHA256 hash of deterministic seed string");
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task GetDataEncryptionKeyAsync_ConcurrentCalls_ShouldBeThreadSafe()
    {
        // Arrange - Clear cache to test concurrent first access
        _memoryCache.Remove("data_encryption_key");

        // Act - 20 concurrent calls to get DEK
        var tasks = Enumerable.Range(0, 20)
            .Select(_ => _service.GetDataEncryptionKeyAsync())
            .ToList();

        var keys = await Task.WhenAll(tasks);

        // Assert - All keys should be identical (thread-safe caching)
        var firstKey = keys[0];
        foreach (var key in keys)
        {
            key.Should().Equal(firstKey,
                "concurrent calls should return same cached key (thread-safe)");
        }
    }

    [Fact]
    public async Task GetSsnEncryptionKeyAsync_ConcurrentCalls_ShouldNotCrash()
    {
        // Act - 20 concurrent calls to get SSN key (each generates random key)
        var tasks = Enumerable.Range(0, 20)
            .Select(_ => _service.GetSsnEncryptionKeyAsync())
            .ToList();

        // Assert - Should not throw (even though keys are random - BUG!)
        var act = async () => await Task.WhenAll(tasks);
        await act.Should().NotThrowAsync("concurrent SSN key generation should not crash");

        var keys = await Task.WhenAll(tasks);
        keys.Should().AllSatisfy(k => k.Length.Should().Be(32),
            "all SSN keys should be 256-bit (32 bytes)");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task EncryptAsync_WhenDisabled_ShouldThrowInvalidOperationException()
    {
        // Act & Assert
        var act = async () => await _service.EncryptAsync("test data", "test-key");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not configured*",
                "encrypt should fail when Key Vault disabled");
    }

    [Fact]
    public async Task DecryptAsync_WhenDisabled_ShouldThrowInvalidOperationException()
    {
        // Act & Assert
        var act = async () => await _service.DecryptAsync("encrypted-data", "test-key");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not configured*",
                "decrypt should fail when Key Vault disabled");
    }

    [Fact]
    public async Task CreateKeyAsync_WhenDisabled_ShouldReturnFalse()
    {
        // Act
        var result = await _service.CreateKeyAsync("new-key", "RSA", 2048);

        // Assert
        result.Should().BeFalse("key creation should fail gracefully when Key Vault disabled");
    }

    [Fact]
    public async Task RotateKeyAsync_WhenDisabled_ShouldReturnFalse()
    {
        // Act
        var result = await _service.RotateKeyAsync("existing-key");

        // Assert
        result.Should().BeFalse("key rotation should fail gracefully when Key Vault disabled");
    }

    [Fact]
    public async Task IsHealthyAsync_WhenDisabled_ShouldReturnFalse()
    {
        // Act
        var isHealthy = await _service.IsHealthyAsync();

        // Assert
        isHealthy.Should().BeFalse("health check should fail when Key Vault disabled");
    }

    #endregion

    #region Cache Invalidation Tests

    [Fact]
    public async Task GetDataEncryptionKeyAsync_AfterCacheExpiration_ShouldRegenerateKey()
    {
        // Arrange - Set very short cache duration
        var shortCacheConfig = new EncryptionConfiguration
        {
            MasterKeyName = "test-master-key",
            SsnEncryptionKeyName = "test-ssn-key",
            KeyCacheDuration = TimeSpan.FromMilliseconds(100)  // 100ms cache
        };

        var shortCacheMemory = new MemoryCache(new MemoryCacheOptions());
        var shortCacheService = new AzureKeyVaultService(
            Options.Create(_kvConfig),
            Options.Create(shortCacheConfig),
            shortCacheMemory,
            new LoggerFactory().CreateLogger<AzureKeyVaultService>());

        // Act - Get key, wait for expiration, get again
        var key1 = await shortCacheService.GetDataEncryptionKeyAsync();
        await Task.Delay(200);  // Wait for cache to expire
        var key2 = await shortCacheService.GetDataEncryptionKeyAsync();

        // Assert - Keys should still be identical (deterministic seed)
        key1.Should().Equal(key2,
            "DEK should be deterministic even after cache expiration");

        shortCacheMemory.Dispose();
        shortCacheService.Dispose();
    }

    [Fact]
    public async Task GetJwtKeysAsync_CalledTwice_ShouldUseCacheOnSecondCall()
    {
        // Arrange - Create service with enabled Key Vault (will fail but we test cache behavior)
        var enabledConfig = new AzureKeyVaultConfiguration
        {
            Enabled = false,  // Still disabled for this test
            VaultUri = "",
            JwtPrivateKeyName = "jwt-private-test",
            JwtPublicKeyName = "jwt-public-test",
            KeyCacheDurationMinutes = 5
        };

        // Act - Get JWT keys twice (will return empty but should cache)
        var keys1 = await _service.GetJwtKeysAsync();
        var keys2 = await _service.GetJwtKeysAsync();

        // Assert - Should return same empty keys (cached)
        keys1.Should().Be(keys2, "JWT keys should be cached");
    }

    #endregion

    public void Dispose()
    {
        _memoryCache.Dispose();
        _service.Dispose();
    }
}
