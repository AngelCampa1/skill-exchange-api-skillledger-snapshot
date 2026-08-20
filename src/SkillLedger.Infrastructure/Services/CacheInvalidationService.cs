using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace SkillLedger.Infrastructure.Services;

/// <summary>
/// P1 DATA CONSISTENCY FIX: Service for distributed cache invalidation
/// Ensures cache consistency across multiple servers/instances
/// </summary>
public interface ICacheInvalidationService
{
    Task InvalidateAsync(string cacheKey);
    Task InvalidatePatternAsync(string pattern);
    Task InvalidateMultipleAsync(IEnumerable<string> cacheKeys);
    Task InvalidateUserCacheAsync(Guid userId);
    Task InvalidateProjectCacheAsync(Guid projectId);
    Task InvalidateSkillCacheAsync(Guid skillId);
}

public class CacheInvalidationService : ICacheInvalidationService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache? _distributedCache;
    private readonly ILogger<CacheInvalidationService> _logger;

    // PERFORMANCE FIX: Track cache keys with timestamps for LRU eviction
    // Changed from ConcurrentDictionary<string, byte> to include last access time
    private static readonly ConcurrentDictionary<string, long> _cacheKeys = new();
    private static readonly int MaxCacheKeys = 10000; // Reduced from 100K to 10K for better performance
    private static long _accessCounter = 0;

    public CacheInvalidationService(
        IMemoryCache memoryCache,
        IDistributedCache distributedCache,
        ILogger<CacheInvalidationService> logger)
    {
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _logger = logger;
    }

    /// <summary>
    /// P1 FIX: Invalidate a single cache entry across all cache layers
    /// </summary>
    public async Task InvalidateAsync(string cacheKey)
    {
        try
        {
            // Remove from memory cache
            _memoryCache.Remove(cacheKey);

            // Remove from distributed cache (Redis)
            if (_distributedCache != null)
            {
                await _distributedCache.RemoveAsync(cacheKey);
            }

            // Remove from tracking
            _cacheKeys.TryRemove(cacheKey, out _);

            _logger.LogInformation("Cache invalidated: {CacheKey}", cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invalidate cache key: {CacheKey}", cacheKey);
        }
    }

    /// <summary>
    /// P1 FIX: Invalidate multiple cache entries matching a pattern
    /// Example: "user:*" invalidates all user-related cache entries
    /// </summary>
    public async Task InvalidatePatternAsync(string pattern)
    {
        try
        {
            var matchingKeys = _cacheKeys.Keys
                .Where(key => IsMatch(key, pattern))
                .ToList();

            _logger.LogInformation(
                "Invalidating {Count} cache entries matching pattern: {Pattern}",
                matchingKeys.Count,
                pattern);

            foreach (var key in matchingKeys)
            {
                await InvalidateAsync(key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invalidate cache pattern: {Pattern}", pattern);
        }
    }

    /// <summary>
    /// P1 FIX: Invalidate multiple specific cache keys
    /// </summary>
    public async Task InvalidateMultipleAsync(IEnumerable<string> cacheKeys)
    {
        var keys = cacheKeys.ToList();
        _logger.LogInformation("Invalidating {Count} cache entries", keys.Count);

        foreach (var key in keys)
        {
            await InvalidateAsync(key);
        }
    }

    /// <summary>
    /// P1 FIX: Invalidate all cache entries related to a user
    /// </summary>
    public async Task InvalidateUserCacheAsync(Guid userId)
    {
        var keysToInvalidate = new List<string>
        {
            $"user:{userId}",
            $"user:{userId}:profile",
            $"user:{userId}:skills",
            $"user:{userId}:projects",
            $"user:{userId}:credits",
            $"user:{userId}:reviews"
        };

        await InvalidateMultipleAsync(keysToInvalidate);
        _logger.LogInformation("Invalidated all cache for user: {UserId}", userId);
    }

    /// <summary>
    /// P1 FIX: Invalidate all cache entries related to a project
    /// </summary>
    public async Task InvalidateProjectCacheAsync(Guid projectId)
    {
        var keysToInvalidate = new List<string>
        {
            $"project:{projectId}",
            $"project:{projectId}:details",
            $"project:{projectId}:applications",
            $"project:{projectId}:deliverables",
            "projects:search", // Invalidate search results
            "projects:featured"
        };

        await InvalidateMultipleAsync(keysToInvalidate);
        _logger.LogInformation("Invalidated all cache for project: {ProjectId}", projectId);
    }

    /// <summary>
    /// P1 FIX: Invalidate all cache entries related to a skill
    /// </summary>
    public async Task InvalidateSkillCacheAsync(Guid skillId)
    {
        var keysToInvalidate = new List<string>
        {
            $"skill:{skillId}",
            "skills:all",
            "skills:categories",
            $"skill:{skillId}:users"
        };

        await InvalidateMultipleAsync(keysToInvalidate);
        _logger.LogInformation("Invalidated all cache for skill: {SkillId}", skillId);
    }

    /// <summary>
    /// Helper method to track cache keys for pattern matching with LRU eviction
    /// Call this when setting cache entries
    /// </summary>
    /// <remarks>
    /// PERFORMANCE FIX: Implements proper LRU eviction instead of arbitrary removal.
    /// - Max size reduced from 100K to 10K for better performance
    /// - Evicts least recently used entries when limit is reached
    /// - Uses atomic counter for access tracking
    /// </remarks>
    public static void TrackCacheKey(string cacheKey)
    {
        // Update access time with atomic increment
        var accessTime = Interlocked.Increment(ref _accessCounter);
        _cacheKeys.AddOrUpdate(cacheKey, accessTime, (_, __) => accessTime);

        // Perform LRU eviction if dictionary grows too large
        if (_cacheKeys.Count > MaxCacheKeys)
        {
            // Find and remove the oldest 10% of entries (LRU eviction)
            var evictionCount = MaxCacheKeys / 10; // Remove 1,000 entries
            var oldestKeys = _cacheKeys
                .OrderBy(kvp => kvp.Value) // Sort by access time (oldest first)
                .Take(evictionCount)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in oldestKeys)
            {
                _cacheKeys.TryRemove(key, out _);
            }
        }
    }

    /// <summary>
    /// Simple wildcard pattern matching
    /// Supports * wildcard at the end
    /// Example: "user:*" matches "user:123", "user:456", etc.
    /// </summary>
    private static bool IsMatch(string value, string pattern)
    {
        if (pattern.EndsWith("*"))
        {
            var prefix = pattern.Substring(0, pattern.Length - 1);
            return value.StartsWith(prefix);
        }

        return value == pattern;
    }
}

/// <summary>
/// P1 FIX: Extension methods for easy cache invalidation
/// </summary>
public static class CacheExtensions
{
    /// <summary>
    /// Set cache entry and track it for invalidation
    /// </summary>
    public static void SetWithTracking(this IMemoryCache cache, string key, object value, TimeSpan expiration)
    {
        cache.Set(key, value, expiration);
        CacheInvalidationService.TrackCacheKey(key);
    }

    /// <summary>
    /// Set distributed cache entry with JSON serialization
    /// </summary>
    public static async Task SetJsonAsync<T>(
        this IDistributedCache cache,
        string key,
        T value,
        TimeSpan expiration)
    {
        var json = JsonSerializer.Serialize(value);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
        };
        await cache.SetStringAsync(key, json, options);
        CacheInvalidationService.TrackCacheKey(key);
    }

    /// <summary>
    /// Get distributed cache entry with JSON deserialization
    /// </summary>
    public static async Task<T?> GetJsonAsync<T>(this IDistributedCache cache, string key)
    {
        var json = await cache.GetStringAsync(key);
        if (string.IsNullOrEmpty(json))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(json);
    }
}

