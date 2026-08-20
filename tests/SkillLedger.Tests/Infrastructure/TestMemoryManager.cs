using System.Runtime;

namespace SkillLedger.Tests.Infrastructure;

/// <summary>
/// Memory management utilities for test execution
/// Helps prevent memory pressure during large test runs
/// </summary>
public static class TestMemoryManager
{
    private static readonly object _gcLock = new object();
    private static long _lastGcTime = 0;
    private static readonly long GcIntervalTicks = TimeSpan.FromSeconds(30).Ticks;

    /// <summary>
    /// Force garbage collection if enough time has passed
    /// Prevents memory pressure during long test runs
    /// </summary>
    public static void TryCollectMemory()
    {
        var currentTime = DateTime.UtcNow.Ticks;

        lock (_gcLock)
        {
            if (currentTime - _lastGcTime > GcIntervalTicks)
            {
                try
                {
                    // More aggressive memory cleanup for tests
                    GC.Collect(2, GCCollectionMode.Forced, true, true);
                    GC.WaitForPendingFinalizers();
                    GC.Collect(2, GCCollectionMode.Forced, true, true);

                    // Compact the heap for maximum memory recovery
                    GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                    GC.Collect();

                    _lastGcTime = currentTime;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Memory collection warning: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Get current memory usage in MB
    /// </summary>
    public static long GetMemoryUsageMB()
    {
        return GC.GetTotalMemory(false) / 1024 / 1024;
    }

    /// <summary>
    /// Configure memory settings for test environment
    /// </summary>
    public static void ConfigureForTesting()
    {
        // Configure conservative GC settings for testing
        if (Environment.GetEnvironmentVariable("DOTNET_GCConserveMemory") == null)
        {
            Environment.SetEnvironmentVariable("DOTNET_GCConserveMemory", "5");
        }

        // Configure GC latency mode for throughput
        GCSettings.LatencyMode = GCLatencyMode.Batch;
    }

    /// <summary>
    /// Reset memory configuration after testing
    /// </summary>
    public static void ResetConfiguration()
    {
        GCSettings.LatencyMode = GCLatencyMode.Interactive;
    }
}