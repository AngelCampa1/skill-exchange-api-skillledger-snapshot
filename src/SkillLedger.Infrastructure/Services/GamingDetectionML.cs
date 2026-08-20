using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Interfaces;

namespace SkillLedger.Infrastructure.Services;

/// <summary>
/// Machine Learning service for gaming detection using ML.NET
/// </summary>
public class GamingDetectionML : IGamingDetectionML
{
    private readonly MLContext _mlContext;
    private readonly ILogger<GamingDetectionML> _logger;
    private ITransformer? _behaviorModel;
    private ITransformer? _contentModel;

    public GamingDetectionML(ILogger<GamingDetectionML> logger)
    {
        _mlContext = new MLContext(seed: 42);
        _logger = logger;
    }

    /// <summary>
    /// Analyzes user behavior patterns for gaming detection
    /// </summary>
    public async Task<float> AnalyzeBehaviorPatternAsync(UserBehaviorData data)
    {
        try
        {
            if (_behaviorModel == null)
            {
                _logger.LogWarning("Behavior ML model not loaded, using rule-based detection");
                return await AnalyzeWithRulesAsync(data);
            }

            var predictionEngine = _mlContext.Model.CreatePredictionEngine<UserBehaviorData, BehaviorPrediction>(_behaviorModel);
            var prediction = predictionEngine.Predict(data);

            _logger.LogDebug("Behavior analysis completed: Risk Score = {RiskScore}, Confidence = {Confidence}",
                prediction.RiskScore, prediction.Probability);

            return prediction.RiskScore;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing behavior pattern for user");
            return await AnalyzeWithRulesAsync(data);
        }
    }

    /// <summary>
    /// Analyzes content for similarity and gaming patterns
    /// </summary>
    public async Task<float> AnalyzeContentSimilarityAsync(ContentAnalysisData data)
    {
        try
        {
            if (_contentModel == null)
            {
                _logger.LogWarning("Content ML model not loaded, using rule-based analysis");
                return await AnalyzeContentWithRulesAsync(data);
            }

            var predictionEngine = _mlContext.Model.CreatePredictionEngine<ContentAnalysisData, ContentPrediction>(_contentModel);
            var prediction = predictionEngine.Predict(data);

            _logger.LogDebug("Content analysis completed: Similarity Score = {Score}", prediction.SimilarityScore);

            return prediction.SimilarityScore;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing content similarity");
            return await AnalyzeContentWithRulesAsync(data);
        }
    }

    /// <summary>
    /// Analyzes user network connections for coordinated attacks
    /// </summary>
    public Task<NetworkRisk> AnalyzeUserNetworkAsync(Guid userId, IEnumerable<UserNetworkConnection> connections)
    {
        try
        {
            var networkFeatures = ExtractNetworkFeatures(connections);

            // For now, use rule-based analysis
            // TODO: Implement graph neural network models for network analysis
            var riskScore = CalculateNetworkRiskScore(networkFeatures);

            return Task.FromResult(new NetworkRisk
            {
                UserId = userId,
                RiskScore = riskScore,
                ConnectionCount = networkFeatures.ConnectionCount,
                SuspiciousPatterns = networkFeatures.SuspiciousPatterns,
                AnalyzedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing user network for user {UserId}", userId);
            return Task.FromResult(new NetworkRisk
            {
                UserId = userId,
                RiskScore = 0.5f,
                AnalyzedAt = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Loads pre-trained ML models from storage
    /// </summary>
    public async Task LoadModelsAsync(string modelsPath)
    {
        try
        {
            var behaviorModelPath = Path.Combine(modelsPath, "behavior-model.zip");
            var contentModelPath = Path.Combine(modelsPath, "content-model.zip");

            if (File.Exists(behaviorModelPath))
            {
                _behaviorModel = _mlContext.Model.Load(behaviorModelPath, out var behaviorSchema);
                _logger.LogInformation("Behavior detection model loaded successfully");
            }
            else
            {
                _logger.LogWarning("Behavior model file not found at {Path}. Using rule-based detection.", behaviorModelPath);
            }

            if (File.Exists(contentModelPath))
            {
                _contentModel = _mlContext.Model.Load(contentModelPath, out var contentSchema);
                _logger.LogInformation("Content similarity model loaded successfully");
            }
            else
            {
                _logger.LogWarning("Content model file not found at {Path}. Using rule-based analysis.", contentModelPath);
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading ML models from {Path}", modelsPath);
        }
    }

    /// <summary>
    /// Trains a new behavior detection model with provided data
    /// </summary>
    public Task<ModelTrainingResult> TrainBehaviorModelAsync(IEnumerable<UserBehaviorData> trainingData)
    {
        try
        {
            _logger.LogInformation("Starting behavior model training with {Count} samples", trainingData.Count());

            var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

            // Create training pipeline
            var pipeline = _mlContext.Transforms.Conversion.MapValueToKey(outputColumnName: "Label", inputColumnName: nameof(UserBehaviorData.IsGaming))
                .Append(_mlContext.Transforms.Concatenate("Features",
                    nameof(UserBehaviorData.ReviewFrequency),
                    nameof(UserBehaviorData.AvgTimeBetweenReviews),
                    nameof(UserBehaviorData.ContentSimilarityScore),
                    nameof(UserBehaviorData.DeviceConsistencyScore),
                    nameof(UserBehaviorData.GeographicConsistencyScore)))
                .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy("Label", "Features"))
                .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

            _logger.LogInformation("Training behavior model...");
            _behaviorModel = pipeline.Fit(dataView);

            // Evaluate model
            var predictions = _behaviorModel.Transform(dataView);
            var metrics = _mlContext.MulticlassClassification.Evaluate(predictions, "Label", "Score");

            _logger.LogInformation("Model training completed. Accuracy: {Accuracy:P2}", metrics.MacroAccuracy);

            return Task.FromResult(new ModelTrainingResult
            {
                Success = true,
                Accuracy = (float)metrics.MacroAccuracy,
                ModelType = "BehaviorDetection",
                TrainedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error training behavior model");
            return Task.FromResult(new ModelTrainingResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ModelType = "BehaviorDetection",
                TrainedAt = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Rule-based fallback analysis when ML models are not available
    /// </summary>
    private Task<float> AnalyzeWithRulesAsync(UserBehaviorData data)
    {
        var riskScore = 0.0f;

        // High review frequency indicates potential gaming
        if (data.ReviewFrequency > 10) riskScore += 0.3f;
        else if (data.ReviewFrequency > 5) riskScore += 0.2f;

        // Very short time between reviews is suspicious
        if (data.AvgTimeBetweenReviews < 300) riskScore += 0.4f; // Less than 5 minutes
        else if (data.AvgTimeBetweenReviews < 3600) riskScore += 0.2f; // Less than 1 hour

        // High content similarity suggests copy-paste behavior
        if (data.ContentSimilarityScore > 0.8f) riskScore += 0.4f;
        else if (data.ContentSimilarityScore > 0.6f) riskScore += 0.2f;

        // Low device/geographic consistency indicates multiple users
        if (data.DeviceConsistencyScore < 0.3f) riskScore += 0.3f;
        if (data.GeographicConsistencyScore < 0.3f) riskScore += 0.2f;

        return Task.FromResult(Math.Min(1.0f, riskScore));
    }

    /// <summary>
    /// Rule-based content analysis fallback
    /// </summary>
    private Task<float> AnalyzeContentWithRulesAsync(ContentAnalysisData data)
    {
        // Simple text similarity using Levenshtein distance ratio
        var similarity = CalculateTextSimilarity(data.CurrentText, data.ComparisonTexts);
        return Task.FromResult(similarity);
    }

    private float CalculateTextSimilarity(string text1, IEnumerable<string> comparisonTexts)
    {
        if (!comparisonTexts.Any()) return 0.0f;

        var maxSimilarity = 0.0f;
        foreach (var comparisonText in comparisonTexts)
        {
            var similarity = CalculateLevenshteinRatio(text1, comparisonText);
            maxSimilarity = Math.Max(maxSimilarity, similarity);
        }

        return maxSimilarity;
    }

    private float CalculateLevenshteinRatio(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) && string.IsNullOrEmpty(s2)) return 1.0f;
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2)) return 0.0f;

        var distance = LevenshteinDistance(s1, s2);
        var maxLength = Math.Max(s1.Length, s2.Length);
        return 1.0f - (float)distance / maxLength;
    }

    private int LevenshteinDistance(string s1, string s2)
    {
        var matrix = new int[s1.Length + 1, s2.Length + 1];

        for (int i = 0; i <= s1.Length; i++) matrix[i, 0] = i;
        for (int j = 0; j <= s2.Length; j++) matrix[0, j] = j;

        for (int i = 1; i <= s1.Length; i++)
        {
            for (int j = 1; j <= s2.Length; j++)
            {
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(Math.Min(
                    matrix[i - 1, j] + 1,      // deletion
                    matrix[i, j - 1] + 1),     // insertion
                    matrix[i - 1, j - 1] + cost); // substitution
            }
        }

        return matrix[s1.Length, s2.Length];
    }

    private NetworkFeatures ExtractNetworkFeatures(IEnumerable<UserNetworkConnection> connections)
    {
        var connectionList = connections.ToList();

        return new NetworkFeatures
        {
            ConnectionCount = connectionList.Count,
            AvgConnectionStrength = connectionList.Any() ? connectionList.Average(c => (double)c.ConnectionStrength) : 0,
            SharedDeviceConnections = connectionList.Count(c => c.ConnectionType == "SharedDevice"),
            SharedIPConnections = connectionList.Count(c => c.ConnectionType == "IPSharing"),
            SuspiciousPatterns = connectionList.Where(c => c.ConnectionStrength > 0.8m).ToList()
        };
    }

    private float CalculateNetworkRiskScore(NetworkFeatures features)
    {
        var riskScore = 0.0f;

        // High number of connections is suspicious
        if (features.ConnectionCount > 10) riskScore += 0.4f;
        else if (features.ConnectionCount > 5) riskScore += 0.2f;

        // Very strong connections indicate coordinated behavior
        if (features.AvgConnectionStrength > 0.8) riskScore += 0.3f;
        else if (features.AvgConnectionStrength > 0.6) riskScore += 0.1f;

        // Multiple shared devices/IPs are red flags
        if (features.SharedDeviceConnections > 3) riskScore += 0.3f;
        if (features.SharedIPConnections > 5) riskScore += 0.2f;

        return Math.Min(1.0f, riskScore);
    }

    public void Dispose()
    {
        // ML.NET models are disposed automatically
    }
}

// ML.NET prediction models (internal to this service)
public class BehaviorPrediction
{
    [ColumnName("Score")]
    public float[] Scores { get; set; } = Array.Empty<float>();

    [ColumnName("PredictedLabel")]
    public string PredictedLabel { get; set; } = string.Empty;

    public float RiskScore => Scores?.Max() ?? 0.0f;
    public float Probability => Scores?.Max() ?? 0.0f;
}

public class ContentPrediction
{
    public float SimilarityScore { get; set; }
}

public class NetworkFeatures
{
    public int ConnectionCount { get; set; }
    public double AvgConnectionStrength { get; set; }
    public int SharedDeviceConnections { get; set; }
    public int SharedIPConnections { get; set; }
    public List<UserNetworkConnection> SuspiciousPatterns { get; set; } = new();
}