using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SkillLedger.Infrastructure.Data;
using System.Collections.Concurrent;

namespace SkillLedger.Tests.Infrastructure;

/// <summary>
/// Provides shared in-memory databases for integration tests to ensure proper isolation
/// and data sharing between test contexts and web application contexts
/// </summary>
public static class SharedDatabaseProvider
{
    private static readonly ConcurrentDictionary<string, InMemoryDatabaseRoot> _databaseRoots = new();

    /// <summary>
    /// Gets or creates a shared database root for the given database name
    /// This ensures all contexts with the same database name share the same data
    /// </summary>
    public static InMemoryDatabaseRoot GetOrCreateDatabaseRoot(string databaseName)
    {
        return _databaseRoots.GetOrAdd(databaseName, _ => new InMemoryDatabaseRoot());
    }

    /// <summary>
    /// Creates DbContextOptions that use a shared database root
    /// </summary>
    public static DbContextOptions<SkillLedgerDbContext> CreateSharedDbContextOptions(string databaseName)
    {
        var databaseRoot = GetOrCreateDatabaseRoot(databaseName);

        var builder = new DbContextOptionsBuilder<SkillLedgerDbContext>();
        builder.UseInMemoryDatabase(databaseName, databaseRoot);

        // Disable sensitive data logging to prevent file system watchers
        builder.EnableSensitiveDataLogging(false);
        // Disable service provider caching to prevent memory issues
        builder.EnableServiceProviderCaching(false);

        return builder.Options;
    }

    /// <summary>
    /// Clears all shared databases (useful for cleanup)
    /// </summary>
    public static void ClearAllDatabases()
    {
        _databaseRoots.Clear();
    }

    /// <summary>
    /// Removes a specific database from the shared cache
    /// </summary>
    public static bool RemoveDatabase(string databaseName)
    {
        return _databaseRoots.TryRemove(databaseName, out _);
    }
}