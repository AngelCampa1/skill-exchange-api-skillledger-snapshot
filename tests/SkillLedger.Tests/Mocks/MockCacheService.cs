using SkillLedger.Core.Models;
using SkillLedger.Core.Interfaces;
using System.Collections.Concurrent;

namespace SkillLedger.Tests.Mocks;

public class MockCacheService : ICacheService
{
    private readonly ConcurrentDictionary<string, object> _cache = new();
    private readonly ConcurrentDictionary<string, DateTime> _expiration = new();

    public Task<T?> GetAsync<T>(string key) where T : class
    {
        if (_cache.TryGetValue(key, out var value))
        {
            if (_expiration.TryGetValue(key, out var expiry) && expiry < DateTime.UtcNow)
            {
                _cache.TryRemove(key, out _);
                _expiration.TryRemove(key, out _);
                return Task.FromResult<T?>(null);
            }

            return Task.FromResult((T?)value);
        }

        return Task.FromResult<T?>(null);
    }

    public Task<bool> SetAsync<T>(string key, T value, TimeSpan expiration) where T : class
    {
        _cache[key] = value;
        _expiration[key] = DateTime.UtcNow.Add(expiration);
        return Task.FromResult(true);
    }

    public Task<bool> RemoveAsync(string key)
    {
        var removed = _cache.TryRemove(key, out _);
        _expiration.TryRemove(key, out _);
        return Task.FromResult(removed);
    }

    public Task<int> RemoveByPatternAsync(string pattern)
    {
        var keysToRemove = _cache.Keys.Where(k => MatchesPattern(k, pattern)).ToList();
        foreach (var key in keysToRemove)
        {
            _cache.TryRemove(key, out _);
            _expiration.TryRemove(key, out _);
        }
        return Task.FromResult(keysToRemove.Count);
    }

    public Task<bool> IsRedisAvailableAsync()
    {
        return Task.FromResult(false); // Mock always uses in-memory
    }

    public Task<CacheStatistics> GetStatisticsAsync()
    {
        return Task.FromResult(new CacheStatistics
        {
            IsRedisConnected = false,
            InMemoryCacheSize = _cache.Count
        });
    }

    private static bool MatchesPattern(string key, string pattern)
    {
        // Simple wildcard pattern matching for testing
        if (pattern.EndsWith("*"))
        {
            var prefix = pattern.TrimEnd('*');
            return key.StartsWith(prefix);
        }
        return key == pattern;
    }
}
