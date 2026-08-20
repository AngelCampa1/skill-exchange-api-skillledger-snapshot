using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkillLedger.Core.Models;
using StackExchange.Redis;
using System.Diagnostics;
using System.Text.Json;

namespace SkillLedger.Infrastructure.Services;

public class PerformanceOptimizationService
{
    private readonly ILogger<PerformanceOptimizationService> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly IConnectionMultiplexer? _redisConnection;
    private readonly PerformanceOptimizationSettings _settings;
    private readonly ActivitySource _activitySource;

    public PerformanceOptimizationService(
        ILogger<PerformanceOptimizationService> logger,
        IMemoryCache memoryCache,
        IConnectionMultiplexer? redisConnection,
        IOptions<PerformanceOptimizationSettings> settings)
    {
        _logger = logger;
        _memoryCache = memoryCache;
        _redisConnection = redisConnection;
        _settings = settings.Value;
        _activitySource = new ActivitySource("SkillLedger.Performance");
    }

    /// <summary>
    /// Cache data with intelligent fallback strategy
    /// </summary>
    public async Task<T?> GetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
    {
        using var activity = _activitySource.StartActivity("Cache.Get");
        activity?.SetTag("cache.key", key);
        activity?.SetTag("cache.type", typeof(T).Name);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Try Redis first if available
            if (_redisConnection?.IsConnected == true && _settings.UseRedisCache)
            {
                var redisValue = await _redisConnection.GetDatabase().StringGetAsync(key);
                if (redisValue.HasValue)
                {
                    var result = JsonSerializer.Deserialize<T>(redisValue!);
                    stopwatch.Stop();
                    activity?.SetTag("cache.hit", "redis");
                    activity?.SetTag("cache.duration_ms", stopwatch.ElapsedMilliseconds);
                    _logger.LogDebug("Cache hit (Redis) for key: {Key} in {ElapsedMs}ms", key, stopwatch.ElapsedMilliseconds);
                    return result;
                }
            }

            // Try memory cache
            if (_memoryCache.TryGetValue(key, out T? memoryValue))
            {
                stopwatch.Stop();
                activity?.SetTag("cache.hit", "memory");
                activity?.SetTag("cache.duration_ms", stopwatch.ElapsedMilliseconds);
                _logger.LogDebug("Cache hit (Memory) for key: {Key} in {ElapsedMs}ms", key, stopwatch.ElapsedMilliseconds);
                return memoryValue;
            }

            // Cache miss - fetch data
            activity?.SetTag("cache.hit", "miss");
            var data = await factory();

            if (data != null)
            {
                await SetAsync(key, data, expiration);
            }

            stopwatch.Stop();
            activity?.SetTag("cache.duration_ms", stopwatch.ElapsedMilliseconds);
            _logger.LogDebug("Cache miss for key: {Key}, fetched in {ElapsedMs}ms", key, stopwatch.ElapsedMilliseconds);
            return data;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetTag("cache.error", ex.Message);
            activity?.SetTag("cache.duration_ms", stopwatch.ElapsedMilliseconds);
            _logger.LogError(ex, "Error getting cache for key: {Key}", key);
            return await factory(); // Fallback to direct fetch
        }
    }

    /// <summary>
    /// Set cache data with intelligent distribution
    /// </summary>
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        using var activity = _activitySource.StartActivity("Cache.Set");
        activity?.SetTag("cache.key", key);
        activity?.SetTag("cache.type", typeof(T).Name);

        var expirationTime = expiration ?? _settings.DefaultCacheExpiration;
        var serializedValue = JsonSerializer.Serialize(value);

        try
        {
            // Set in memory cache
            _memoryCache.Set(key, value, expirationTime);

            // Set in Redis if available
            if (_redisConnection?.IsConnected == true && _settings.UseRedisCache)
            {
                await _redisConnection.GetDatabase().StringSetAsync(
                    key,
                    serializedValue,
                    expirationTime);
            }

            _logger.LogDebug("Cache set for key: {Key}, expiration: {Expiration}", key, expirationTime);
        }
        catch (Exception ex)
        {
            activity?.SetTag("cache.error", ex.Message);
            _logger.LogError(ex, "Error setting cache for key: {Key}", key);
        }
    }

    /// <summary>
    /// Invalidate cache across all layers
    /// </summary>
    public async Task InvalidateAsync(string key)
    {
        using var activity = _activitySource.StartActivity("Cache.Invalidate");
        activity?.SetTag("cache.key", key);

        try
        {
            // Remove from memory cache
            _memoryCache.Remove(key);

            // Remove from Redis if available
            if (_redisConnection?.IsConnected == true)
            {
                await _redisConnection.GetDatabase().KeyDeleteAsync(key);
            }

            _logger.LogDebug("Cache invalidated for key: {Key}", key);
        }
        catch (Exception ex)
        {
            activity?.SetTag("cache.error", ex.Message);
            _logger.LogError(ex, "Error invalidating cache for key: {Key}", key);
        }
    }

    /// <summary>
    /// Batch cache operations for performance
    /// </summary>
    public async Task<IDictionary<string, T?>> GetBatchAsync<T>(IEnumerable<string> keys, Func<string, Task<T>> factory)
    {
        using var activity = _activitySource.StartActivity("Cache.GetBatch");
        var keyList = keys.ToList();
        var results = new Dictionary<string, T?>();

        activity?.SetTag("cache.keys_count", keyList.Count);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Try Redis batch if available
            if (_redisConnection?.IsConnected == true && _settings.UseRedisCache)
            {
                var redisTasks = keyList.Select(key =>
                    _redisConnection.GetDatabase().StringGetAsync(key));
                var redisValues = await Task.WhenAll(redisTasks);

                for (int i = 0; i < keyList.Count; i++)
                {
                    var key = keyList[i];
                    var redisValue = redisValues[i];

                    if (redisValue.HasValue)
                    {
                        results[key] = JsonSerializer.Deserialize<T>(redisValue!);
                    }
                }
            }

            // Check memory cache for missing items
            var missingKeys = keyList.Where(k => !results.ContainsKey(k)).ToList();
            foreach (var key in missingKeys)
            {
                if (_memoryCache.TryGetValue(key, out T? memoryValue))
                {
                    results[key] = memoryValue;
                }
            }

            // Fetch remaining items from factory
            var fetchKeys = keyList.Where(k => !results.ContainsKey(k)).ToList();
            var fetchTasks = fetchKeys.ToDictionary(
                key => key,
                key => factory(key));

            if (fetchTasks.Any())
            {
                var fetchResults = await Task.WhenAll(fetchTasks.Values);
                for (int i = 0; i < fetchKeys.Count; i++)
                {
                    var key = fetchKeys[i];
                    var value = fetchResults[i];
                    results[key] = value;

                    // Cache the fetched value
                    if (value != null)
                    {
                        await SetAsync(key, value);
                    }
                }
            }

            stopwatch.Stop();
            activity?.SetTag("cache.duration_ms", stopwatch.ElapsedMilliseconds);
            activity?.SetTag("cache.hit_count", results.Count);
            activity?.SetTag("cache.miss_count", fetchKeys.Count);

            _logger.LogDebug("Batch cache operation completed: {TotalKeys} keys, {Hits} hits, {Misses} misses in {ElapsedMs}ms",
                keyList.Count, results.Count, fetchKeys.Count, stopwatch.ElapsedMilliseconds);

            return results;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetTag("cache.error", ex.Message);
            activity?.SetTag("cache.duration_ms", stopwatch.ElapsedMilliseconds);
            _logger.LogError(ex, "Error in batch cache operation");

            // Fallback to individual factory calls
            var fallbackResults = new Dictionary<string, T?>();
            foreach (var key in keyList)
            {
                try
                {
                    fallbackResults[key] = await factory(key);
                }
                catch (Exception factoryEx)
                {
                    _logger.LogError(factoryEx, "Error fetching data for key: {Key}", key);
                    fallbackResults[key] = default;
                }
            }
            return fallbackResults;
        }
    }

    /// <summary>
    /// Get cache statistics for monitoring
    /// </summary>
    public async Task<CacheStatistics> GetStatisticsAsync()
    {
        using var activity = _activitySource.StartActivity("Cache.Statistics");

        try
        {
            var stats = new CacheStatistics
            {
                IsRedisConnected = _redisConnection?.IsConnected == true && _settings.UseRedisCache
            };

            // Redis statistics
            if (_redisConnection?.IsConnected == true)
            {
                var database = _redisConnection.GetDatabase();

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
                var endpoints = _redisConnection.GetEndPoints();
                foreach (var endpoint in endpoints)
                {
                    var server = _redisConnection.GetServer(endpoint);
                    var info = await server.InfoAsync("Stats");
                    stats.RedisInfo = info.ToString() ?? string.Empty;
                    break; // Only need one endpoint's stats
                }
            }

            // Memory cache statistics (approximate)
            stats.InMemoryCacheSize = GetMemoryCacheEntryCount();

            _logger.LogDebug("Cache statistics retrieved: {@Stats}", stats);
            return stats;
        }
        catch (Exception ex)
        {
            activity?.SetTag("cache.error", ex.Message);
            _logger.LogError(ex, "Error getting cache statistics");
            return new CacheStatistics();
        }
    }

    /// <summary>
    /// Warm up cache with frequently accessed data
    /// </summary>
    public async Task WarmUpCacheAsync<T>(IEnumerable<string> keys, Func<string, Task<T>> factory)
    {
        using var activity = _activitySource.StartActivity("Cache.WarmUp");
        var keyList = keys.ToList();

        activity?.SetTag("cache.keys_count", keyList.Count);

        _logger.LogInformation("Starting cache warm-up for {Count} keys", keyList.Count);

        var stopwatch = Stopwatch.StartNew();
        var successCount = 0;
        var errorCount = 0;

        // Process in parallel batches
        const int batchSize = 10;
        var batches = keyList.Chunk(batchSize);

        foreach (var batch in batches)
        {
            var tasks = batch.Select(async key =>
            {
                try
                {
                    if (!_memoryCache.TryGetValue(key, out _) &&
                        (_redisConnection?.IsConnected != true || !await _redisConnection.GetDatabase().KeyExistsAsync(key)))
                    {
                        var value = await factory(key);
                        if (value != null)
                        {
                            await SetAsync(key, value);
                            Interlocked.Increment(ref successCount);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref errorCount);
                    _logger.LogError(ex, "Error warming up cache for key: {Key}", key);
                }
            });

            await Task.WhenAll(tasks);
        }

        stopwatch.Stop();
        activity?.SetTag("cache.duration_ms", stopwatch.ElapsedMilliseconds);
        activity?.SetTag("cache.success_count", successCount);
        activity?.SetTag("cache.error_count", errorCount);

        _logger.LogInformation("Cache warm-up completed: {Success} succeeded, {Errors} failed in {ElapsedMs}ms",
            successCount, errorCount, stopwatch.ElapsedMilliseconds);
    }

    private int GetMemoryCacheEntryCount()
    {
        // Use reflection to get memory cache entry count (internal API)
        try
        {
            var cacheEntriesProperty = typeof(MemoryCache).GetProperty("EntriesCollection",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (cacheEntriesProperty?.GetValue(_memoryCache) is System.Collections.ICollection entries)
            {
                return entries.Count;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not get memory cache entry count");
        }
        return 0;
    }
}

public class PerformanceOptimizationSettings
{
    public bool UseRedisCache { get; set; } = true;
    public TimeSpan DefaultCacheExpiration { get; set; } = TimeSpan.FromMinutes(30);
    public int MaxCacheSize { get; set; } = 1000;
    public bool EnableCompression { get; set; } = true;
    public TimeSpan CacheWarmUpInterval { get; set; } = TimeSpan.FromHours(1);
}


public static class PerformanceOptimizationExtensions
{
    public static async Task<T?> GetCachedAsync<T>(
        this PerformanceOptimizationService cache,
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiration = null)
    {
        return await cache.GetAsync(key, factory, expiration);
    }

    public static async Task<IDictionary<string, T?>> GetCachedBatchAsync<T>(
        this PerformanceOptimizationService cache,
        IEnumerable<string> keys,
        Func<string, Task<T>> factory)
    {
        return await cache.GetBatchAsync<T>(keys, factory);
    }
}
