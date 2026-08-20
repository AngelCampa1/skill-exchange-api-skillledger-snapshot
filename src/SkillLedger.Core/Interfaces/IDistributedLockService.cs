namespace SkillLedger.Core.Interfaces;

/// <summary>
/// Service for distributed locking to prevent race conditions in concurrent operations
/// </summary>
public interface IDistributedLockService
{
    /// <summary>
    /// Acquires a distributed lock with automatic release
    /// </summary>
    /// <param name="resource">The resource name to lock (e.g., "transfer:user123")</param>
    /// <param name="expirationTime">Maximum time the lock can be held</param>
    /// <param name="waitTime">Maximum time to wait for acquiring the lock</param>
    /// <param name="retryTime">Time between retry attempts</param>
    /// <returns>Distributed lock instance that must be disposed to release the lock</returns>
    Task<IDistributedLock> AcquireLockAsync(
        string resource,
        TimeSpan expirationTime,
        TimeSpan? waitTime = null,
        TimeSpan? retryTime = null);

    /// <summary>
    /// Tries to acquire a lock without waiting
    /// </summary>
    /// <param name="resource">The resource name to lock</param>
    /// <param name="expirationTime">Maximum time the lock can be held</param>
    /// <returns>Distributed lock instance or null if lock could not be acquired</returns>
    Task<IDistributedLock?> TryAcquireLockAsync(string resource, TimeSpan expirationTime);

    /// <summary>
    /// Checks if a resource is currently locked
    /// </summary>
    /// <param name="resource">The resource name to check</param>
    /// <returns>True if the resource is locked, otherwise false</returns>
    Task<bool> IsLockedAsync(string resource);
}

/// <summary>
/// Represents a distributed lock that must be disposed to release
/// </summary>
public interface IDistributedLock : IAsyncDisposable
{
    /// <summary>
    /// The resource that is locked
    /// </summary>
    string Resource { get; }

    /// <summary>
    /// Whether the lock was successfully acquired
    /// </summary>
    bool IsAcquired { get; }

    /// <summary>
    /// When the lock was acquired
    /// </summary>
    DateTime? AcquiredAt { get; }

    /// <summary>
    /// When the lock will expire
    /// </summary>
    DateTime? ExpiresAt { get; }

    /// <summary>
    /// Extends the lock expiration time
    /// </summary>
    /// <param name="additionalTime">Additional time to extend the lock</param>
    /// <returns>True if extended successfully</returns>
    Task<bool> ExtendAsync(TimeSpan additionalTime);
}

