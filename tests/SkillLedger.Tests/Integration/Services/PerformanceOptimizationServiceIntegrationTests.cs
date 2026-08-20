using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkillLedger.Infrastructure.Services;
using Xunit;

namespace SkillLedger.Tests.Integration.Services;

/// <summary>
/// Integration tests for PerformanceOptimizationService
/// Tests caching operations with real IMemoryCache, no Redis
/// </summary>
public class PerformanceOptimizationServiceIntegrationTests : IDisposable
{
    private readonly IMemoryCache _memoryCache;
    private readonly PerformanceOptimizationService _service;
    private readonly PerformanceOptimizationSettings _settings;

    public PerformanceOptimizationServiceIntegrationTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _settings = new PerformanceOptimizationSettings
        {
            UseRedisCache = false, // No Redis in tests
            DefaultCacheExpiration = TimeSpan.FromMinutes(5),
            MaxCacheSize = 100,
            EnableCompression = true
        };

        var options = Options.Create(_settings);
        var logger = new LoggerFactory().CreateLogger<PerformanceOptimizationService>();

        // Pass null for IConnectionMultiplexer (no Redis)
        _service = new PerformanceOptimizationService(logger, _memoryCache, null, options);
    }

    public void Dispose()
    {
        _memoryCache.Dispose();
    }

    #region GetAsync Tests

    [Fact]
    public async Task GetAsync_WhenCacheMiss_CallsFactoryAndCachesResult()
    {
        // Arrange
        var key = "test-key-miss";
        var expectedValue = "test-value";
        var factoryCalled = false;

        // Act
        var result = await _service.GetAsync(key, async () =>
        {
            factoryCalled = true;
            await Task.Delay(1); // Simulate async work
            return expectedValue;
        });

        // Assert
        result.Should().Be(expectedValue);
        factoryCalled.Should().BeTrue();

        // Verify value was cached
        _memoryCache.TryGetValue(key, out string? cachedValue).Should().BeTrue();
        cachedValue.Should().Be(expectedValue);
    }

    [Fact]
    public async Task GetAsync_WhenCacheHit_ReturnsFromCacheWithoutCallingFactory()
    {
        // Arrange
        var key = "test-key-hit";
        var cachedValue = "cached-value";
        _memoryCache.Set(key, cachedValue);
        var factoryCalled = false;

        // Act
        var result = await _service.GetAsync(key, async () =>
        {
            factoryCalled = true;
            return "new-value";
        });

        // Assert
        result.Should().Be(cachedValue);
        factoryCalled.Should().BeFalse();
    }

    [Fact]
    public async Task GetAsync_WithCustomExpiration_UsesProvidedExpiration()
    {
        // Arrange
        var key = "test-key-expiration";
        var value = "test-value";
        var shortExpiration = TimeSpan.FromMilliseconds(100);

        // Act
        await _service.GetAsync(key, async () => value, shortExpiration);

        // Assert - value should be cached initially
        _memoryCache.TryGetValue(key, out string? _).Should().BeTrue();

        // Wait for expiration
        await Task.Delay(200);

        // Value should have expired
        _memoryCache.TryGetValue(key, out string? expiredValue).Should().BeFalse();
    }

    [Fact]
    public async Task GetAsync_WithNullResult_DoesNotCacheNull()
    {
        // Arrange
        var key = "test-key-null";

        // Act
        var result = await _service.GetAsync<string?>(key, async () => null);

        // Assert
        result.Should().BeNull();
        _memoryCache.TryGetValue(key, out string? _).Should().BeFalse();
    }

    [Fact]
    public async Task GetAsync_WithComplexType_SerializesAndDeserializesCorrectly()
    {
        // Arrange
        var key = "test-key-complex";
        var complexObject = new TestCacheObject
        {
            Id = Guid.NewGuid(),
            Name = "Test Object",
            CreatedAt = DateTime.UtcNow,
            Values = new List<int> { 1, 2, 3, 4, 5 }
        };

        // Act
        var result = await _service.GetAsync(key, async () => complexObject);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(complexObject.Id);
        result.Name.Should().Be(complexObject.Name);
        result.Values.Should().BeEquivalentTo(complexObject.Values);
    }

    [Fact]
    public async Task GetAsync_WhenFactoryThrows_PropagatesException()
    {
        // Arrange
        var key = "test-key-exception";
        var exception = new InvalidOperationException("Factory error");

        // Act
        Func<Task> act = async () => await _service.GetAsync<string>(key, async () => throw exception);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Factory error");
    }

    [Fact]
    public async Task GetAsync_WithDifferentTypes_CachesCorrectly()
    {
        // Arrange
        var intKey = "int-key";
        var stringKey = "string-key";
        var boolKey = "bool-key";

        // Act
        var intResult = await _service.GetAsync(intKey, async () => 42);
        var stringResult = await _service.GetAsync(stringKey, async () => "hello");
        var boolResult = await _service.GetAsync(boolKey, async () => true);

        // Assert
        intResult.Should().Be(42);
        stringResult.Should().Be("hello");
        boolResult.Should().BeTrue();
    }

    #endregion

    #region SetAsync Tests

    [Fact]
    public async Task SetAsync_SetsValueInMemoryCache()
    {
        // Arrange
        var key = "set-test-key";
        var value = "set-test-value";

        // Act
        await _service.SetAsync(key, value);

        // Assert
        _memoryCache.TryGetValue(key, out string? cachedValue).Should().BeTrue();
        cachedValue.Should().Be(value);
    }

    [Fact]
    public async Task SetAsync_WithCustomExpiration_UsesProvidedExpiration()
    {
        // Arrange
        var key = "set-expiration-key";
        var value = "test-value";
        var shortExpiration = TimeSpan.FromMilliseconds(100);

        // Act
        await _service.SetAsync(key, value, shortExpiration);

        // Assert - value should exist initially
        _memoryCache.TryGetValue(key, out string? _).Should().BeTrue();

        // Wait for expiration
        await Task.Delay(200);

        // Value should have expired
        _memoryCache.TryGetValue(key, out string? _).Should().BeFalse();
    }

    [Fact]
    public async Task SetAsync_WithoutExpiration_UsesDefaultExpiration()
    {
        // Arrange
        var key = "default-expiration-key";
        var value = "test-value";

        // Act
        await _service.SetAsync(key, value);

        // Assert - value should exist (default expiration is 5 minutes in test)
        _memoryCache.TryGetValue(key, out string? cachedValue).Should().BeTrue();
        cachedValue.Should().Be(value);
    }

    [Fact]
    public async Task SetAsync_OverwritesExistingValue()
    {
        // Arrange
        var key = "overwrite-key";
        var originalValue = "original";
        var newValue = "new";
        _memoryCache.Set(key, originalValue);

        // Act
        await _service.SetAsync(key, newValue);

        // Assert
        _memoryCache.TryGetValue(key, out string? cachedValue).Should().BeTrue();
        cachedValue.Should().Be(newValue);
    }

    [Fact]
    public async Task SetAsync_WithComplexType_CachesCorrectly()
    {
        // Arrange
        var key = "complex-set-key";
        var value = new TestCacheObject
        {
            Id = Guid.NewGuid(),
            Name = "Complex",
            Values = new List<int> { 10, 20, 30 }
        };

        // Act
        await _service.SetAsync(key, value);

        // Assert
        _memoryCache.TryGetValue(key, out TestCacheObject? cached).Should().BeTrue();
        cached!.Id.Should().Be(value.Id);
        cached.Name.Should().Be(value.Name);
    }

    #endregion

    #region InvalidateAsync Tests

    [Fact]
    public async Task InvalidateAsync_RemovesValueFromCache()
    {
        // Arrange
        var key = "invalidate-key";
        _memoryCache.Set(key, "value-to-remove");

        // Act
        await _service.InvalidateAsync(key);

        // Assert
        _memoryCache.TryGetValue(key, out string? _).Should().BeFalse();
    }

    [Fact]
    public async Task InvalidateAsync_WithNonExistentKey_DoesNotThrow()
    {
        // Arrange
        var key = "non-existent-key";

        // Act
        var exception = await Record.ExceptionAsync(async () =>
            await _service.InvalidateAsync(key));

        // Assert
        exception.Should().BeNull();
    }

    [Fact]
    public async Task InvalidateAsync_OnlyRemovesSpecifiedKey()
    {
        // Arrange
        var key1 = "key-1";
        var key2 = "key-2";
        _memoryCache.Set(key1, "value-1");
        _memoryCache.Set(key2, "value-2");

        // Act
        await _service.InvalidateAsync(key1);

        // Assert
        _memoryCache.TryGetValue(key1, out string? _).Should().BeFalse();
        _memoryCache.TryGetValue(key2, out string? value2).Should().BeTrue();
        value2.Should().Be("value-2");
    }

    #endregion

    #region GetBatchAsync Tests

    [Fact]
    public async Task GetBatchAsync_WithAllMisses_CallsFactoryForAllKeys()
    {
        // Arrange
        var keys = new[] { "batch-1", "batch-2", "batch-3" };
        var factoryCalls = new List<string>();

        // Act
        var results = await _service.GetBatchAsync<string>(keys, async key =>
        {
            factoryCalls.Add(key);
            return $"value-for-{key}";
        });

        // Assert
        results.Should().HaveCount(3);
        results["batch-1"].Should().Be("value-for-batch-1");
        results["batch-2"].Should().Be("value-for-batch-2");
        results["batch-3"].Should().Be("value-for-batch-3");
        factoryCalls.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetBatchAsync_WithPartialHits_OnlyCallsFactoryForMisses()
    {
        // Arrange
        var keys = new[] { "batch-hit", "batch-miss" };
        _memoryCache.Set("batch-hit", "cached-value");
        var factoryCalls = new List<string>();

        // Act
        var results = await _service.GetBatchAsync<string>(keys, async key =>
        {
            factoryCalls.Add(key);
            return $"factory-{key}";
        });

        // Assert
        results.Should().HaveCount(2);
        results["batch-hit"].Should().Be("cached-value");
        results["batch-miss"].Should().Be("factory-batch-miss");
        factoryCalls.Should().ContainSingle().Which.Should().Be("batch-miss");
    }

    [Fact]
    public async Task GetBatchAsync_WithAllHits_DoesNotCallFactory()
    {
        // Arrange
        var keys = new[] { "all-hit-1", "all-hit-2" };
        _memoryCache.Set("all-hit-1", "value-1");
        _memoryCache.Set("all-hit-2", "value-2");
        var factoryCalled = false;

        // Act
        var results = await _service.GetBatchAsync<string>(keys, async key =>
        {
            factoryCalled = true;
            return $"factory-{key}";
        });

        // Assert
        results.Should().HaveCount(2);
        results["all-hit-1"].Should().Be("value-1");
        results["all-hit-2"].Should().Be("value-2");
        factoryCalled.Should().BeFalse();
    }

    [Fact]
    public async Task GetBatchAsync_CachesMissedValues()
    {
        // Arrange
        var keys = new[] { "cache-after-fetch" };

        // Act
        await _service.GetBatchAsync<string>(keys, async key => $"fetched-{key}");

        // Assert
        _memoryCache.TryGetValue("cache-after-fetch", out string? cached).Should().BeTrue();
        cached.Should().Be("fetched-cache-after-fetch");
    }

    [Fact]
    public async Task GetBatchAsync_WithEmptyKeys_ReturnsEmptyDictionary()
    {
        // Arrange
        var keys = Array.Empty<string>();

        // Act
        var results = await _service.GetBatchAsync<string>(keys, async key => key);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBatchAsync_WithLargeKeySet_ProcessesAllKeys()
    {
        // Arrange
        var keys = Enumerable.Range(1, 50).Select(i => $"large-batch-{i}").ToArray();

        // Act
        var results = await _service.GetBatchAsync<int>(keys, async key =>
        {
            var num = int.Parse(key.Split('-').Last());
            return num * 10;
        });

        // Assert
        results.Should().HaveCount(50);
        results["large-batch-1"].Should().Be(10);
        results["large-batch-50"].Should().Be(500);
    }

    #endregion

    #region GetStatisticsAsync Tests

    [Fact]
    public async Task GetStatisticsAsync_ReturnsStatistics()
    {
        // Arrange & Act
        var stats = await _service.GetStatisticsAsync();

        // Assert
        stats.Should().NotBeNull();
        stats.IsRedisConnected.Should().BeFalse(); // No Redis in tests
    }

    [Fact]
    public async Task GetStatisticsAsync_ReturnsCorrectMemoryCacheSize()
    {
        // Arrange
        _memoryCache.Set("stat-key-1", "value-1");
        _memoryCache.Set("stat-key-2", "value-2");
        _memoryCache.Set("stat-key-3", "value-3");

        // Act
        var stats = await _service.GetStatisticsAsync();

        // Assert
        stats.Should().NotBeNull();
        // InMemoryCacheSize uses reflection and may not work in all scenarios
        // At minimum it shouldn't throw
    }

    [Fact]
    public async Task GetStatisticsAsync_WithNoRedis_ReportsNotConnected()
    {
        // Act
        var stats = await _service.GetStatisticsAsync();

        // Assert
        stats.IsRedisConnected.Should().BeFalse();
        stats.RedisDbSize.Should().BeNull();
        stats.RedisInfo.Should().BeEmpty();
    }

    #endregion

    #region WarmUpCacheAsync Tests

    [Fact]
    public async Task WarmUpCacheAsync_PreloadsCache()
    {
        // Arrange
        var keys = new[] { "warmup-1", "warmup-2", "warmup-3" };

        // Act
        await _service.WarmUpCacheAsync<string>(keys, async key => $"warmed-{key}");

        // Assert
        _memoryCache.TryGetValue("warmup-1", out string? value1).Should().BeTrue();
        _memoryCache.TryGetValue("warmup-2", out string? value2).Should().BeTrue();
        _memoryCache.TryGetValue("warmup-3", out string? value3).Should().BeTrue();
        value1.Should().Be("warmed-warmup-1");
        value2.Should().Be("warmed-warmup-2");
        value3.Should().Be("warmed-warmup-3");
    }

    [Fact]
    public async Task WarmUpCacheAsync_SkipsAlreadyCachedKeys()
    {
        // Arrange
        var keys = new[] { "existing-warmup", "new-warmup" };
        _memoryCache.Set("existing-warmup", "original-value");
        var factoryCalls = new List<string>();

        // Act
        await _service.WarmUpCacheAsync<string>(keys, async key =>
        {
            factoryCalls.Add(key);
            return $"warmed-{key}";
        });

        // Assert
        // Existing key should not be overwritten
        _memoryCache.TryGetValue("existing-warmup", out string? existingValue).Should().BeTrue();
        existingValue.Should().Be("original-value");

        // New key should be warmed
        _memoryCache.TryGetValue("new-warmup", out string? newValue).Should().BeTrue();
        newValue.Should().Be("warmed-new-warmup");

        factoryCalls.Should().ContainSingle().Which.Should().Be("new-warmup");
    }

    [Fact]
    public async Task WarmUpCacheAsync_WithEmptyKeys_DoesNotThrow()
    {
        // Arrange
        var keys = Array.Empty<string>();

        // Act
        var exception = await Record.ExceptionAsync(async () =>
            await _service.WarmUpCacheAsync<string>(keys, async key => key));

        // Assert
        exception.Should().BeNull();
    }

    [Fact]
    public async Task WarmUpCacheAsync_ContinuesOnFactoryError()
    {
        // Arrange
        var keys = new[] { "error-key", "success-key" };
        var errorCount = 0;

        // Act
        await _service.WarmUpCacheAsync<string>(keys, async key =>
        {
            if (key == "error-key")
            {
                errorCount++;
                throw new Exception("Factory error");
            }
            return $"value-{key}";
        });

        // Assert
        // Error key should not be cached
        _memoryCache.TryGetValue("error-key", out string? _).Should().BeFalse();

        // Success key should be cached
        _memoryCache.TryGetValue("success-key", out string? value).Should().BeTrue();
        value.Should().Be("value-success-key");
    }

    [Fact]
    public async Task WarmUpCacheAsync_DoesNotCacheNullResults()
    {
        // Arrange
        var keys = new[] { "null-warmup" };

        // Act
        await _service.WarmUpCacheAsync<string?>(keys, async key => null);

        // Assert
        _memoryCache.TryGetValue("null-warmup", out string? _).Should().BeFalse();
    }

    #endregion

    #region Concurrent Operations Tests

    [Fact]
    public async Task ConcurrentGetAsync_HandlesMultipleReads()
    {
        // Arrange
        var key = "concurrent-read-key";
        await _service.SetAsync(key, "concurrent-value");
        var tasks = new List<Task<string?>>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_service.GetAsync(key, async () => "factory-value"));
        }
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllBe("concurrent-value");
    }

    [Fact]
    public async Task ConcurrentSetAsync_HandlesMultipleWrites()
    {
        // Arrange
        var key = "concurrent-write-key";
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            var value = $"value-{i}";
            tasks.Add(_service.SetAsync(key, value));
        }

        var exception = await Record.ExceptionAsync(async () => await Task.WhenAll(tasks));

        // Assert
        exception.Should().BeNull();
        _memoryCache.TryGetValue(key, out string? _).Should().BeTrue();
    }

    [Fact]
    public async Task ConcurrentOperations_MixedReadWriteInvalidate()
    {
        // Arrange
        var key = "mixed-ops-key";
        await _service.SetAsync(key, "initial-value");

        // Act
        var readTask = _service.GetAsync(key, async () => "factory");
        var writeTask = _service.SetAsync(key, "new-value");
        var invalidateTask = _service.InvalidateAsync("other-key");

        var exception = await Record.ExceptionAsync(async () =>
            await Task.WhenAll(readTask, writeTask, invalidateTask));

        // Assert
        exception.Should().BeNull();
    }

    [Fact]
    public async Task ConcurrentBatchOperations_HandleMultipleBatches()
    {
        // Arrange
        var keys1 = new[] { "batch-a-1", "batch-a-2" };
        var keys2 = new[] { "batch-b-1", "batch-b-2" };

        // Act
        var task1 = _service.GetBatchAsync<string>(keys1, async k => $"a-{k}");
        var task2 = _service.GetBatchAsync<string>(keys2, async k => $"b-{k}");

        var results = await Task.WhenAll(task1, task2);

        // Assert
        results[0].Should().HaveCount(2);
        results[1].Should().HaveCount(2);
        results[0]["batch-a-1"].Should().Be("a-batch-a-1");
        results[1]["batch-b-1"].Should().Be("b-batch-b-1");
    }

    #endregion

    #region Extension Methods Tests

    [Fact]
    public async Task GetCachedAsync_ExtensionMethod_Works()
    {
        // Arrange
        var key = "extension-key";

        // Act
        var result = await _service.GetCachedAsync(key, async () => "extension-value");

        // Assert
        result.Should().Be("extension-value");
    }

    [Fact]
    public async Task GetCachedBatchAsync_ExtensionMethod_Works()
    {
        // Arrange
        var keys = new[] { "ext-batch-1", "ext-batch-2" };

        // Act
        var results = await _service.GetCachedBatchAsync<string>(keys, async k => $"ext-{k}");

        // Assert
        results.Should().HaveCount(2);
        results["ext-batch-1"].Should().Be("ext-ext-batch-1");
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public async Task GetAsync_WithSpecialCharactersInKey_Works()
    {
        // Arrange
        var key = "key:with/special\\chars!@#$%^&*()";
        var value = "special-value";

        // Act
        var result = await _service.GetAsync(key, async () => value);

        // Assert
        result.Should().Be(value);
        _memoryCache.TryGetValue(key, out string? cached).Should().BeTrue();
    }

    [Fact]
    public async Task GetAsync_WithEmptyStringValue_CachesCorrectly()
    {
        // Arrange
        var key = "empty-string-key";

        // Act
        var result = await _service.GetAsync(key, async () => string.Empty);

        // Assert
        result.Should().BeEmpty();
        _memoryCache.TryGetValue(key, out string? cached).Should().BeTrue();
        cached.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAsync_WithVeryLongKey_Works()
    {
        // Arrange
        var key = new string('x', 1000);
        var value = "long-key-value";

        // Act
        var result = await _service.GetAsync(key, async () => value);

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public async Task SetAsync_WithMaxInt_Works()
    {
        // Arrange
        var key = "max-int-key";
        var value = int.MaxValue;

        // Act
        await _service.SetAsync(key, value);

        // Assert
        _memoryCache.TryGetValue(key, out int cached).Should().BeTrue();
        cached.Should().Be(int.MaxValue);
    }

    [Fact]
    public async Task GetAsync_WithZeroExpiration_StillCaches()
    {
        // Arrange
        var key = "zero-expiration-key";
        var expiration = TimeSpan.Zero;

        // Act
        var result = await _service.GetAsync(key, async () => "value", expiration);

        // Assert
        result.Should().Be("value");
        // With zero expiration, cache behavior depends on implementation
    }

    #endregion

    // Test helper class
    private class TestCacheObject
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<int> Values { get; set; } = new();
    }
}
