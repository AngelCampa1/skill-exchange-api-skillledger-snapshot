using SkillLedger.Core.Entities;

namespace SkillLedger.Core.Interfaces;

/// <summary>
/// Service for detecting and preventing review gaming and fraud
/// </summary>
public interface IAntiGamingService
{
    /// <summary>
    /// Analyze user behavior for gaming patterns
    /// </summary>
    /// <param name="userId">User to analyze</param>
    /// <returns>Risk assessment with detected patterns</returns>
    Task<GamingRiskAssessment> AnalyzeUserBehaviorAsync(Guid userId);

    /// <summary>
    /// Validate review authenticity
    /// </summary>
    /// <param name="review">Review to validate</param>
    /// <returns>True if review appears authentic</returns>
    Task<bool> ValidateReviewAuthenticityAsync(ProjectReview review);

    /// <summary>
    /// Detect network connections between users
    /// </summary>
    /// <param name="userId">User to analyze connections for</param>
    /// <returns>List of suspicious connections</returns>
    Task<List<UserNetworkConnection>> DetectSuspiciousConnectionsAsync(Guid userId);

    /// <summary>
    /// Create an anti-gaming alert
    /// </summary>
    /// <param name="userId">User who triggered the alert</param>
    /// <param name="alertType">Type of gaming detected</param>
    /// <param name="severity">Alert severity</param>
    /// <param name="description">Description of the issue</param>
    /// <param name="evidence">Supporting evidence</param>
    /// <returns>Created alert</returns>
    Task<AntiGamingAlert> CreateAlertAsync(Guid userId, string alertType, AlertSeverity severity,
        string description, Dictionary<string, object>? evidence = null);

    /// <summary>
    /// Apply automated sanctions based on risk level
    /// </summary>
    /// <param name="userId">User to sanction</param>
    /// <param name="riskAssessment">Risk assessment data</param>
    /// <returns>Applied sanction or null if no action needed</returns>
    Task<UserSanction?> ApplyAutomatedSanctionAsync(Guid userId, GamingRiskAssessment riskAssessment);

    /// <summary>
    /// Calculate user behavior metrics
    /// </summary>
    /// <param name="userId">User to calculate metrics for</param>
    /// <param name="metricNames">Specific metrics to calculate</param>
    /// <returns>List of calculated metrics</returns>
    Task<List<UserBehaviorMetric>> CalculateBehaviorMetricsAsync(Guid userId, string[]? metricNames = null);

    /// <summary>
    /// Monitor real-time review submission for gaming patterns
    /// </summary>
    /// <param name="review">Review being submitted</param>
    /// <returns>True if review should be allowed</returns>
    Task<bool> MonitorReviewSubmissionAsync(ProjectReview review);

    /// <summary>
    /// Get gaming risk score for a user
    /// </summary>
    /// <param name="userId">User to assess</param>
    /// <returns>Risk score (0-1, where 1 is highest risk)</returns>
    Task<decimal> GetUserRiskScoreAsync(Guid userId);

    /// <summary>
    /// Report suspicious gaming activity
    /// </summary>
    /// <param name="reportingUserId">User making the report</param>
    /// <param name="suspectedUserId">User being reported</param>
    /// <param name="reason">Reason for the report</param>
    /// <param name="evidence">Any evidence provided</param>
    /// <returns>True if report was recorded successfully</returns>
    Task<bool> ReportGamingActivityAsync(Guid reportingUserId, Guid suspectedUserId,
        string reason, Dictionary<string, object>? evidence = null);
}

/// <summary>
/// Risk factors that can be detected in user behavior
/// </summary>
public enum RiskFactor
{
    HighReviewVelocity,
    SimilarReviewContent,
    CoordinatedTiming,
    SharedDeviceFingerprints,
    SuspiciousNetworkConnections,
    AnomalousBehaviorPattern,
    VpnUsage,
    HighRiskGeolocation,
    MultipleAccountsFromSameDevice,
    ReviewBombing,
    SockPuppetNetwork,
    FakeReviewContent,
    UnnaturalReviewDistribution,
    BehaviorAnomalies
}

/// <summary>
/// Gaming patterns that can be detected
/// </summary>
public enum GamingPattern
{
    ReviewFarm,
    SockPuppetNetwork,
    CoordinatedAttack,
    ReviewBombing,
    InflationAttack,
    VelocityAttack,
    ContentDuplication,
    TimingManipulation,
    NetworkClustering,
    BehaviorMimicking
}

/// <summary>
/// Configuration for gaming detection thresholds
/// </summary>
public class GamingDetectionConfig
{
    public decimal HighRiskThreshold { get; set; } = 0.8m;
    public decimal MediumRiskThreshold { get; set; } = 0.6m;
    public decimal AutoSanctionThreshold { get; set; } = 0.95m;
    public int MaxReviewsPerDay { get; set; } = 10;
    public int MaxReviewsPerHour { get; set; } = 3;
    public decimal ContentSimilarityThreshold { get; set; } = 0.5m;
    public int NetworkConnectionMinSize { get; set; } = 3;
    public TimeSpan CoordinatedTimingWindow { get; set; } = TimeSpan.FromMinutes(30);
}

/// <summary>
/// Result of real-time monitoring
/// </summary>
public class MonitoringResult
{
    public bool AllowAction { get; set; }
    public decimal RiskScore { get; set; }
    public List<RiskFactor> DetectedRiskFactors { get; set; } = new();
    public string? BlockReason { get; set; }
    public bool RequiresHumanReview { get; set; }
}