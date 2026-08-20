using SkillLedger.Core.Entities;

namespace SkillLedger.Core.Interfaces;

/// <summary>
/// Interface for graph database operations for network analysis
/// </summary>
public interface IGraphDatabaseService : IDisposable
{
    /// <summary>
    /// Creates or updates a user node in the graph database
    /// </summary>
    Task<bool> CreateOrUpdateUserNodeAsync(Guid userId, Dictionary<string, object> properties);

    /// <summary>
    /// Creates a connection between two users based on shared activities
    /// </summary>
    Task<bool> CreateUserConnectionAsync(Guid fromUserId, Guid toUserId, string connectionType, decimal strength, Dictionary<string, object>? metadata = null);

    /// <summary>
    /// Analyzes the user's network to detect suspicious patterns
    /// </summary>
    Task<NetworkAnalysisResult> AnalyzeUserNetworkAsync(Guid userId);

    /// <summary>
    /// Finds users with similar behavior patterns (potential coordinated activity)
    /// </summary>
    Task<List<SuspiciousCluster>> FindSuspiciousClustersAsync(int minClusterSize = 3, double minConnectionStrength = 0.7);

    /// <summary>
    /// Gets the shortest path between two users to understand relationships
    /// </summary>
    Task<NetworkPath?> GetShortestPathAsync(Guid fromUserId, Guid toUserId, int maxDepth = 6);

    /// <summary>
    /// Updates connection strengths based on new interactions
    /// </summary>
    Task<bool> UpdateConnectionStrengthAsync(Guid fromUserId, Guid toUserId, string interactionType, DateTime timestamp);

    /// <summary>
    /// Removes connections that fall below threshold or are too old
    /// </summary>
    Task<int> CleanupStaleConnectionsAsync(double minStrength = 0.1, TimeSpan maxAge = default);

    /// <summary>
    /// Gets network statistics for a user
    /// </summary>
    Task<NetworkStatistics> GetNetworkStatisticsAsync(Guid userId);

    /// <summary>
    /// Detects potential sock puppet accounts (multiple accounts from same entity)
    /// </summary>
    Task<List<SockPuppetCluster>> DetectSockPuppetAccountsAsync(double similarityThreshold = 0.8);
}

// Data models for graph database operations
public class NetworkAnalysisResult
{
    public Guid UserId { get; set; }
    public int TotalConnections { get; set; }
    public double AverageConnectionStrength { get; set; }
    public int SuspiciousConnections { get; set; }
    public double ClusteringCoefficient { get; set; }
    public List<string> SuspiciousPatterns { get; set; } = new();
    public NetworkRiskLevel RiskLevel { get; set; }
    public DateTime AnalyzedAt { get; set; }
}

public class SuspiciousCluster
{
    public string ClusterId { get; set; } = string.Empty;
    public List<Guid> UserIds { get; set; } = new();
    public double AverageInternalConnection { get; set; }
    public double ExternalConnectionRatio { get; set; }
    public List<string> SuspiciousActivities { get; set; } = new();
    public DateTime DetectedAt { get; set; }
}

public class NetworkPath
{
    public Guid FromUserId { get; set; }
    public Guid ToUserId { get; set; }
    public List<NetworkHop> Hops { get; set; } = new();
    public double TotalStrength { get; set; }
    public int PathLength { get; set; }
}

public class NetworkHop
{
    public Guid FromUserId { get; set; }
    public Guid ToUserId { get; set; }
    public string ConnectionType { get; set; } = string.Empty;
    public decimal Strength { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class NetworkStatistics
{
    public Guid UserId { get; set; }
    public int DirectConnections { get; set; }
    public int SecondDegreeConnections { get; set; }
    public double AverageConnectionStrength { get; set; }
    public Dictionary<string, int> ConnectionTypes { get; set; } = new();
    public double NetworkDensity { get; set; }
    public int CommunityCount { get; set; }
    public double CentralityScore { get; set; }
}

public class SockPuppetCluster
{
    public string ClusterId { get; set; } = string.Empty;
    public List<Guid> SuspectedAccountIds { get; set; } = new();
    public double SimilarityScore { get; set; }
    public List<string> SharedCharacteristics { get; set; } = new();
    public DateTime FirstDetected { get; set; }
    public bool RequiresHumanReview { get; set; }
}

public enum NetworkRiskLevel
{
    Low,
    Medium,
    High,
    Critical
}