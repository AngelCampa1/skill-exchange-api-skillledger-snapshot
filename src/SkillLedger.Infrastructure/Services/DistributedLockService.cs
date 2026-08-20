using Microsoft.Extensions.Logging;
using SkillLedger.Core.Interfaces;
using StackExchange.Redis;
using System.Diagnostics;

namespace SkillLedger.Infrastructure.Services;

/// <summary>
/// Distributed lock service using Redis for coordination
/// Falls back to in-memory locking if Redis is unavailable
/// </summary>
public class DistributedLockService : IDistributedLockService
{
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<DistributedLockService> _logger;
    private static readonly SemaphoreSlim _localLockSemaphore = new(1, 1);
    private static readonly Dictionary<string, LocalLock> _localLocks = new();

    private const int DEFAULT_WAIT_MS = 5000;
    private const int DEFAULT_RETRY_MS = 100;
    private const int SEMAPHORE_TIMEOUT_MS = 10000;  // BUG FIX CRIT-004: Timeout for local lock semaphore

    public DistributedLockService(
        IConnectionMultiplexer? redis,
        ILogger<DistributedLockService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<IDistributedLock> AcquireLockAsync(
        string resource,
        TimeSpan expirationTime,
        TimeSpan? waitTime = null,
        TimeSpan? retryTime = null)
    {
        if (string.IsNullOrWhiteSpace(resource))
            throw new ArgumentException("Resource name cannot be empty", nameof(resource));

        var maxWaitTime = waitTime ?? TimeSpan.FromMilliseconds(DEFAULT_WAIT_MS);
        var retryInterval = retryTime ?? TimeSpan.FromMilliseconds(DEFAULT_RETRY_MS);
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < maxWaitTime)
        {
            var distributedLock = await TryAcquireLockAsync(resource, expirationTime);
            if (distributedLock != null && distributedLock.IsAcquired)
            {
                // BUG-025 FIX: Add guard for performance-sensitive debug logging
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Lock acquired for resource: {Resource} after {Elapsed}ms",
                        resource, stopwatch.ElapsedMilliseconds);
                }
                return distributedLock;
            }

            await Task.Delay(retryInterval);
        }

        _logger.LogWarning("Failed to acquire lock for resource: {Resource} after {Elapsed}ms",
            resource, stopwatch.ElapsedMilliseconds);

        throw new TimeoutException($"Could not acquire lock for resource '{resource}' within {maxWaitTime.TotalSeconds} seconds");
    }

    public async Task<IDistributedLock?> TryAcquireLockAsync(string resource, TimeSpan expirationTime)
    {
        if (string.IsNullOrWhiteSpace(resource))
            throw new ArgumentException("Resource name cannot be empty", nameof(resource));

        // Try Redis first if available
        if (_redis?.IsConnected == true)
        {
            try
            {
                var db = _redis.GetDatabase();
                var lockKey = $"lock:{resource}";
                var lockValue = $"{Environment.MachineName}:{Guid.NewGuid()}";

                var acquired = await db.StringSetAsync(
                    lockKey,
                    lockValue,
                    expirationTime,
                    When.NotExists);

                if (acquired)
                {
                    // BUG-025 FIX: Add guard for performance-sensitive debug logging
                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug("Redis lock acquired for resource: {Resource}", resource);
                    }
                    return new RedisDistributedLock(db, lockKey, lockValue, expirationTime, _logger);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to acquire Redis lock for resource: {Resource}, falling back to local lock", resource);
            }
        }

        // Fallback to in-memory lock
        return await AcquireLocalLockAsync(resource, expirationTime);
    }

    public async Task<bool> IsLockedAsync(string resource)
    {
        if (string.IsNullOrWhiteSpace(resource))
            throw new ArgumentException("Resource name cannot be empty", nameof(resource));

        // Check Redis first
        if (_redis?.IsConnected == true)
        {
            try
            {
                var db = _redis.GetDatabase();
                var lockKey = $"lock:{resource}";
                return await db.KeyExistsAsync(lockKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check Redis lock status for resource: {Resource}", resource);
            }
        }

        // Check local locks - BUG FIX CRIT-004: Add timeout to prevent deadlocks
        if (!await _localLockSemaphore.WaitAsync(SEMAPHORE_TIMEOUT_MS))
        {
            _logger.LogWarning("Timeout waiting for local lock semaphore when checking resource: {Resource}", resource);
            throw new TimeoutException($"Timeout waiting for local lock semaphore when checking resource '{resource}'");
        }
        try
        {
            if (_localLocks.TryGetValue(resource, out var localLock))
            {
                if (localLock.ExpiresAt > DateTime.UtcNow)
                {
                    return true;
                }

                // Lock expired, remove it
                _localLocks.Remove(resource);
            }

            return false;
        }
        finally
        {
            _localLockSemaphore.Release();
        }
    }

    private async Task<IDistributedLock?> AcquireLocalLockAsync(string resource, TimeSpan expirationTime)
    {
        // BUG FIX CRIT-004: Add timeout to prevent deadlocks
        if (!await _localLockSemaphore.WaitAsync(SEMAPHORE_TIMEOUT_MS))
        {
            _logger.LogWarning("Timeout waiting for local lock semaphore when acquiring resource: {Resource}", resource);
            return null;  // Return null to allow retry logic in caller
        }
        try
        {
            // Check if lock exists and is not expired
            if (_localLocks.TryGetValue(resource, out var existingLock))
            {
                if (existingLock.ExpiresAt > DateTime.UtcNow)
                {
                    // BUG-025 FIX: Add guard for performance-sensitive debug logging
                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug("Resource {Resource} is already locked locally", resource);
                    }
                    return null;
                }

                // Lock expired, remove it
                _localLocks.Remove(resource);
            }

            // Acquire new lock
            var lockInstance = new LocalDistributedLock(resource, expirationTime, this, _logger);
            var localLock = new LocalLock
            {
                Resource = resource,
                AcquiredAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(expirationTime),
                LockId = lockInstance.LockId
            };

            _localLocks[resource] = localLock;

            // BUG-025 FIX: Add guard for performance-sensitive debug logging
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Local lock acquired for resource: {Resource}", resource);
            }

            return lockInstance;
        }
        finally
        {
            _localLockSemaphore.Release();
        }
    }

    internal static async Task ReleaseLocalLockAsync(string resource, string lockId)
    {
        // BUG FIX CRIT-004: Add timeout to prevent deadlocks
        // Note: We still try to release even on timeout to avoid lock leaks
        var acquired = await _localLockSemaphore.WaitAsync(SEMAPHORE_TIMEOUT_MS);
        if (!acquired)
        {
            // Log would require ILogger, but this is a static method
            // In worst case, let lock expire naturally
            return;
        }
        try
        {
            if (_localLocks.TryGetValue(resource, out var localLock))
            {
                if (localLock.LockId == lockId)
                {
                    _localLocks.Remove(resource);
                }
            }
        }
        finally
        {
            _localLockSemaphore.Release();
        }
    }

    private class LocalLock
    {
        public required string Resource { get; init; }
        public DateTime AcquiredAt { get; init; }
        public DateTime ExpiresAt { get; set; }
        public required string LockId { get; init; }
    }
}

/// <summary>
/// Redis-based distributed lock implementation
/// </summary>
internal class RedisDistributedLock : IDistributedLock
{
    private readonly IDatabase _db;
    private readonly string _lockKey;
    private readonly string _lockValue;
    private readonly TimeSpan _expirationTime;
    private readonly ILogger _logger;
    private bool _disposed;

    public string Resource { get; }
    public bool IsAcquired { get; private set; }
    public DateTime? AcquiredAt { get; }
    public DateTime? ExpiresAt { get; private set; }

    public RedisDistributedLock(
        IDatabase db,
        string lockKey,
        string lockValue,
        TimeSpan expirationTime,
        ILogger logger)
    {
        _db = db;
        _lockKey = lockKey;
        _lockValue = lockValue;
        _expirationTime = expirationTime;
        _logger = logger;

        Resource = lockKey.Replace("lock:", string.Empty);
        IsAcquired = true;
        AcquiredAt = DateTime.UtcNow;
        ExpiresAt = DateTime.UtcNow.Add(expirationTime);
    }

    public async Task<bool> ExtendAsync(TimeSpan additionalTime)
    {
        if (_disposed || !IsAcquired)
            return false;

        try
        {
            var currentValue = await _db.StringGetAsync(_lockKey);
            if (currentValue == _lockValue)
            {
                var newExpiration = _expirationTime.Add(additionalTime);
                await _db.KeyExpireAsync(_lockKey, newExpiration);
                ExpiresAt = DateTime.UtcNow.Add(newExpiration);

                // BUG-025 FIX: Add guard for performance-sensitive debug logging
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Extended lock for resource: {Resource}", Resource);
                }
                return true;
            }

            _logger.LogWarning("Lock for resource {Resource} was lost before extension", Resource);
            IsAcquired = false;
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extend lock for resource: {Resource}", Resource);
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (!IsAcquired)
            return;

        try
        {
            // Use Lua script to ensure we only delete our lock
            var script = @"
                if redis.call('get', KEYS[1]) == ARGV[1] then
                    return redis.call('del', KEYS[1])
                else
                    return 0
                end";

            var result = await _db.ScriptEvaluateAsync(
                script,
                new RedisKey[] { _lockKey },
                new RedisValue[] { _lockValue });

            if ((int)result == 1)
            {
                // BUG-025 FIX: Add guard for performance-sensitive debug logging
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Released Redis lock for resource: {Resource}", Resource);
                }
            }
            else
            {
                _logger.LogWarning("Lock for resource {Resource} was already released or expired", Resource);
            }

            IsAcquired = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to release Redis lock for resource: {Resource}", Resource);
        }
    }
}

/// <summary>
/// In-memory distributed lock implementation (fallback when Redis unavailable)
/// </summary>
internal class LocalDistributedLock : IDistributedLock
{
    private readonly DistributedLockService _service;
    private readonly ILogger _logger;
    private bool _disposed;

    public string Resource { get; }
    public bool IsAcquired { get; private set; }
    public DateTime? AcquiredAt { get; }
    public DateTime? ExpiresAt { get; private set; }
    public string LockId { get; }

    public LocalDistributedLock(
        string resource,
        TimeSpan expirationTime,
        DistributedLockService service,
        ILogger logger)
    {
        Resource = resource;
        IsAcquired = true;
        AcquiredAt = DateTime.UtcNow;
        ExpiresAt = DateTime.UtcNow.Add(expirationTime);
        LockId = Guid.NewGuid().ToString();
        _service = service;
        _logger = logger;
    }

    public Task<bool> ExtendAsync(TimeSpan additionalTime)
    {
        if (_disposed || !IsAcquired)
            return Task.FromResult(false);

        ExpiresAt = ExpiresAt?.Add(additionalTime);

        // BUG-025 FIX: Add guard for performance-sensitive debug logging
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Extended local lock for resource: {Resource}", Resource);
        }
        return Task.FromResult(true);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (!IsAcquired)
            return;

        try
        {
            await DistributedLockService.ReleaseLocalLockAsync(Resource, LockId);

            // BUG-025 FIX: Add guard for performance-sensitive debug logging
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Released local lock for resource: {Resource}", Resource);
            }
            IsAcquired = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to release local lock for resource: {Resource}", Resource);
        }
    }
}

