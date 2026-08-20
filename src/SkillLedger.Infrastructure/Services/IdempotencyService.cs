using Microsoft.Extensions.Caching.Distributed;
using SkillLedger.Core.Interfaces;

namespace SkillLedger.Infrastructure.Services;

/// <summary>
/// Service for preventing duplicate operations using distributed caching
/// CRITICAL for financial operations to prevent double payment releases
/// </summary>
public class IdempotencyService : IIdempotencyService
{
    private readonly IDistributedCache _cache;
    private readonly TimeSpan _lockDuration = TimeSpan.FromMinutes(5);

    public IdempotencyService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<bool> IsDuplicateOperationAsync(string operationKey)
    {
        var cacheKey = $"idempotency:{operationKey}";
        var cachedValue = await _cache.GetStringAsync(cacheKey);
        return !string.IsNullOrEmpty(cachedValue);
    }

    public async Task MarkOperationCompletedAsync(string operationKey)
    {
        var cacheKey = $"idempotency:{operationKey}";
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _lockDuration
        };
        await _cache.SetStringAsync(cacheKey, DateTime.UtcNow.ToString("o"), options);
    }
}
