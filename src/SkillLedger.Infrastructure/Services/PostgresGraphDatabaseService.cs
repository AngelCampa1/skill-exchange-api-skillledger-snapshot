using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;
using System.Text.Json;

namespace SkillLedger.Infrastructure.Services;

/// <summary>
/// PostgreSQL implementation for graph database operations using existing tables
/// </summary>
public class PostgresGraphDatabaseService : IGraphDatabaseService
{
    private readonly SkillLedgerDbContext _context;
    private readonly ILogger<PostgresGraphDatabaseService> _logger;

    public PostgresGraphDatabaseService(
        SkillLedgerDbContext context,
        ILogger<PostgresGraphDatabaseService> logger)
    {
        _context = context;
        _logger = logger;
        _logger.LogInformation("PostgreSQL Graph Database Service initialized");
    }

    /// <summary>
    /// Creates or updates a user node in the graph database.
    /// Since users already exist in the Users table, this is a no-op that returns success.
    /// </summary>
    public async Task<bool> CreateOrUpdateUserNodeAsync(Guid userId, Dictionary<string, object> properties)
    {
        try
        {
            // Users are already stored in the Users table - verify the user exists
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                _logger.LogWarning("User {UserId} not found in database", userId);
                return false;
            }

            _logger.LogDebug("User node verified for user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify user node for {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Creates a connection between two users based on shared activities
    /// </summary>
    public async Task<bool> CreateUserConnectionAsync(
        Guid fromUserId,
        Guid toUserId,
        string connectionType,
        decimal strength,
        Dictionary<string, object>? metadata = null)
    {
        try
        {
            // Normalize user order to avoid duplicate connections (User1Id < User2Id)
            var (user1Id, user2Id) = fromUserId.CompareTo(toUserId) < 0
                ? (fromUserId, toUserId)
                : (toUserId, fromUserId);

            var existingConnection = await _context.UserNetworkConnections
                .FirstOrDefaultAsync(c => c.User1Id == user1Id && c.User2Id == user2Id);

            if (existingConnection != null)
            {
                // Update existing connection
                existingConnection.ConnectionType = connectionType;
                existingConnection.ConnectionStrength = strength;
                existingConnection.Metadata = metadata != null ? JsonSerializer.Serialize(metadata) : null;
                existingConnection.LastInteractionAt = DateTime.UtcNow;
                existingConnection.InteractionCount++;
            }
            else
            {
                // Create new connection
                var connection = new UserNetworkConnection
                {
                    User1Id = user1Id,
                    User2Id = user2Id,
                    ConnectionType = connectionType,
                    ConnectionStrength = strength,
                    Metadata = metadata != null ? JsonSerializer.Serialize(metadata) : null,
                    DetectedAt = DateTime.UtcNow,
                    LastInteractionAt = DateTime.UtcNow,
                    InteractionCount = 1
                };
                _context.UserNetworkConnections.Add(connection);
            }

            await _context.SaveChangesAsync();

            _logger.LogDebug("Connection created between {FromUserId} and {ToUserId} of type {Type}",
                fromUserId, toUserId, connectionType);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create connection between {FromUserId} and {ToUserId}",
                fromUserId, toUserId);
            return false;
        }
    }

    /// <summary>
    /// Analyzes the user's network to detect suspicious patterns
    /// </summary>
    public async Task<NetworkAnalysisResult> AnalyzeUserNetworkAsync(Guid userId)
    {
        try
        {
            // Get all connections for the user
            var connections = await _context.UserNetworkConnections
                .Where(c => c.User1Id == userId || c.User2Id == userId)
                .ToListAsync();

            var totalConnections = connections.Count;
            var avgStrength = connections.Any()
                ? (double)connections.Average(c => c.ConnectionStrength)
                : 0;
            var suspiciousConnections = connections.Count(c => c.ConnectionStrength > 0.8m);

            // Calculate clustering coefficient
            var clusteringCoeff = await CalculateClusteringCoefficientAsync(userId, connections);

            // Detect suspicious patterns
            var suspiciousPatterns = new List<string>();

            if (avgStrength > 0.8)
                suspiciousPatterns.Add("Unusually high average connection strength");

            if (totalConnections > 50)
                suspiciousPatterns.Add("Excessive number of connections");

            if (clusteringCoeff > 0.8)
                suspiciousPatterns.Add("Highly clustered network (possible coordination)");

            // Determine risk level
            var riskLevel = NetworkRiskLevel.Low;
            if (suspiciousPatterns.Count >= 3 || suspiciousConnections > 20)
                riskLevel = NetworkRiskLevel.Critical;
            else if (suspiciousPatterns.Count >= 2 || suspiciousConnections > 10)
                riskLevel = NetworkRiskLevel.High;
            else if (suspiciousPatterns.Count >= 1 || suspiciousConnections > 5)
                riskLevel = NetworkRiskLevel.Medium;

            return new NetworkAnalysisResult
            {
                UserId = userId,
                TotalConnections = totalConnections,
                AverageConnectionStrength = avgStrength,
                SuspiciousConnections = suspiciousConnections,
                ClusteringCoefficient = clusteringCoeff,
                SuspiciousPatterns = suspiciousPatterns,
                RiskLevel = riskLevel,
                AnalyzedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze network for user {UserId}", userId);
            return new NetworkAnalysisResult
            {
                UserId = userId,
                RiskLevel = NetworkRiskLevel.Medium,
                AnalyzedAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Finds users with similar behavior patterns (potential coordinated activity)
    /// Uses simplified connection-based grouping instead of Louvain algorithm
    /// </summary>
    public async Task<List<SuspiciousCluster>> FindSuspiciousClustersAsync(
        int minClusterSize = 3,
        double minConnectionStrength = 0.7)
    {
        try
        {
            // Find clusters of highly connected users
            var highStrengthConnections = await _context.UserNetworkConnections
                .Where(c => c.ConnectionStrength >= (decimal)minConnectionStrength)
                .ToListAsync();

            // Build adjacency list
            var adjacencyList = new Dictionary<Guid, HashSet<Guid>>();
            foreach (var conn in highStrengthConnections)
            {
                if (!adjacencyList.ContainsKey(conn.User1Id))
                    adjacencyList[conn.User1Id] = new HashSet<Guid>();
                if (!adjacencyList.ContainsKey(conn.User2Id))
                    adjacencyList[conn.User2Id] = new HashSet<Guid>();

                adjacencyList[conn.User1Id].Add(conn.User2Id);
                adjacencyList[conn.User2Id].Add(conn.User1Id);
            }

            // Find connected components (clusters)
            var visited = new HashSet<Guid>();
            var clusters = new List<SuspiciousCluster>();
            var clusterIndex = 0;

            foreach (var userId in adjacencyList.Keys)
            {
                if (visited.Contains(userId))
                    continue;

                var cluster = new HashSet<Guid>();
                var queue = new Queue<Guid>();
                queue.Enqueue(userId);

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    if (visited.Contains(current))
                        continue;

                    visited.Add(current);
                    cluster.Add(current);

                    if (adjacencyList.TryGetValue(current, out var neighbors))
                    {
                        foreach (var neighbor in neighbors)
                        {
                            if (!visited.Contains(neighbor))
                                queue.Enqueue(neighbor);
                        }
                    }
                }

                if (cluster.Count >= minClusterSize)
                {
                    // Calculate cluster metrics
                    var clusterUserIds = cluster.ToList();
                    var internalConnections = highStrengthConnections
                        .Where(c => cluster.Contains(c.User1Id) && cluster.Contains(c.User2Id))
                        .ToList();

                    var avgInternalStrength = internalConnections.Any()
                        ? (double)internalConnections.Average(c => c.ConnectionStrength)
                        : 0;

                    // Count external connections
                    var externalConnectionCount = await _context.UserNetworkConnections
                        .Where(c => (cluster.Contains(c.User1Id) && !cluster.Contains(c.User2Id)) ||
                                   (cluster.Contains(c.User2Id) && !cluster.Contains(c.User1Id)))
                        .CountAsync();

                    var totalConnections = internalConnections.Count + externalConnectionCount;
                    var externalRatio = totalConnections > 0
                        ? (double)externalConnectionCount / totalConnections
                        : 0;

                    var suspiciousActivities = new List<string>();
                    if (avgInternalStrength > 0.8)
                        suspiciousActivities.Add("Very high internal connectivity");
                    if (externalRatio < 0.2)
                        suspiciousActivities.Add("Low external connectivity (isolation)");

                    clusters.Add(new SuspiciousCluster
                    {
                        ClusterId = $"cluster_{clusterIndex++}",
                        UserIds = clusterUserIds,
                        AverageInternalConnection = avgInternalStrength,
                        ExternalConnectionRatio = externalRatio,
                        SuspiciousActivities = suspiciousActivities,
                        DetectedAt = DateTime.UtcNow
                    });
                }
            }

            _logger.LogInformation("Found {Count} suspicious clusters", clusters.Count);
            return clusters;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to find suspicious clusters");
            return new List<SuspiciousCluster>();
        }
    }

    /// <summary>
    /// Gets the shortest path between two users using BFS
    /// </summary>
    public async Task<NetworkPath?> GetShortestPathAsync(Guid fromUserId, Guid toUserId, int maxDepth = 6)
    {
        try
        {
            // Load all connections into memory for BFS (efficient for small graphs)
            var allConnections = await _context.UserNetworkConnections.ToListAsync();

            // Build adjacency list with connection details
            var adjacencyList = new Dictionary<Guid, List<(Guid neighbor, UserNetworkConnection connection)>>();
            foreach (var conn in allConnections)
            {
                if (!adjacencyList.ContainsKey(conn.User1Id))
                    adjacencyList[conn.User1Id] = new List<(Guid, UserNetworkConnection)>();
                if (!adjacencyList.ContainsKey(conn.User2Id))
                    adjacencyList[conn.User2Id] = new List<(Guid, UserNetworkConnection)>();

                adjacencyList[conn.User1Id].Add((conn.User2Id, conn));
                adjacencyList[conn.User2Id].Add((conn.User1Id, conn));
            }

            // BFS to find shortest path
            var queue = new Queue<(Guid current, List<NetworkHop> path)>();
            var visited = new HashSet<Guid> { fromUserId };
            queue.Enqueue((fromUserId, new List<NetworkHop>()));

            while (queue.Count > 0)
            {
                var (current, path) = queue.Dequeue();

                if (current == toUserId)
                {
                    var totalStrength = path.Sum(h => (double)h.Strength);
                    return new NetworkPath
                    {
                        FromUserId = fromUserId,
                        ToUserId = toUserId,
                        Hops = path,
                        TotalStrength = path.Count > 0 ? totalStrength / path.Count : 0,
                        PathLength = path.Count
                    };
                }

                if (path.Count >= maxDepth)
                    continue;

                if (adjacencyList.TryGetValue(current, out var neighbors))
                {
                    foreach (var (neighbor, connection) in neighbors)
                    {
                        if (visited.Contains(neighbor))
                            continue;

                        visited.Add(neighbor);
                        var newPath = new List<NetworkHop>(path)
                        {
                            new NetworkHop
                            {
                                FromUserId = current,
                                ToUserId = neighbor,
                                ConnectionType = connection.ConnectionType,
                                Strength = connection.ConnectionStrength,
                                Metadata = string.IsNullOrEmpty(connection.Metadata)
                                    ? new Dictionary<string, object>()
                                    : JsonSerializer.Deserialize<Dictionary<string, object>>(connection.Metadata) ?? new()
                            }
                        };
                        queue.Enqueue((neighbor, newPath));
                    }
                }
            }

            return null; // No path found
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to find shortest path between {FromUserId} and {ToUserId}",
                fromUserId, toUserId);
            return null;
        }
    }

    /// <summary>
    /// Updates connection strengths based on new interactions
    /// </summary>
    public async Task<bool> UpdateConnectionStrengthAsync(
        Guid fromUserId,
        Guid toUserId,
        string interactionType,
        DateTime timestamp)
    {
        try
        {
            // Calculate strength increment based on interaction type
            var strengthIncrement = interactionType switch
            {
                "ReviewExchange" => 0.3m,
                "ProjectCollaboration" => 0.2m,
                "MessageExchange" => 0.1m,
                "ProfileView" => 0.05m,
                _ => 0.1m
            };

            // Normalize user order
            var (user1Id, user2Id) = fromUserId.CompareTo(toUserId) < 0
                ? (fromUserId, toUserId)
                : (toUserId, fromUserId);

            var connection = await _context.UserNetworkConnections
                .FirstOrDefaultAsync(c => c.User1Id == user1Id && c.User2Id == user2Id);

            if (connection != null)
            {
                connection.ConnectionStrength += strengthIncrement;
                connection.LastInteractionAt = timestamp;
                connection.InteractionCount++;
                await _context.SaveChangesAsync();

                _logger.LogDebug("Updated connection strength to {Strength} between {FromUserId} and {ToUserId}",
                    connection.ConnectionStrength, fromUserId, toUserId);
            }
            else
            {
                // Create new connection if it doesn't exist
                await CreateUserConnectionAsync(fromUserId, toUserId, interactionType, strengthIncrement);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update connection strength between {FromUserId} and {ToUserId}",
                fromUserId, toUserId);
            return false;
        }
    }

    /// <summary>
    /// Removes connections that fall below threshold or are too old
    /// </summary>
    public async Task<int> CleanupStaleConnectionsAsync(double minStrength = 0.1, TimeSpan maxAge = default)
    {
        try
        {
            if (maxAge == default)
                maxAge = TimeSpan.FromDays(365);

            var cutoffDate = DateTime.UtcNow - maxAge;
            var minStrengthDecimal = (decimal)minStrength;

            var staleConnections = await _context.UserNetworkConnections
                .Where(c => c.ConnectionStrength < minStrengthDecimal ||
                           (c.LastInteractionAt != null && c.LastInteractionAt < cutoffDate))
                .ToListAsync();

            _context.UserNetworkConnections.RemoveRange(staleConnections);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cleaned up {Count} stale connections", staleConnections.Count);
            return staleConnections.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup stale connections");
            return 0;
        }
    }

    /// <summary>
    /// Gets network statistics for a user
    /// </summary>
    public async Task<NetworkStatistics> GetNetworkStatisticsAsync(Guid userId)
    {
        try
        {
            // Get direct connections
            var directConnections = await _context.UserNetworkConnections
                .Where(c => c.User1Id == userId || c.User2Id == userId)
                .ToListAsync();

            var directNeighbors = directConnections
                .Select(c => c.User1Id == userId ? c.User2Id : c.User1Id)
                .ToHashSet();

            // Get second-degree connections (friends of friends)
            var secondDegreeConnections = await _context.UserNetworkConnections
                .Where(c => (directNeighbors.Contains(c.User1Id) || directNeighbors.Contains(c.User2Id)) &&
                           c.User1Id != userId && c.User2Id != userId)
                .ToListAsync();

            var secondDegreeNeighbors = secondDegreeConnections
                .SelectMany(c => new[] { c.User1Id, c.User2Id })
                .Where(id => id != userId && !directNeighbors.Contains(id))
                .Distinct()
                .Count();

            // Calculate connection types
            var connectionTypes = directConnections
                .GroupBy(c => c.ConnectionType)
                .ToDictionary(g => g.Key, g => g.Count());

            // Calculate average strength
            var avgStrength = directConnections.Any()
                ? (double)directConnections.Average(c => c.ConnectionStrength)
                : 0;

            // Calculate network density (connections / possible connections)
            var directCount = directConnections.Count;
            var density = directCount > 1
                ? (double)directCount / (directCount * (directCount - 1) / 2.0)
                : 0;

            return new NetworkStatistics
            {
                UserId = userId,
                DirectConnections = directCount,
                SecondDegreeConnections = secondDegreeNeighbors,
                AverageConnectionStrength = avgStrength,
                ConnectionTypes = connectionTypes,
                NetworkDensity = Math.Min(density, 1.0), // Cap at 1.0
                CommunityCount = 1, // Simplified - user's own community
                CentralityScore = directCount // Degree centrality
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get network statistics for user {UserId}", userId);
            return new NetworkStatistics { UserId = userId };
        }
    }

    /// <summary>
    /// Detects potential sock puppet accounts (multiple accounts from same entity)
    /// </summary>
    public async Task<List<SockPuppetCluster>> DetectSockPuppetAccountsAsync(double similarityThreshold = 0.8)
    {
        try
        {
            var clusters = new List<SockPuppetCluster>();

            // Find users with matching device fingerprints
            var fingerprintGroups = await _context.DeviceFingerprints
                .Where(df => df.UserId != null)
                .GroupBy(df => df.FingerprintHash)
                .Where(g => g.Count() > 1)
                .Select(g => new
                {
                    Fingerprint = g.Key,
                    UserIds = g.Select(df => df.UserId!.Value).Distinct().ToList()
                })
                .ToListAsync();

            // Find users with matching IP addresses
            var ipGroups = await _context.DeviceFingerprints
                .Where(df => df.UserId != null && !string.IsNullOrEmpty(df.IpAddress))
                .GroupBy(df => df.IpAddress)
                .Where(g => g.Count() > 1)
                .Select(g => new
                {
                    IpAddress = g.Key,
                    UserIds = g.Select(df => df.UserId!.Value).Distinct().ToList()
                })
                .ToListAsync();

            // Combine results and calculate similarity scores
            var suspectedPairs = new Dictionary<(Guid, Guid), (double score, List<string> characteristics)>();

            foreach (var group in fingerprintGroups)
            {
                for (int i = 0; i < group.UserIds.Count; i++)
                {
                    for (int j = i + 1; j < group.UserIds.Count; j++)
                    {
                        var pair = (group.UserIds[i], group.UserIds[j]);
                        if (pair.Item1.CompareTo(pair.Item2) > 0)
                            pair = (pair.Item2, pair.Item1);

                        if (!suspectedPairs.ContainsKey(pair))
                            suspectedPairs[pair] = (0, new List<string>());

                        var (score, chars) = suspectedPairs[pair];
                        suspectedPairs[pair] = (score + 0.4, chars);
                        chars.Add("Device Fingerprint");
                    }
                }
            }

            foreach (var group in ipGroups)
            {
                for (int i = 0; i < group.UserIds.Count; i++)
                {
                    for (int j = i + 1; j < group.UserIds.Count; j++)
                    {
                        var pair = (group.UserIds[i], group.UserIds[j]);
                        if (pair.Item1.CompareTo(pair.Item2) > 0)
                            pair = (pair.Item2, pair.Item1);

                        if (!suspectedPairs.ContainsKey(pair))
                            suspectedPairs[pair] = (0, new List<string>());

                        var (score, chars) = suspectedPairs[pair];
                        suspectedPairs[pair] = (score + 0.4, chars);
                        if (!chars.Contains("IP Address"))
                            chars.Add("IP Address");
                    }
                }
            }

            // Check creation time proximity
            var userCreationDates = await _context.Users
                .Where(u => suspectedPairs.Keys.SelectMany(p => new[] { p.Item1, p.Item2 }).Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.CreatedAt);

            foreach (var pair in suspectedPairs.Keys.ToList())
            {
                if (userCreationDates.TryGetValue(pair.Item1, out var date1) &&
                    userCreationDates.TryGetValue(pair.Item2, out var date2))
                {
                    if (Math.Abs((date1 - date2).TotalDays) < 7)
                    {
                        var (score, chars) = suspectedPairs[pair];
                        suspectedPairs[pair] = (score + 0.2, chars);
                        if (!chars.Contains("Creation Time"))
                            chars.Add("Creation Time");
                    }
                }
            }

            // Group pairs that meet threshold into clusters
            var highSimilarityPairs = suspectedPairs
                .Where(p => p.Value.score >= similarityThreshold)
                .ToList();

            if (highSimilarityPairs.Any())
            {
                // Use union-find to group connected pairs
                var parent = new Dictionary<Guid, Guid>();
                Guid Find(Guid x)
                {
                    if (!parent.ContainsKey(x)) parent[x] = x;
                    if (parent[x] != x) parent[x] = Find(parent[x]);
                    return parent[x];
                }
                void Union(Guid x, Guid y)
                {
                    parent[Find(x)] = Find(y);
                }

                foreach (var pair in highSimilarityPairs)
                {
                    Union(pair.Key.Item1, pair.Key.Item2);
                }

                // Group by root
                var groups = highSimilarityPairs
                    .SelectMany(p => new[] { p.Key.Item1, p.Key.Item2 })
                    .Distinct()
                    .GroupBy(Find)
                    .Where(g => g.Count() >= 2);

                foreach (var group in groups)
                {
                    var userIds = group.ToList();
                    var relevantPairs = highSimilarityPairs
                        .Where(p => userIds.Contains(p.Key.Item1) && userIds.Contains(p.Key.Item2))
                        .ToList();

                    var avgScore = relevantPairs.Average(p => p.Value.score);
                    var allChars = relevantPairs.SelectMany(p => p.Value.characteristics).Distinct().ToList();

                    clusters.Add(new SockPuppetCluster
                    {
                        ClusterId = Guid.NewGuid().ToString(),
                        SuspectedAccountIds = userIds,
                        SimilarityScore = avgScore,
                        SharedCharacteristics = allChars,
                        FirstDetected = DateTime.UtcNow,
                        RequiresHumanReview = avgScore > 0.9
                    });
                }
            }

            _logger.LogInformation("Detected {Count} potential sock puppet clusters", clusters.Count);
            return clusters;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect sock puppet accounts");
            return new List<SockPuppetCluster>();
        }
    }

    /// <summary>
    /// Calculates clustering coefficient for a user
    /// </summary>
    private async Task<double> CalculateClusteringCoefficientAsync(
        Guid userId,
        List<UserNetworkConnection> userConnections)
    {
        try
        {
            // Get neighbors
            var neighbors = userConnections
                .Select(c => c.User1Id == userId ? c.User2Id : c.User1Id)
                .ToHashSet();

            if (neighbors.Count < 2)
                return 0;

            // Count connections between neighbors
            var neighborConnections = await _context.UserNetworkConnections
                .Where(c => neighbors.Contains(c.User1Id) && neighbors.Contains(c.User2Id))
                .CountAsync();

            // Possible connections = n(n-1)/2
            var possibleConnections = neighbors.Count * (neighbors.Count - 1) / 2.0;

            return possibleConnections > 0 ? neighborConnections / possibleConnections : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate clustering coefficient for user {UserId}", userId);
            return 0;
        }
    }

    public void Dispose()
    {
        // DbContext is managed by DI container
        _logger.LogInformation("PostgreSQL Graph Database Service disposed");
    }
}
