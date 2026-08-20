using System.Collections.Concurrent;

namespace SkillLedger.Tests.Infrastructure;

/// <summary>
/// Coordinates test execution to prevent resource conflicts
/// Manages database access, memory usage, and concurrent operations
/// </summary>
public static class TestExecutionCoordinator
{
    // More restrictive semaphores to prevent resource exhaustion
    private static readonly SemaphoreSlim _databaseSemaphore = new SemaphoreSlim(1, 1);
    private static readonly SemaphoreSlim _memorySemaphore = new SemaphoreSlim(1, 1);
    private static readonly ConcurrentDictionary<string, DateTime> _lastCleanupTimes = new();
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Acquire database access with controlled concurrency
    /// </summary>
    public static async Task<IDisposable> AcquireDatabaseAccessAsync(string testName, CancellationToken cancellationToken = default)
    {
        await _databaseSemaphore.WaitAsync(cancellationToken);

        // Perform cleanup if needed
        await MaybeCleanupAsync(testName);

        return new DatabaseAccessToken(_databaseSemaphore);
    }

    /// <summary>
    /// Acquire memory-intensive operation access
    /// </summary>
    public static async Task<IDisposable> AcquireMemoryIntensiveAccessAsync(CancellationToken cancellationToken = default)
    {
        await _memorySemaphore.WaitAsync(cancellationToken);
        TestMemoryManager.TryCollectMemory();

        return new MemoryAccessToken(_memorySemaphore);
    }

    /// <summary>
    /// Maybe perform cleanup if enough time has passed
    /// </summary>
    private static async Task MaybeCleanupAsync(string testName)
    {
        var now = DateTime.UtcNow;
        var lastCleanup = _lastCleanupTimes.GetValueOrDefault(testName, DateTime.MinValue);

        if (now - lastCleanup > CleanupInterval)
        {
            TestMemoryManager.TryCollectMemory();
            _lastCleanupTimes[testName] = now;
        }

        await Task.Delay(1); // Yield to allow other operations
    }

    private class DatabaseAccessToken : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        private bool _disposed = false;

        public DatabaseAccessToken(SemaphoreSlim semaphore)
        {
            _semaphore = semaphore;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _semaphore.Release();
                _disposed = true;
            }
        }
    }

    private class MemoryAccessToken : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        private bool _disposed = false;

        public MemoryAccessToken(SemaphoreSlim semaphore)
        {
            _semaphore = semaphore;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                TestMemoryManager.TryCollectMemory();
                _semaphore.Release();
                _disposed = true;
            }
        }
    }
}