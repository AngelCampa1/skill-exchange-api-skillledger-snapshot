using Microsoft.Extensions.Caching.Distributed;

namespace SkillLedger.Tests.Mocks;

/// <summary>
/// Mock distributed cache for testing purposes
/// </summary>
public class MockDistributedCache : IDistributedCache
{
    private readonly Dictionary<string, byte[]> _cache = new();
    private readonly Dictionary<string, DateTimeOffset> _expirations = new();

    public byte[]? Get(string key)
    {
        CleanupExpired();
        return _cache.TryGetValue(key, out var value) ? value : null;
    }

    public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        return Task.FromResult(Get(key));
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        _cache[key] = value;

        if (options.AbsoluteExpiration.HasValue)
        {
            _expirations[key] = options.AbsoluteExpiration.Value;
        }
        else if (options.AbsoluteExpirationRelativeToNow.HasValue)
        {
            _expirations[key] = DateTimeOffset.UtcNow.Add(options.AbsoluteExpirationRelativeToNow.Value);
        }
    }

    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        Set(key, value, options);
        return Task.CompletedTask;
    }

    public void Refresh(string key)
    {
        // No-op for in-memory cache
    }

    public Task RefreshAsync(string key, CancellationToken token = default)
    {
        return Task.CompletedTask;
    }

    public void Remove(string key)
    {
        _cache.Remove(key);
        _expirations.Remove(key);
    }

    public Task RemoveAsync(string key, CancellationToken token = default)
    {
        Remove(key);
        return Task.CompletedTask;
    }

    private void CleanupExpired()
    {
        var now = DateTimeOffset.UtcNow;
        var expiredKeys = _expirations
            .Where(x => x.Value < now)
            .Select(x => x.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _cache.Remove(key);
            _expirations.Remove(key);
        }
    }
}
