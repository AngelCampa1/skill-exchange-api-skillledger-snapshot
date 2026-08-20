using SkillLedger.Core.Entities;
using SkillLedger.Core.Interfaces;

namespace SkillLedger.Tests.Mocks;

/// <summary>
/// Mock implementation of IGamingDetectionML for testing
/// </summary>
public class MockGamingDetectionML : IGamingDetectionML
{
    private bool _disposed = false;

    public Task<float> AnalyzeBehaviorPatternAsync(UserBehaviorData data)
    {
        // Enhanced mock implementation that returns higher risk scores for gaming patterns
        float riskScore = 0.1f; // Base risk score

        // High-frequency reviewing (velocity attacks)
        if (data.ReviewFrequency > 10f)
            riskScore += 0.5f;
        else if (data.ReviewFrequency > 5f)
            riskScore += 0.3f;
        else if (data.ReviewFrequency > 3f)
            riskScore += 0.2f;

        // Content similarity analysis
        if (data.ContentSimilarityScore > 0.8f)
            riskScore += 0.4f;
        else if (data.ContentSimilarityScore > 0.6f)
            riskScore += 0.3f;
        else if (data.ContentSimilarityScore > 0.4f)
            riskScore += 0.2f;

        // Device consistency issues
        if (data.DeviceConsistencyScore < 0.3f)
            riskScore += 0.4f;
        else if (data.DeviceConsistencyScore < 0.5f)
            riskScore += 0.3f;
        else if (data.DeviceConsistencyScore < 0.7f)
            riskScore += 0.2f;

        // Geographic consistency
        if (data.GeographicConsistencyScore < 0.4f)
            riskScore += 0.3f;

        // Explicit gaming flag
        if (data.IsGaming)
            riskScore += 0.6f;

        // Time between reviews (short intervals indicate automation)
        if (data.AvgTimeBetweenReviews < 60f) // Less than 1 minute
            riskScore += 0.4f;
        else if (data.AvgTimeBetweenReviews < 300f) // Less than 5 minutes
            riskScore += 0.3f;
        else if (data.AvgTimeBetweenReviews < 1800f) // Less than 30 minutes
            riskScore += 0.2f;

        return Task.FromResult(Math.Min(1.0f, riskScore));
    }

    public Task<float> AnalyzeContentSimilarityAsync(ContentAnalysisData data)
    {
        // Mock implementation that returns higher similarity scores for test scenarios
        float similarityScore = 0.2f; // Base similarity score

        if (!string.IsNullOrEmpty(data.CurrentText) && data.ComparisonTexts.Any())
        {
            // Check for gaming patterns - increase similarity for suspicious content
            var currentTextLower = data.CurrentText.ToLower();

            // High similarity patterns (content spinning attacks)
            if (currentTextLower.Contains("excellent") ||
                currentTextLower.Contains("outstanding") ||
                currentTextLower.Contains("professional"))
            {
                similarityScore += 0.3f;
            }

            // Check against comparison texts for duplication
            var similarCount = data.ComparisonTexts.Count(text =>
            {
                var textLower = text.ToLower();
                return (textLower.Contains("excellent") ||
                       textLower.Contains("outstanding") ||
                       textLower.Contains("professional") ||
                       textLower.Contains("great work")) &&
                       Math.Abs(text.Length - data.CurrentText.Length) < 50;
            });

            if (similarCount > 0)
            {
                similarityScore += (float)similarCount / data.ComparisonTexts.Count() * 0.5f;
            }
        }

        return Task.FromResult(Math.Min(1.0f, similarityScore));
    }

    public Task<NetworkRisk> AnalyzeUserNetworkAsync(Guid userId, IEnumerable<UserNetworkConnection> connections)
    {
        // Simple mock implementation for network analysis
        var connectionList = connections.ToList();
        var connectionCount = connectionList.Count;
        var avgStrength = connectionCount > 0 ? (float)connectionList.Average(c => c.ConnectionStrength) : 0f;

        var riskScore = Math.Min(1.0f, (connectionCount * 0.1f) + (avgStrength * 0.2f));

        return Task.FromResult(new NetworkRisk
        {
            UserId = userId,
            RiskScore = riskScore,
            ConnectionCount = connectionCount,
            SuspiciousPatterns = new List<UserNetworkConnection>(),
            AnalyzedAt = DateTime.UtcNow
        });
    }

    public Task LoadModelsAsync(string modelsPath)
    {
        // Mock implementation - does nothing
        return Task.CompletedTask;
    }

    public Task<ModelTrainingResult> TrainBehaviorModelAsync(IEnumerable<UserBehaviorData> trainingData)
    {
        // Mock implementation - returns success
        return Task.FromResult(new ModelTrainingResult
        {
            Success = true,
            Accuracy = 0.95f,
            ModelType = "MockMLModel",
            ErrorMessage = null,
            TrainedAt = DateTime.UtcNow
        });
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}

