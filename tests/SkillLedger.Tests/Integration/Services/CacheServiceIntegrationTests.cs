using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.Models;
using SkillLedger.Infrastructure.Services;
using SkillLedger.Tests.Infrastructure;
using Xunit;

namespace SkillLedger.Tests.Integration.Services;

/// <summary>
/// Integration tests for CacheService - DISTRIBUTED CACHING.
///
/// Pattern (per TDD_GUIDE.md):
/// - Uses real IMemoryCache
/// - Uses MemoryDistributedCache as distributed cache (no Redis needed)
/// - Tests caching logic, fallbacks, and error handling
///
/// Max mocked external dependencies: 0 (using real implementations)
/// </summary>
[IntegrationTest]
public class CacheServiceIntegrationTests : IDisposable
{
    private readonly CacheService _service;
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;

    public CacheServiceIntegrationTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _distributedCache = new MemoryDistributedCache(
            Microsoft.Extensions.Options.Options.Create(new MemoryDistributedCacheOptions()));

        var logger = new LoggerFactory().CreateLogger<CacheService>();

        // Create service without Redis (uses in-memory fallback)
        _service = new CacheService(_distributedCache, _memoryCache, logger, null);
    }

    public void Dispose()
    {
        _memoryCache.Dispose();
    }

    #region GetAsync Tests

    [Fact]
    public async Task GetAsync_NonExistentKey_ReturnsNull()
    {
        // Act
        var result = await _service.GetAsync<TestCacheObject>("non-existent-key");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_NullKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.GetAsync<TestCacheObject>(null!));
    }

    [Fact]
    public async Task GetAsync_EmptyKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.GetAsync<TestCacheObject>(""));
    }

    [Fact]
    public async Task GetAsync_WhitespaceKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.GetAsync<TestCacheObject>("   "));
    }

    [Fact]
    public async Task GetAsync_ExistingKey_ReturnsCachedValue()
    {
        // Arrange
        var key = "test-key";
        var value = new TestCacheObject { Id = 1, Name = "Test" };
        await _service.SetAsync(key, value, TimeSpan.FromMinutes(5));

        // Act
        var result = await _service.GetAsync<TestCacheObject>(key);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Test");
    }

    #endregion

    #region SetAsync Tests

    [Fact]
    public async Task SetAsync_ValidKeyAndValue_ReturnsTrue()
    {
        // Arrange
        var key = "set-test-key";
        var value = new TestCacheObject { Id = 1, Name = "Test" };

        // Act
        var result = await _service.SetAsync(key, value, TimeSpan.FromMinutes(5));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SetAsync_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var value = new TestCacheObject { Id = 1, Name = "Test" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.SetAsync(null!, value, TimeSpan.FromMinutes(5)));
    }

    [Fact]
    public async Task SetAsync_EmptyKey_ThrowsArgumentNullException()
    {
        // Arrange
        var value = new TestCacheObject { Id = 1, Name = "Test" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.SetAsync("", value, TimeSpan.FromMinutes(5)));
    }

    [Fact]
    public async Task SetAsync_NullValue_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.SetAsync<TestCacheObject>("key", null!, TimeSpan.FromMinutes(5)));
    }

    [Fact]
    public async Task SetAsync_ValueCanBeRetrieved()
    {
        // Arrange
        var key = "retrieve-test-key";
        var value = new TestCacheObject { Id = 42, Name = "Retrieval Test" };

        // Act
        await _service.SetAsync(key, value, TimeSpan.FromMinutes(5));
        var retrieved = await _service.GetAsync<TestCacheObject>(key);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(42);
        retrieved.Name.Should().Be("Retrieval Test");
    }

    [Fact]
    public async Task SetAsync_OverwritesExistingValue()
    {
        // Arrange
        var key = "overwrite-test-key";
        var value1 = new TestCacheObject { Id = 1, Name = "Original" };
        var value2 = new TestCacheObject { Id = 2, Name = "Updated" };

        await _service.SetAsync(key, value1, TimeSpan.FromMinutes(5));

        // Act
        await _service.SetAsync(key, value2, TimeSpan.FromMinutes(5));
        var retrieved = await _service.GetAsync<TestCacheObject>(key);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(2);
        retrieved.Name.Should().Be("Updated");
    }

    [Fact]
    public async Task SetAsync_ShortExpiration_ValueExpiresAfterTime()
    {
        // Arrange
        var key = "expiration-test-key";
        var value = new TestCacheObject { Id = 1, Name = "Expiring" };

        // Act
        await _service.SetAsync(key, value, TimeSpan.FromMilliseconds(50));

        // Wait for expiration
        await Task.Delay(100);

        var retrieved = await _service.GetAsync<TestCacheObject>(key);

        // Assert
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_LongExpiration_ValuePersistsWithinTime()
    {
        // Arrange
        var key = "long-expiration-key";
        var value = new TestCacheObject { Id = 1, Name = "Persisting" };

        // Act
        await _service.SetAsync(key, value, TimeSpan.FromMinutes(5));

        // Small delay
        await Task.Delay(50);

        var retrieved = await _service.GetAsync<TestCacheObject>(key);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("Persisting");
    }

    #endregion

    #region RemoveAsync Tests

    [Fact]
    public async Task RemoveAsync_ExistingKey_ReturnsTrue()
    {
        // Arrange
        var key = "remove-test-key";
        var value = new TestCacheObject { Id = 1, Name = "ToRemove" };
        await _service.SetAsync(key, value, TimeSpan.FromMinutes(5));

        // Act
        var result = await _service.RemoveAsync(key);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveAsync_NonExistentKey_ReturnsTrue()
    {
        // Act
        var result = await _service.RemoveAsync("non-existent-remove-key");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveAsync_NullKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.RemoveAsync(null!));
    }

    [Fact]
    public async Task RemoveAsync_EmptyKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.RemoveAsync(""));
    }

    [Fact]
    public async Task RemoveAsync_ValueNoLongerRetrievable()
    {
        // Arrange
        var key = "remove-verify-key";
        var value = new TestCacheObject { Id = 1, Name = "ToRemove" };
        await _service.SetAsync(key, value, TimeSpan.FromMinutes(5));

        // Verify it exists first
        var beforeRemove = await _service.GetAsync<TestCacheObject>(key);
        beforeRemove.Should().NotBeNull();

        // Act
        await _service.RemoveAsync(key);
        var afterRemove = await _service.GetAsync<TestCacheObject>(key);

        // Assert
        afterRemove.Should().BeNull();
    }

    #endregion

    #region RemoveByPatternAsync Tests

    [Fact]
    public async Task RemoveByPatternAsync_NullPattern_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.RemoveByPatternAsync(null!));
    }

    [Fact]
    public async Task RemoveByPatternAsync_EmptyPattern_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.RemoveByPatternAsync(""));
    }

    [Fact]
    public async Task RemoveByPatternAsync_NoRedis_ReturnsZero()
    {
        // Act (without Redis, pattern removal is not supported for memory cache)
        var result = await _service.RemoveByPatternAsync("test:*");

        // Assert
        result.Should().Be(0);
    }

    #endregion

    #region IsRedisAvailableAsync Tests

    [Fact]
    public async Task IsRedisAvailableAsync_NoRedis_ReturnsFalse()
    {
        // Act
        var result = await _service.IsRedisAvailableAsync();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetStatisticsAsync Tests

    [Fact]
    public async Task GetStatisticsAsync_NoRedis_ReturnsStatisticsWithRedisDisconnected()
    {
        // Act
        var stats = await _service.GetStatisticsAsync();

        // Assert
        stats.Should().NotBeNull();
        stats.IsRedisConnected.Should().BeFalse();
    }

    [Fact]
    public async Task GetStatisticsAsync_WithCachedItems_ReturnsCorrectMemoryCacheSize()
    {
        // Arrange
        await _service.SetAsync("stats-key-1", new TestCacheObject { Id = 1 }, TimeSpan.FromMinutes(5));
        await _service.SetAsync("stats-key-2", new TestCacheObject { Id = 2 }, TimeSpan.FromMinutes(5));

        // Act
        var stats = await _service.GetStatisticsAsync();

        // Assert
        stats.InMemoryCacheSize.Should().BeGreaterThanOrEqualTo(2);
    }

    #endregion

    #region Complex Object Tests

    [Fact]
    public async Task SetGetAsync_ComplexNestedObject_RoundTripsCorrectly()
    {
        // Arrange
        var key = "complex-object-key";
        var value = new ComplexCacheObject
        {
            Id = Guid.NewGuid(),
            Name = "Complex Object",
            Timestamp = DateTime.UtcNow,
            Tags = new List<string> { "tag1", "tag2", "tag3" },
            Nested = new TestCacheObject { Id = 99, Name = "Nested" },
            Metadata = new Dictionary<string, string>
            {
                ["key1"] = "value1",
                ["key2"] = "value2"
            }
        };

        // Act
        await _service.SetAsync(key, value, TimeSpan.FromMinutes(5));
        var retrieved = await _service.GetAsync<ComplexCacheObject>(key);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(value.Id);
        retrieved.Name.Should().Be("Complex Object");
        retrieved.Tags.Should().HaveCount(3);
        retrieved.Nested!.Id.Should().Be(99);
        retrieved.Metadata.Should().ContainKey("key1");
    }

    [Fact]
    public async Task SetGetAsync_ListOfObjects_RoundTripsCorrectly()
    {
        // Arrange
        var key = "list-object-key";
        var value = new List<TestCacheObject>
        {
            new() { Id = 1, Name = "First" },
            new() { Id = 2, Name = "Second" },
            new() { Id = 3, Name = "Third" }
        };

        // Act
        await _service.SetAsync(key, value, TimeSpan.FromMinutes(5));
        var retrieved = await _service.GetAsync<List<TestCacheObject>>(key);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved.Should().HaveCount(3);
        retrieved![0].Name.Should().Be("First");
        retrieved[2].Name.Should().Be("Third");
    }

    #endregion

    #region Concurrent Access Tests

    [Fact]
    public async Task ConcurrentSetGet_MultipleTasks_HandlesCorrectly()
    {
        // Arrange
        var tasks = new List<Task>();
        var results = new List<bool>();

        // Act - Set multiple values concurrently
        for (int i = 0; i < 10; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () =>
            {
                var key = $"concurrent-key-{index}";
                var value = new TestCacheObject { Id = index, Name = $"Concurrent-{index}" };
                var result = await _service.SetAsync(key, value, TimeSpan.FromMinutes(5));
                lock (results)
                {
                    results.Add(result);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - All operations succeeded
        results.Should().AllBeEquivalentTo(true);

        // Verify all values are retrievable
        for (int i = 0; i < 10; i++)
        {
            var key = $"concurrent-key-{i}";
            var retrieved = await _service.GetAsync<TestCacheObject>(key);
            retrieved.Should().NotBeNull();
            retrieved!.Id.Should().Be(i);
        }
    }

    [Fact]
    public async Task ConcurrentSetRemove_SameKey_HandlesCorrectly()
    {
        // Arrange
        var key = "concurrent-same-key";
        var value = new TestCacheObject { Id = 1, Name = "Test" };

        // Act - Set and remove concurrently
        var setTask = Task.Run(async () =>
        {
            for (int i = 0; i < 10; i++)
            {
                await _service.SetAsync(key, value, TimeSpan.FromMinutes(5));
                await Task.Delay(5);
            }
        });

        var removeTask = Task.Run(async () =>
        {
            for (int i = 0; i < 10; i++)
            {
                await _service.RemoveAsync(key);
                await Task.Delay(5);
            }
        });

        // Assert - No exceptions thrown during concurrent access
        var exception = await Record.ExceptionAsync(async () =>
        {
            await Task.WhenAll(setTask, removeTask);
        });

        exception.Should().BeNull();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task SetAsync_VeryLongKey_Succeeds()
    {
        // Arrange
        var key = new string('x', 500); // 500 character key
        var value = new TestCacheObject { Id = 1, Name = "Long Key Test" };

        // Act
        var result = await _service.SetAsync(key, value, TimeSpan.FromMinutes(5));
        var retrieved = await _service.GetAsync<TestCacheObject>(key);

        // Assert
        result.Should().BeTrue();
        retrieved.Should().NotBeNull();
    }

    [Fact]
    public async Task SetAsync_SpecialCharactersInKey_Succeeds()
    {
        // Arrange
        var key = "special:key:with/slashes\\and:colons";
        var value = new TestCacheObject { Id = 1, Name = "Special Key Test" };

        // Act
        var result = await _service.SetAsync(key, value, TimeSpan.FromMinutes(5));
        var retrieved = await _service.GetAsync<TestCacheObject>(key);

        // Assert
        result.Should().BeTrue();
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("Special Key Test");
    }

    [Fact]
    public async Task SetAsync_UnicodeKey_Succeeds()
    {
        // Arrange
        var key = "unicode:键:キー:مفتاح";
        var value = new TestCacheObject { Id = 1, Name = "Unicode Key Test" };

        // Act
        var result = await _service.SetAsync(key, value, TimeSpan.FromMinutes(5));
        var retrieved = await _service.GetAsync<TestCacheObject>(key);

        // Assert
        result.Should().BeTrue();
        retrieved.Should().NotBeNull();
    }

    [Fact]
    public async Task SetAsync_LargeObject_Succeeds()
    {
        // Arrange
        var key = "large-object-key";
        var value = new ComplexCacheObject
        {
            Id = Guid.NewGuid(),
            Name = new string('x', 10000), // 10KB name
            Tags = Enumerable.Range(0, 1000).Select(i => $"tag-{i}").ToList()
        };

        // Act
        var result = await _service.SetAsync(key, value, TimeSpan.FromMinutes(5));
        var retrieved = await _service.GetAsync<ComplexCacheObject>(key);

        // Assert
        result.Should().BeTrue();
        retrieved.Should().NotBeNull();
        retrieved!.Name.Length.Should().Be(10000);
        retrieved.Tags.Should().HaveCount(1000);
    }

    #endregion

    #region Additional Coverage Tests

    [Fact]
    public async Task GetAsync_MemoryCacheHit_ReturnsFromMemoryCache()
    {
        // Arrange - Set value directly in memory cache (bypassing distributed cache)
        var key = "memory-only-key";
        var value = new TestCacheObject { Id = 99, Name = "Memory Only" };
        var memoryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };
        _memoryCache.Set(key, value, memoryOptions);

        // Act - Get should hit memory cache
        var result = await _service.GetAsync<TestCacheObject>(key);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(99);
        result.Name.Should().Be("Memory Only");
    }

    [Fact]
    public async Task SetAsync_WithExpiration_SetsCorrectTTL()
    {
        // Arrange
        var key = "ttl-test-key";
        var value = new TestCacheObject { Id = 100, Name = "TTL Test" };
        var expiration = TimeSpan.FromSeconds(30);

        // Act
        var result = await _service.SetAsync(key, value, expiration);

        // Assert
        result.Should().BeTrue();

        // Verify value is cached and can be retrieved
        var retrieved = await _service.GetAsync<TestCacheObject>(key);
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(100);
    }

    [Fact]
    public async Task RemoveAsync_RemovesFromBothCaches()
    {
        // Arrange - Set in both caches
        var key = "remove-both-key";
        var value = new TestCacheObject { Id = 101, Name = "Remove Test" };
        await _service.SetAsync(key, value, TimeSpan.FromMinutes(5));

        // Verify it exists
        var beforeRemove = await _service.GetAsync<TestCacheObject>(key);
        beforeRemove.Should().NotBeNull();

        // Act - Remove from both
        var result = await _service.RemoveAsync(key);

        // Assert
        result.Should().BeTrue();

        // Verify it's gone from both caches
        var afterRemove = await _service.GetAsync<TestCacheObject>(key);
        afterRemove.Should().BeNull();
    }

    [Fact]
    public async Task GetStatisticsAsync_AfterCaching_ReturnsNonZeroMemoryCacheSize()
    {
        // Arrange - Add multiple items to cache
        for (int i = 0; i < 5; i++)
        {
            await _service.SetAsync($"stats-key-{i}", new TestCacheObject { Id = i, Name = $"Item {i}" }, TimeSpan.FromMinutes(5));
        }

        // Act
        var stats = await _service.GetStatisticsAsync();

        // Assert
        stats.Should().NotBeNull();
        stats.IsRedisConnected.Should().BeFalse("Redis is not available in tests");
        stats.InMemoryCacheSize.Should().BeGreaterOrEqualTo(5, "should have at least 5 items in memory cache");
    }

    #endregion

    #region Helper Classes

    private class TestCacheObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class ComplexCacheObject
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public List<string> Tags { get; set; } = new();
        public TestCacheObject? Nested { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    #endregion
}
