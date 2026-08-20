using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkillLedger.Infrastructure.Services;
using SkillLedger.Tests.Infrastructure;
using SkillLedger.Tests.Mocks;
using Xunit;

namespace SkillLedger.Tests.Integration.Services;

/// <summary>
/// Integration tests for CacheInvalidationService using real caching infrastructure
/// Following anti-mocking pattern: Use real in-memory and distributed caches
/// </summary>
[IntegrationTest]
public class CacheInvalidationServiceIntegrationTests : IDisposable
{
    private readonly CacheInvalidationService _cacheService;
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<CacheInvalidationService> _logger;

    public CacheInvalidationServiceIntegrationTests()
    {
        // Use real in-memory cache
        _memoryCache = new MemoryCache(new MemoryCacheOptions());

        // Use real in-memory distributed cache for testing
        _distributedCache = new MockDistributedCache();

        _logger = LoggerFactory
            .Create(builder => builder.AddConsole())
            .CreateLogger<CacheInvalidationService>();

        _cacheService = new CacheInvalidationService(_memoryCache, _distributedCache, _logger);
    }

    public void Dispose()
    {
        (_memoryCache as IDisposable)?.Dispose();
    }

    [Fact]
    public async Task InvalidateAsync_SingleKey_ShouldRemoveFromMemoryCache()
    {
        // Arrange
        const string cacheKey = "test:key:1";
        _memoryCache.Set(cacheKey, "test value");
        CacheInvalidationService.TrackCacheKey(cacheKey);

        // Act
        await _cacheService.InvalidateAsync(cacheKey);

        // Assert
        var cachedValue = _memoryCache.Get(cacheKey);
        cachedValue.Should().BeNull();
    }

    [Fact]
    public async Task InvalidateAsync_SingleKey_ShouldRemoveFromDistributedCache()
    {
        // Arrange
        const string cacheKey = "test:key:distributed";
        await _distributedCache.SetStringAsync(cacheKey, "test value");
        CacheInvalidationService.TrackCacheKey(cacheKey);

        // Act
        await _cacheService.InvalidateAsync(cacheKey);

        // Assert
        var cachedValue = await _distributedCache.GetStringAsync(cacheKey);
        cachedValue.Should().BeNull();
    }

    [Fact]
    public async Task InvalidatePatternAsync_WildcardMatch_ShouldInvalidateMatchingKeys()
    {
        // Arrange
        const string pattern = "user:*";
        var keys = new[]
        {
            "user:123",
            "user:456",
            "user:789",
            "project:123" // Should NOT be invalidated
        };

        foreach (var key in keys)
        {
            _memoryCache.Set(key, "test value");
            CacheInvalidationService.TrackCacheKey(key);
        }

        // Act
        await _cacheService.InvalidatePatternAsync(pattern);

        // Assert
        _memoryCache.Get("user:123").Should().BeNull();
        _memoryCache.Get("user:456").Should().BeNull();
        _memoryCache.Get("user:789").Should().BeNull();
        _memoryCache.Get("project:123").Should().NotBeNull(); // Not matching pattern
    }

    [Fact]
    public async Task InvalidatePatternAsync_ExactMatch_ShouldInvalidateExactKeyOnly()
    {
        // Arrange
        const string pattern = "exact:key";
        var keys = new[]
        {
            "exact:key",
            "exact:key:suffix",
            "prefix:exact:key"
        };

        foreach (var key in keys)
        {
            _memoryCache.Set(key, "test value");
            CacheInvalidationService.TrackCacheKey(key);
        }

        // Act
        await _cacheService.InvalidatePatternAsync(pattern);

        // Assert
        _memoryCache.Get("exact:key").Should().BeNull();
        _memoryCache.Get("exact:key:suffix").Should().NotBeNull();
        _memoryCache.Get("prefix:exact:key").Should().NotBeNull();
    }

    [Fact]
    public async Task InvalidateMultipleAsync_ShouldInvalidateAllSpecifiedKeys()
    {
        // Arrange
        var keys = new[]
        {
            "key:1",
            "key:2",
            "key:3"
        };

        foreach (var key in keys)
        {
            _memoryCache.Set(key, "test value");
            CacheInvalidationService.TrackCacheKey(key);
        }

        // Act
        await _cacheService.InvalidateMultipleAsync(keys);

        // Assert
        foreach (var key in keys)
        {
            _memoryCache.Get(key).Should().BeNull();
        }
    }

    [Fact]
    public async Task InvalidateUserCacheAsync_ShouldInvalidateAllUserRelatedKeys()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userKeys = new[]
        {
            $"user:{userId}",
            $"user:{userId}:profile",
            $"user:{userId}:skills",
            $"user:{userId}:projects",
            $"user:{userId}:credits",
            $"user:{userId}:reviews"
        };

        foreach (var key in userKeys)
        {
            _memoryCache.Set(key, "test value");
            CacheInvalidationService.TrackCacheKey(key);
        }

        // Add a non-user key that should NOT be invalidated
        var otherKey = "project:123";
        _memoryCache.Set(otherKey, "test value");

        // Act
        await _cacheService.InvalidateUserCacheAsync(userId);

        // Assert
        foreach (var key in userKeys)
        {
            _memoryCache.Get(key).Should().BeNull();
        }
        _memoryCache.Get(otherKey).Should().NotBeNull();
    }

    [Fact]
    public async Task InvalidateProjectCacheAsync_ShouldInvalidateAllProjectRelatedKeys()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var projectKeys = new[]
        {
            $"project:{projectId}",
            $"project:{projectId}:details",
            $"project:{projectId}:applications",
            $"project:{projectId}:deliverables",
            "projects:search",
            "projects:featured"
        };

        foreach (var key in projectKeys)
        {
            _memoryCache.Set(key, "test value");
            CacheInvalidationService.TrackCacheKey(key);
        }

        // Add a non-project key that should NOT be invalidated
        var otherKey = "user:123";
        _memoryCache.Set(otherKey, "test value");

        // Act
        await _cacheService.InvalidateProjectCacheAsync(projectId);

        // Assert
        foreach (var key in projectKeys)
        {
            _memoryCache.Get(key).Should().BeNull();
        }
        _memoryCache.Get(otherKey).Should().NotBeNull();
    }

    [Fact]
    public async Task InvalidateSkillCacheAsync_ShouldInvalidateAllSkillRelatedKeys()
    {
        // Arrange
        var skillId = Guid.NewGuid();
        var skillKeys = new[]
        {
            $"skill:{skillId}",
            "skills:all",
            "skills:categories",
            $"skill:{skillId}:users"
        };

        foreach (var key in skillKeys)
        {
            _memoryCache.Set(key, "test value");
            CacheInvalidationService.TrackCacheKey(key);
        }

        // Add a non-skill key that should NOT be invalidated
        var otherKey = "user:123";
        _memoryCache.Set(otherKey, "test value");

        // Act
        await _cacheService.InvalidateSkillCacheAsync(skillId);

        // Assert
        foreach (var key in skillKeys)
        {
            _memoryCache.Get(key).Should().BeNull();
        }
        _memoryCache.Get(otherKey).Should().NotBeNull();
    }

    [Fact]
    public async Task TrackCacheKey_ShouldAddKeyToTracking()
    {
        // Arrange
        const string cacheKey = "tracked:key";

        // Act
        CacheInvalidationService.TrackCacheKey(cacheKey);
        _memoryCache.Set(cacheKey, "test value");

        // Assert - Key can be used in pattern matching after tracking
        await _cacheService.InvalidatePatternAsync("tracked:*");

        _memoryCache.Get(cacheKey).Should().BeNull();
    }

    [Fact]
    public async Task SetWithTracking_ShouldSetCacheAndTrackKey()
    {
        // Arrange
        const string cacheKey = "extension:key";
        const string value = "extension value";
        var expiration = TimeSpan.FromMinutes(5);

        // Act
        _memoryCache.SetWithTracking(cacheKey, value, expiration);

        // Assert
        var cachedValue = _memoryCache.Get<string>(cacheKey);
        cachedValue.Should().Be(value);

        // Verify tracking by invalidating with pattern
        await _cacheService.InvalidatePatternAsync("extension:*");

        _memoryCache.Get(cacheKey).Should().BeNull();
    }

    [Fact]
    public async Task SetJsonAsync_ShouldSerializeAndStoreObject()
    {
        // Arrange
        const string cacheKey = "json:object";
        var testObject = new TestCacheObject
        {
            Id = Guid.NewGuid(),
            Name = "Test Object",
            Value = 42
        };
        var expiration = TimeSpan.FromMinutes(5);

        // Act
        await _distributedCache.SetJsonAsync(cacheKey, testObject, expiration);

        // Assert
        var cachedObject = await _distributedCache.GetJsonAsync<TestCacheObject>(cacheKey);
        cachedObject.Should().NotBeNull();
        cachedObject!.Id.Should().Be(testObject.Id);
        cachedObject.Name.Should().Be(testObject.Name);
        cachedObject.Value.Should().Be(testObject.Value);
    }

    [Fact]
    public async Task GetJsonAsync_NonExistentKey_ShouldReturnNull()
    {
        // Arrange
        const string cacheKey = "nonexistent:key";

        // Act
        var result = await _distributedCache.GetJsonAsync<TestCacheObject>(cacheKey);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task InvalidateAsync_NonExistentKey_ShouldNotThrowException()
    {
        // Arrange
        const string cacheKey = "does:not:exist";

        // Act
        Func<Task> act = async () => await _cacheService.InvalidateAsync(cacheKey);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task InvalidatePatternAsync_NoMatchingKeys_ShouldNotThrowException()
    {
        // Arrange
        const string pattern = "nomatch:*";

        // Act
        Func<Task> act = async () => await _cacheService.InvalidatePatternAsync(pattern);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task InvalidateMultipleAsync_EmptyList_ShouldNotThrowException()
    {
        // Arrange
        var emptyKeys = Array.Empty<string>();

        // Act
        Func<Task> act = async () => await _cacheService.InvalidateMultipleAsync(emptyKeys);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ConcurrentInvalidation_ShouldHandleMultipleThreadsSafely()
    {
        // Arrange
        var keys = Enumerable.Range(1, 100).Select(i => $"concurrent:key:{i}").ToList();
        foreach (var key in keys)
        {
            _memoryCache.Set(key, "test value");
            CacheInvalidationService.TrackCacheKey(key);
        }

        // Act - Invalidate keys concurrently
        var tasks = keys.Select(key => _cacheService.InvalidateAsync(key));
        await Task.WhenAll(tasks);

        // Assert - All keys should be invalidated
        foreach (var key in keys)
        {
            _memoryCache.Get(key).Should().BeNull();
        }
    }

    [Fact]
    public async Task PatternInvalidation_WithOverlappingPatterns_ShouldWork()
    {
        // Arrange
        var keys = new[]
        {
            "user:123:profile",
            "user:123:skills",
            "user:456:profile",
            "user:456:skills"
        };

        foreach (var key in keys)
        {
            _memoryCache.Set(key, "test value");
            CacheInvalidationService.TrackCacheKey(key);
        }

        // Act - Invalidate with more specific pattern first, then broader pattern
        await _cacheService.InvalidatePatternAsync("user:123:*");
        await _cacheService.InvalidatePatternAsync("user:*");

        // Assert - All keys should be invalidated
        foreach (var key in keys)
        {
            _memoryCache.Get(key).Should().BeNull();
        }
    }

    [Fact]
    public async Task InvalidateUserCacheAsync_DifferentUsers_ShouldOnlyInvalidateSpecificUser()
    {
        // Arrange
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();

        var user1Keys = new[] { $"user:{user1Id}", $"user:{user1Id}:profile" };
        var user2Keys = new[] { $"user:{user2Id}", $"user:{user2Id}:profile" };

        foreach (var key in user1Keys.Concat(user2Keys))
        {
            _memoryCache.Set(key, "test value");
            CacheInvalidationService.TrackCacheKey(key);
        }

        // Act - Invalidate only user1
        await _cacheService.InvalidateUserCacheAsync(user1Id);

        // Assert
        foreach (var key in user1Keys)
        {
            _memoryCache.Get(key).Should().BeNull();
        }
        foreach (var key in user2Keys)
        {
            _memoryCache.Get(key).Should().NotBeNull();
        }
    }

    [Fact]
    public async Task SetJsonAsync_ThenInvalidate_ShouldRemoveFromCache()
    {
        // Arrange
        const string cacheKey = "json:invalidate:test";
        var testObject = new TestCacheObject { Id = Guid.NewGuid(), Name = "Test", Value = 100 };
        await _distributedCache.SetJsonAsync(cacheKey, testObject, TimeSpan.FromMinutes(5));

        // Act
        await _cacheService.InvalidateAsync(cacheKey);

        // Assert
        var result = await _distributedCache.GetJsonAsync<TestCacheObject>(cacheKey);
        result.Should().BeNull();
    }

    private class TestCacheObject
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}
