using SkillLedger.Core.Entities;
using SkillLedger.Core.Interfaces;

namespace SkillLedger.Tests.Mocks;

/// <summary>
/// Enhanced mock implementation of IGraphDatabaseService for testing
/// Provides realistic network analysis patterns for fraud detection tests
/// </summary>
public class MockGraphDatabaseService : IGraphDatabaseService
{
    private bool _disposed = false;
    private readonly Dictionary<Guid, Dictionary<string, object>> _userNodes = new();
    private readonly List<UserNetworkConnection> _connections = new();
    private readonly Dictionary<Guid, NetworkAnalysisResult> _networkAnalysisCache = new();

    public Task<bool> CreateOrUpdateUserNodeAsync(Guid userId, Dictionary<string, object> properties)
    {
        _userNodes[userId] = new Dictionary<string, object>(properties);
        return Task.FromResult(true);
    }

    public Task<bool> CreateUserConnectionAsync(Guid fromUserId, Guid toUserId, string connectionType, decimal strength, Dictionary<string, object>? metadata = null)
    {
        var connection = new UserNetworkConnection
        {
            User1Id = fromUserId,
            User2Id = toUserId,
            ConnectionType = connectionType,
            ConnectionStrength = strength,
            DetectedAt = DateTime.UtcNow
        };

        _connections.Add(connection);

        // Invalidate cache for affected users
        _networkAnalysisCache.Remove(fromUserId);
        _networkAnalysisCache.Remove(toUserId);

        return Task.FromResult(true);
    }

    public Task<NetworkAnalysisResult> AnalyzeUserNetworkAsync(Guid userId)
    {
        // Use cached result if available
        if (_networkAnalysisCache.TryGetValue(userId, out var cachedResult))
        {
            return Task.FromResult(cachedResult);
        }

        var userConnections = _connections.Where(c => c.User1Id == userId || c.User2Id == userId).ToList();
        var totalConnections = userConnections.Count;
        var suspiciousConnections = userConnections.Where(c => c.ConnectionStrength > 0.8m).Count();

        // Enhanced risk calculation for test scenarios
        var riskScore = CalculateNetworkRiskScore(userConnections);
        var riskLevel = DetermineRiskLevel(riskScore, totalConnections);

        var suspiciousPatterns = DetectSuspiciousNetworkPatterns(userConnections);

        // Calculate clustering coefficient (mock realistic values for testing)
        var clusteringCoefficient = CalculateMockClusteringCoefficient(userConnections);

        var result = new NetworkAnalysisResult
        {
            UserId = userId,
            TotalConnections = totalConnections,
            AverageConnectionStrength = totalConnections > 0 ? (double)userConnections.Average(c => c.ConnectionStrength) : 0.0,
            SuspiciousConnections = suspiciousConnections,
            SuspiciousPatterns = suspiciousPatterns,
            RiskLevel = riskLevel,
            ClusteringCoefficient = clusteringCoefficient,
            AnalyzedAt = DateTime.UtcNow
        };

        // Cache the result
        _networkAnalysisCache[userId] = result;

        return Task.FromResult(result);
    }

    private decimal CalculateNetworkRiskScore(List<UserNetworkConnection> connections)
    {
        if (!connections.Any()) return 0m;

        var totalRisk = 0m;

        foreach (var connection in connections)
        {
            var connectionRisk = connection.ConnectionStrength * 0.3m;

            // Extra risk for certain connection types
            if (connection.ConnectionType.Contains("VPN") ||
                connection.ConnectionType.Contains("Proxy") ||
                connection.ConnectionType.Contains("Suspicious"))
            {
                connectionRisk += 0.2m;
            }

            // Extra risk for high-strength connections
            if (connection.ConnectionStrength > 0.9m)
            {
                connectionRisk += 0.15m;
            }
            else if (connection.ConnectionStrength > 0.8m)
            {
                connectionRisk += 0.1m;
            }

            totalRisk += connectionRisk;
        }

        // Risk based on connection count (test scenarios often create many connections)
        var connectionCountRisk = connections.Count switch
        {
            > 15 => 0.4m,
            > 10 => 0.3m,
            > 5 => 0.2m,
            > 2 => 0.1m,
            _ => 0m
        };

        return Math.Min(1.0m, (totalRisk / connections.Count) + connectionCountRisk);
    }

    private NetworkRiskLevel DetermineRiskLevel(decimal riskScore, int connectionCount)
    {
        // More aggressive risk levels for testing
        if (riskScore >= 0.7m || connectionCount > 15)
            return NetworkRiskLevel.Critical;
        if (riskScore >= 0.5m || connectionCount > 10)
            return NetworkRiskLevel.High;
        if (riskScore >= 0.3m || connectionCount > 5)
            return NetworkRiskLevel.Medium;

        return NetworkRiskLevel.Low;
    }

    private List<string> DetectSuspiciousNetworkPatterns(List<UserNetworkConnection> connections)
    {
        var patterns = new List<string>();

        if (!connections.Any()) return patterns;

        var connectionTypes = connections.GroupBy(c => c.ConnectionType).ToList();
        var ipAddresses = connections.SelectMany(c => new[] {
            // Extract IPs from metadata or connection types that might contain IPs
            c.ConnectionType.Contains(".") ? c.ConnectionType.Split(' ').FirstOrDefault(s => s.Contains(".")) : ""
        }).Where(ip => !string.IsNullOrEmpty(ip)).ToList();

        // High connection count pattern
        if (connections.Count > 10)
        {
            patterns.Add("High connection count");
        }

        // Multiple suspicious connection types
        var suspiciousTypes = connectionTypes.Where(g =>
            g.Key.Contains("VPN") ||
            g.Key.Contains("Proxy") ||
            g.Key.Contains("Suspicious") ||
            g.Key.Contains("Shared")).ToList();

        if (suspiciousTypes.Count > 2)
        {
            patterns.Add("Multiple suspicious connection types");
        }

        // High average connection strength
        var avgStrength = connections.Average(c => c.ConnectionStrength);
        if (avgStrength > 0.8m)
        {
            patterns.Add("High average connection strength");
        }

        // VPN/proxy usage patterns
        var vpnConnections = connections.Count(c =>
            c.ConnectionType.Contains("VPN") ||
            c.ConnectionType.Contains("Proxy") ||
            c.ConnectionType.Contains("10.") ||
            c.ConnectionType.Contains("172.") ||
            c.ConnectionType.Contains("192.168."));

        if (vpnConnections > 2)
        {
            patterns.Add("Multiple VPN/Proxy connections");
        }
        else if (vpnConnections > 0)
        {
            patterns.Add("VPN/Proxy usage detected");
        }

        // Geographic diversity (suspicious if too many different geographic regions)
        var uniqueRegions = ipAddresses.Distinct().Count();
        if (uniqueRegions > 5)
        {
            patterns.Add("High geographic diversity (potential VPN rotation)");
        }

        // Shared device patterns
        var sharedDeviceConnections = connections.Count(c =>
            c.ConnectionType.Contains("SharedDevice") ||
            c.ConnectionType.Contains("Shared"));

        if (sharedDeviceConnections > 3)
        {
            patterns.Add("Multiple shared device connections");
        }

        // Time-based patterns (recent connections might indicate coordinated activity)
        var recentConnections = connections.Count(c =>
            (DateTime.UtcNow - c.DetectedAt).TotalDays <= 1);

        if (recentConnections > connections.Count * 0.7m)
        {
            patterns.Add("High concentration of recent connections");
        }

        return patterns.Distinct().ToList();
    }

    private double CalculateMockClusteringCoefficient(List<UserNetworkConnection> connections)
    {
        if (!connections.Any()) return 0.0;

        // Mock calculation that returns realistic values for testing
        var connectionCount = connections.Count;

        // Higher clustering for more connected users
        if (connectionCount > 10)
            return 0.8 + (connectionCount % 10) * 0.02;
        if (connectionCount > 5)
            return 0.6 + (connectionCount % 5) * 0.04;
        if (connectionCount > 2)
            return 0.4 + (connectionCount % 3) * 0.1;

        return connectionCount * 0.1;
    }

    public Task<List<SuspiciousCluster>> FindSuspiciousClustersAsync(int minClusterSize = 3, double minConnectionStrength = 0.7)
    {
        // Create mock clusters that match test expectations
        var clusters = new List<SuspiciousCluster>();

        // Create a mock cluster if there are sufficient connections
        var userGroups = _connections
            .GroupBy(c => c.ConnectionType)
            .Where(g => g.Count() >= minClusterSize)
            .ToList();

        foreach (var group in userGroups)
        {
            var userIds = group.SelectMany(c => new[] { c.User1Id, c.User2Id })
                .Distinct()
                .Take(minClusterSize)
                .ToList();

            if (userIds.Count >= minClusterSize)
            {
                clusters.Add(new SuspiciousCluster
                {
                    ClusterId = $"Cluster_{group.Key}_{Guid.NewGuid():N}",
                    UserIds = userIds,
                    AverageInternalConnection = group.Average(c => (double)c.ConnectionStrength),
                    ExternalConnectionRatio = 0.3, // Mock value
                    SuspiciousActivities = new List<string>
                    {
                        $"Shared {group.Key} connections",
                        "High internal connectivity",
                        "Potential coordinated activity"
                    },
                    DetectedAt = DateTime.UtcNow
                });
            }
        }

        // Always add at least one cluster for testing if we have enough data
        if (!clusters.Any() && _connections.Count >= minClusterSize * 2)
        {
            clusters.Add(new SuspiciousCluster
            {
                ClusterId = $"DefaultCluster_{Guid.NewGuid():N}",
                UserIds = _connections.Take(minClusterSize).Select(c => c.User1Id).Distinct().ToList(),
                AverageInternalConnection = 0.85,
                ExternalConnectionRatio = 0.25,
                SuspiciousActivities = new List<string> { "High connectivity pattern" },
                DetectedAt = DateTime.UtcNow
            });
        }

        return Task.FromResult(clusters);
    }

    public Task<NetworkPath?> GetShortestPathAsync(Guid fromUserId, Guid toUserId, int maxDepth = 6)
    {
        var directConnections = _connections.Where(c =>
            (c.User1Id == fromUserId && c.User2Id == toUserId) ||
            (c.User2Id == fromUserId && c.User1Id == toUserId)).ToList();

        if (directConnections.Any())
        {
            return Task.FromResult(new NetworkPath
            {
                FromUserId = fromUserId,
                ToUserId = toUserId,
                Hops = new List<NetworkHop>
                {
                    new NetworkHop
                    {
                        FromUserId = fromUserId,
                        ToUserId = toUserId,
                        ConnectionType = directConnections.First().ConnectionType,
                        Strength = directConnections.First().ConnectionStrength,
                        Metadata = new Dictionary<string, object>
                        {
                            { "DetectedAt", DateTime.UtcNow },
                            { "IsDirect", true }
                        }
                    }
                },
                TotalStrength = directConnections.Average(c => (double)c.ConnectionStrength),
                PathLength = 1
            });
        }

        // Look for indirect paths (mock implementation for testing)
        var intermediateConnections = _connections.Where(c =>
            (c.User1Id == fromUserId || c.User2Id == fromUserId) &&
            maxDepth > 1).ToList();

        foreach (var intermediate in intermediateConnections)
        {
            var intermediateId = intermediate.User1Id == fromUserId ? intermediate.User2Id : intermediate.User1Id;

            var finalConnections = _connections.Where(c =>
                (c.User1Id == intermediateId && c.User2Id == toUserId) ||
                (c.User2Id == intermediateId && c.User1Id == toUserId)).ToList();

            if (finalConnections.Any())
            {
                return Task.FromResult(new NetworkPath
                {
                    FromUserId = fromUserId,
                    ToUserId = toUserId,
                    Hops = new List<NetworkHop>
                    {
                        new NetworkHop
                        {
                            FromUserId = fromUserId,
                            ToUserId = intermediateId,
                            ConnectionType = intermediate.ConnectionType,
                            Strength = intermediate.ConnectionStrength,
                            Metadata = new Dictionary<string, object> { { "Hop", 1 } }
                        },
                        new NetworkHop
                        {
                            FromUserId = intermediateId,
                            ToUserId = toUserId,
                            ConnectionType = finalConnections.First().ConnectionType,
                            Strength = finalConnections.First().ConnectionStrength,
                            Metadata = new Dictionary<string, object> { { "Hop", 2 } }
                        }
                    },
                    TotalStrength = (double)(intermediate.ConnectionStrength + finalConnections.First().ConnectionStrength) / 2.0,
                    PathLength = 2
                });
            }
        }

        return Task.FromResult<NetworkPath?>(null);
    }

    public Task<bool> UpdateConnectionStrengthAsync(Guid fromUserId, Guid toUserId, string interactionType, DateTime timestamp)
    {
        var connection = _connections.FirstOrDefault(c =>
            (c.User1Id == fromUserId && c.User2Id == toUserId) ||
            (c.User2Id == fromUserId && c.User1Id == toUserId));

        if (connection != null)
        {
            // Increase strength based on interaction type
            var strengthIncrease = interactionType switch
            {
                "SharedDevice" => 0.15m,
                "SharedProject" => 0.1m,
                "SuspiciousNetwork" => 0.2m,
                "VPN" => 0.05m,
                _ => 0.05m
            };

            connection.ConnectionStrength = Math.Min(1.0m, connection.ConnectionStrength + strengthIncrease);

            // Update connection type if new interaction type is more suspicious
            if (IsMoreSuspiciousType(interactionType, connection.ConnectionType))
            {
                connection.ConnectionType = interactionType;
            }

            // Invalidate cache
            _networkAnalysisCache.Remove(fromUserId);
            _networkAnalysisCache.Remove(toUserId);

            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    private bool IsMoreSuspiciousType(string newType, string currentType)
    {
        var suspicionOrder = new Dictionary<string, int>
        {
            {"Normal", 1},
            {"SharedDevice", 2},
            {"SharedProject", 3},
            {"VPN", 4},
            {"Proxy", 5},
            {"SuspiciousNetwork", 6},
            {"HighRisk", 7}
        };

        var newSuspicion = suspicionOrder.GetValueOrDefault(newType, 1);
        var currentSuspicion = suspicionOrder.GetValueOrDefault(currentType, 1);

        return newSuspicion > currentSuspicion;
    }

    public Task<int> CleanupStaleConnectionsAsync(double minStrength = 0.1, TimeSpan maxAge = default)
    {
        var cutoffTime = maxAge == default ? DateTime.UtcNow.AddDays(-30) : DateTime.UtcNow.Subtract(maxAge);
        var staleConnections = _connections.Where(c =>
            c.ConnectionStrength < (decimal)minStrength ||
            c.DetectedAt < cutoffTime).ToList();

        var removedCount = staleConnections.Count;
        foreach (var connection in staleConnections)
        {
            _connections.Remove(connection);

            // Invalidate cache for affected users
            _networkAnalysisCache.Remove(connection.User1Id);
            _networkAnalysisCache.Remove(connection.User2Id);
        }

        return Task.FromResult(removedCount);
    }

    public Task<NetworkStatistics> GetNetworkStatisticsAsync(Guid userId)
    {
        var userConnections = _connections.Where(c => c.User1Id == userId || c.User2Id == userId).ToList();
        var connectionTypes = userConnections.GroupBy(c => c.ConnectionType)
            .ToDictionary(g => g.Key, g => g.Count());

        var totalConnections = userConnections.Count;
        var secondDegreeConnections = userConnections.Count(c =>
            c.ConnectionStrength > 0.6m &&
            c.DetectedAt > DateTime.UtcNow.AddHours(-24));

        var avgStrength = totalConnections > 0 ? (double)userConnections.Average(c => c.ConnectionStrength) : 0.0;

        // Enhanced network density calculation
        var networkDensity = totalConnections > 1 ? (double)totalConnections / (totalConnections - 1) : 0.0;

        // Enhanced centrality score based on multiple factors
        var centralityScore = CalculateCentralityScore(userId, userConnections);

        // Mock community count based on connection diversity
        var communityCount = connectionTypes.Count > 0 ? Math.Min(connectionTypes.Count, 5) : 1;

        var clusteringCoefficient = CalculateMockClusteringCoefficient(userConnections);

        return Task.FromResult(new NetworkStatistics
        {
            UserId = userId,
            DirectConnections = totalConnections,
            SecondDegreeConnections = secondDegreeConnections,
            AverageConnectionStrength = avgStrength,
            NetworkDensity = networkDensity,
            ConnectionTypes = connectionTypes,
            CommunityCount = communityCount,
            CentralityScore = centralityScore
        });
    }

    private double CalculateCentralityScore(Guid userId, List<UserNetworkConnection> connections)
    {
        if (!connections.Any()) return 0.0;

        var connectionCount = connections.Count;
        var avgStrength = connections.Average(c => (double)c.ConnectionStrength);
        var suspiciousConnections = connections.Count(c =>
            c.ConnectionType.Contains("VPN") ||
            c.ConnectionType.Contains("Suspicious") ||
            c.ConnectionType.Contains("Proxy"));

        // Enhanced centrality calculation
        var baseCentrality = (double)connectionCount / 50.0; // Normalize by max expected
        var strengthBonus = avgStrength * 0.3;
        var suspiciousPenalty = suspiciousConnections * 0.1;

        return Math.Min(1.0, Math.Max(0.0, baseCentrality + strengthBonus - suspiciousPenalty));
    }

    public Task<List<SockPuppetCluster>> DetectSockPuppetAccountsAsync(double similarityThreshold = 0.8)
    {
        var clusters = new List<SockPuppetCluster>();

        // Group users by similar connection patterns
        var userConnectionGroups = _connections
            .GroupBy(c => new { c.ConnectionType, Strength = c.ConnectionStrength })
            .Where(g => g.Count() >= 2)
            .ToList();

        foreach (var group in userConnectionGroups)
        {
            var userIds = group.SelectMany(c => new[] { c.User1Id, c.User2Id })
                .Distinct()
                .Take(5) // Limit cluster size for testing
                .ToList();

            if (userIds.Count >= 2)
            {
                var similarityScore = group.Key.Strength > 0.8m ?
                    0.9 + (group.Count() * 0.01) : // Higher similarity for strong connections
                    0.7 + (double)(group.Key.Strength * 0.2m); // Moderate similarity otherwise

                clusters.Add(new SockPuppetCluster
                {
                    ClusterId = $"SockPuppet_{group.Key.ConnectionType}_{Guid.NewGuid():N}",
                    SuspectedAccountIds = userIds,
                    SimilarityScore = similarityScore,
                    SharedCharacteristics = new List<string>
                    {
                        $"Shared {group.Key.ConnectionType} pattern",
                        $"Connection strength: {group.Key.Strength:F2}",
                        $"Connection count: {group.Count()}",
                        "High behavioral similarity",
                        "Coordinated activity pattern"
                    },
                    FirstDetected = group.Min(c => c.DetectedAt),
                    RequiresHumanReview = group.Key.Strength > 0.9m || group.Count() > 3
                });
            }
        }

        // Always add at least one cluster for testing if we have any connections
        if (!clusters.Any() && _connections.Any())
        {
            var sampleConnections = _connections.Take(2).ToList();
            var sampleUserIds = sampleConnections.SelectMany(c => new[] { c.User1Id, c.User2Id })
                .Distinct()
                .Take(3)
                .ToList();

            if (sampleUserIds.Count >= 2)
            {
                clusters.Add(new SockPuppetCluster
                {
                    ClusterId = $"DefaultSockPuppet_{Guid.NewGuid():N}",
                    SuspectedAccountIds = sampleUserIds,
                    SimilarityScore = 0.85,
                    SharedCharacteristics = new List<string> { "Similar connection patterns" },
                    FirstDetected = DateTime.UtcNow.AddDays(-1),
                    RequiresHumanReview = false
                });
            }
        }

        return Task.FromResult(clusters);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _userNodes.Clear();
            _connections.Clear();
            _networkAnalysisCache.Clear();
        }
    }
}