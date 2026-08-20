using SkillLedger.Core.Interfaces;

namespace SkillLedger.Tests.Mocks;

/// <summary>
/// Mock distributed lock for testing
/// </summary>
public class MockDistributedLock : IDistributedLock
{
    private readonly Action<string> _releaseAction;
    private bool _disposed = false;

    public MockDistributedLock(string resource, Action<string> releaseAction, bool isAcquired = true)
    {
        Resource = resource;
        _releaseAction = releaseAction;
        IsAcquired = isAcquired;
        AcquiredAt = isAcquired ? DateTime.UtcNow : null;
        ExpiresAt = isAcquired ? AcquiredAt.Value.AddMinutes(5) : null; // Default 5 minute expiration
    }

    public string Resource { get; }
    public bool IsAcquired { get; private set; }
    public DateTime? AcquiredAt { get; }
    public DateTime? ExpiresAt { get; private set; }

    public async Task<bool> ExtendAsync(TimeSpan additionalTime)
    {
        await Task.Delay(1); // Simulate async operation

        if (_disposed || !IsAcquired)
            return false;

        ExpiresAt = ExpiresAt?.Add(additionalTime);
        return true;
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed && IsAcquired)
        {
            IsAcquired = false;
            _releaseAction(Resource);
            _disposed = true;
        }
        await Task.CompletedTask;
    }
}

/// <summary>
/// Mock distributed lock service for testing
/// </summary>
public class MockDistributedLockService : IDistributedLockService
{
    private readonly Dictionary<string, bool> _locks = new();
    private bool _shouldFail = false;

    /// <summary>
    /// Set whether lock acquisitions should fail (for testing lock failure scenarios)
    /// </summary>
    public void SetShouldFail(bool shouldFail)
    {
        _shouldFail = shouldFail;
    }

    public async Task<IDistributedLock> AcquireLockAsync(
        string resource,
        TimeSpan expirationTime,
        TimeSpan? waitTime = null,
        TimeSpan? retryTime = null)
    {
        await Task.Delay(1); // Simulate async operation

        lock (_locks)
        {
            // If forced to fail, return non-acquired lock
            if (_shouldFail)
            {
                return new MockDistributedLock(resource, _ => { }, isAcquired: false);
            }

            if (_locks.ContainsKey(resource))
            {
                // If we can't acquire the lock, return a non-acquired lock
                return new MockDistributedLock(resource, _ => { }, isAcquired: false);
            }

            _locks[resource] = true;

            // Auto-release after expiration
            _ = Task.Delay(expirationTime).ContinueWith(_ =>
            {
                lock (_locks)
                {
                    _locks.Remove(resource);
                }
            });

            return new MockDistributedLock(resource, r =>
            {
                lock (_locks)
                {
                    _locks.Remove(r);
                }
            });
        }
    }

    public async Task<IDistributedLock?> TryAcquireLockAsync(string resource, TimeSpan expirationTime)
    {
        await Task.Delay(1); // Simulate async operation

        lock (_locks)
        {
            if (_locks.ContainsKey(resource))
            {
                return null; // Lock already held
            }

            _locks[resource] = true;

            // Auto-release after expiration
            _ = Task.Delay(expirationTime).ContinueWith(_ =>
            {
                lock (_locks)
                {
                    _locks.Remove(resource);
                }
            });

            return new MockDistributedLock(resource, r =>
            {
                lock (_locks)
                {
                    _locks.Remove(r);
                }
            });
        }
    }

    public async Task<bool> IsLockedAsync(string resource)
    {
        await Task.Delay(1); // Simulate async operation

        lock (_locks)
        {
            return _locks.ContainsKey(resource);
        }
    }
}