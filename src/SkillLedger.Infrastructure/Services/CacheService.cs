using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.Interfaces;
using SkillLedger.Core.Models;
using StackExchange.Redis;

namespace SkillLedger.Infrastructure.Services;

/// <summary>
/// Distributed caching service with Redis primary and in-memory fallback
/// </summary>
public class CacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly IMemoryCache _memoryCache;
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<CacheService> _logger;
    private readonly bool _redisAvailable;

    public CacheService(
        IDistributedCache distributedCache,
        IMemoryCache memoryCache,
        ILogger<CacheService> logger,
        IConnectionMultiplexer? redis = null)
    {
        _distributedCache = distributedCache;
        _memoryCache = memoryCache;
        _logger = logger;
        _redis = redis;
        _redisAvailable = redis?.IsConnected ?? false;

        if (!_redisAvailable)
        {
            _logger.LogWarning("Redis is not available. Using in-memory cache as fallback.");
        }
        else
        {
            _logger.LogInformation("Redis cache is connected and ready.");
        }
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key));
        }

        try
        {
            // Try Redis first if available
            if (_redisAvailable)
            {
                var cachedData = await _distributedCache.GetStringAsync(key);
                if (!string.IsNullOrEmpty(cachedData))
                {
                    _logger.LogDebug("Cache hit (Redis): {Key}", key);
                    return JsonSerializer.Deserialize<T>(cachedData);
                }
            }

            // Fallback to in-memory cache
            if (_memoryCache.TryGetValue(key, out T? cachedValue))
            {
                _logger.LogDebug("Cache hit (Memory): {Key}", key);
                return cachedValue;
            }

            _logger.LogDebug("Cache miss: {Key}", key);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving from cache: {Key}", key);

            // Try memory cache as ultimate fallback
            if (_memoryCache.TryGetValue(key, out T? fallbackValue))
            {
                return fallbackValue;
            }

            return null;
        }
    }

    public async Task<bool> SetAsync<T>(string key, T value, TimeSpan expiration) where T : class
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };

            // Set in Redis if available
            if (_redisAvailable)
            {
                var serializedValue = JsonSerializer.Serialize(value);
                await _distributedCache.SetStringAsync(key, serializedValue, options);
                _logger.LogDebug("Cached in Redis: {Key}, TTL: {Expiration}", key, expiration);
            }

            // Always set in memory cache as backup
            var memoryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };
            _memoryCache.Set(key, value, memoryOptions);
            _logger.LogDebug("Cached in Memory: {Key}, TTL: {Expiration}", key, expiration);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache: {Key}", key);

            // Try to at least cache in memory
            try
            {
                var memoryOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration
                };
                _memoryCache.Set(key, value, memoryOptions);
                return true;
            }
            catch (Exception memEx)
            {
                _logger.LogError(memEx, "Failed to cache in memory: {Key}", key);
                return false;
            }
        }
    }

    public async Task<bool> RemoveAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key));
        }

        try
        {
            // Remove from Redis if available
            if (_redisAvailable)
            {
                await _distributedCache.RemoveAsync(key);
                _logger.LogDebug("Removed from Redis cache: {Key}", key);
            }

            // Remove from memory cache
            _memoryCache.Remove(key);
            _logger.LogDebug("Removed from memory cache: {Key}", key);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing from cache: {Key}", key);
            return false;
        }
    }

    public async Task<int> RemoveByPatternAsync(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new ArgumentNullException(nameof(pattern));
        }

        var removedCount = 0;

        try
        {
            // Remove from Redis if available
            if (_redisAvailable && _redis != null)
            {
                var database = _redis.GetDatabase();
                var endpoints = _redis.GetEndPoints();

                foreach (var endpoint in endpoints)
                {
                    var server = _redis.GetServer(endpoint);

                    // Scan for keys matching pattern
                    var keys = server.Keys(pattern: pattern).ToArray();

                    if (keys.Length > 0)
                    {
                        await database.KeyDeleteAsync(keys);
                        removedCount += keys.Length;
                        _logger.LogDebug("Removed {Count} keys from Redis matching pattern: {Pattern}", keys.Length, pattern);
                    }
                }
            }

            // Note: In-memory cache doesn't support pattern-based removal efficiently
            // Individual keys would need to be tracked separately for this
            _logger.LogDebug("Pattern-based removal from memory cache not implemented. Pattern: {Pattern}", pattern);

            return removedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing by pattern from cache: {Pattern}", pattern);
            return removedCount;
        }
    }

    public async Task<bool> IsRedisAvailableAsync()
    {
        try
        {
            if (_redis == null)
            {
                return false;
            }

            return await Task.FromResult(_redis.IsConnected);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Redis availability");
            return false;
        }
    }

    public async Task<CacheStatistics> GetStatisticsAsync()
    {
        var stats = new CacheStatistics
        {
            IsRedisConnected = _redisAvailable
        };

        try
        {
            if (_redisAvailable && _redis != null)
            {
                var database = _redis.GetDatabase();
                var endpoints = _redis.GetEndPoints();

                foreach (var endpoint in endpoints)
                {
                    var server = _redis.GetServer(endpoint);

                    // BUG-HIGH-003 FIX: Replace ContinueWith with proper try-catch pattern
                    // Get database size
                    try
                    {
                        var dbSizeResult = await database.ExecuteAsync("DBSIZE");
                        stats.RedisDbSize = (long?)dbSizeResult;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to get Redis database size");
                        stats.RedisDbSize = null;
                    }

                    // Get Redis info
                    var info = await server.InfoAsync("Stats");
                    stats.RedisInfo = info.ToString() ?? string.Empty;

                    break; // Only need one endpoint's stats
                }
            }

            // Memory cache size (approximate - counts tracked entries)
            if (_memoryCache is MemoryCache memCache)
            {
                stats.InMemoryCacheSize = memCache.Count;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache statistics");
        }

        return stats;
    }
}
