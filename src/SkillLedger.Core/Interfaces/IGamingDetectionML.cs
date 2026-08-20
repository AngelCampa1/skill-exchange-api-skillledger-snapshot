using SkillLedger.Core.Entities;

namespace SkillLedger.Core.Interfaces;

/// <summary>
/// Interface for machine learning-based gaming detection
/// </summary>
public interface IGamingDetectionML : IDisposable
{
    /// <summary>
    /// Analyzes user behavior patterns for gaming detection
    /// </summary>
    Task<float> AnalyzeBehaviorPatternAsync(UserBehaviorData data);

    /// <summary>
    /// Analyzes content for similarity and gaming patterns
    /// </summary>
    Task<float> AnalyzeContentSimilarityAsync(ContentAnalysisData data);

    /// <summary>
    /// Analyzes user network connections for coordinated attacks
    /// </summary>
    Task<NetworkRisk> AnalyzeUserNetworkAsync(Guid userId, IEnumerable<UserNetworkConnection> connections);

    /// <summary>
    /// Loads pre-trained ML models from storage
    /// </summary>
    Task LoadModelsAsync(string modelsPath);

    /// <summary>
    /// Trains a new behavior detection model with provided data
    /// </summary>
    Task<ModelTrainingResult> TrainBehaviorModelAsync(IEnumerable<UserBehaviorData> trainingData);
}

// Data models for ML integration
public class UserBehaviorData
{
    public float ReviewFrequency { get; set; }
    public float AvgTimeBetweenReviews { get; set; }
    public float ContentSimilarityScore { get; set; }
    public float DeviceConsistencyScore { get; set; }
    public float GeographicConsistencyScore { get; set; }
    public bool IsGaming { get; set; } // Label for training
}

public class ContentAnalysisData
{
    public string CurrentText { get; set; } = string.Empty;
    public IEnumerable<string> ComparisonTexts { get; set; } = Enumerable.Empty<string>();
}

public class NetworkRisk
{
    public Guid UserId { get; set; }
    public float RiskScore { get; set; }
    public int ConnectionCount { get; set; }
    public List<UserNetworkConnection> SuspiciousPatterns { get; set; } = new();
    public DateTime AnalyzedAt { get; set; }
}

public class ModelTrainingResult
{
    public bool Success { get; set; }
    public float Accuracy { get; set; }
    public string ModelType { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTime TrainedAt { get; set; }
}