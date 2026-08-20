using SkillLedger.Core.Models;

namespace SkillLedger.Core.Interfaces;

/// <summary>
/// Service for caching frequently accessed data with Redis and in-memory fallback
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets a cached value by key
    /// </summary>
    /// <typeparam name="T">The type of the cached value</typeparam>
    /// <param name="key">The cache key</param>
    /// <returns>The cached value if found, otherwise null</returns>
    Task<T?> GetAsync<T>(string key) where T : class;

    /// <summary>
    /// Sets a value in the cache with expiration
    /// </summary>
    /// <typeparam name="T">The type of the value to cache</typeparam>
    /// <param name="key">The cache key</param>
    /// <param name="value">The value to cache</param>
    /// <param name="expiration">The cache expiration time</param>
    /// <returns>True if successful, otherwise false</returns>
    Task<bool> SetAsync<T>(string key, T value, TimeSpan expiration) where T : class;

    /// <summary>
    /// Removes a cached value by key
    /// </summary>
    /// <param name="key">The cache key to remove</param>
    /// <returns>True if successful, otherwise false</returns>
    Task<bool> RemoveAsync(string key);

    /// <summary>
    /// Removes all cached values matching a pattern (e.g., "user:123:*")
    /// </summary>
    /// <param name="pattern">The key pattern to match (supports wildcards)</param>
    /// <returns>Number of keys removed</returns>
    Task<int> RemoveByPatternAsync(string pattern);

    /// <summary>
    /// Checks if Redis is available
    /// </summary>
    /// <returns>True if Redis is connected, otherwise false</returns>
    Task<bool> IsRedisAvailableAsync();

    /// <summary>
    /// Gets cache statistics (hits, misses, etc.)
    /// </summary>
    /// <returns>Cache statistics</returns>
    Task<CacheStatistics> GetStatisticsAsync();
}
