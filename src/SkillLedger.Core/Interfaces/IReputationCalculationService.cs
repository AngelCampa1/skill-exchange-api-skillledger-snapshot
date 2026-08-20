using SkillLedger.Core.DTOs;

namespace SkillLedger.Core.Interfaces;

/// <summary>
/// Service for calculating and managing user reputation scores with weighted algorithms,
/// time decay, performance streaks, and penalty systems
/// </summary>
public interface IReputationCalculationService
{
    /// <summary>
    /// Calculate the overall reputation score for a user using weighted algorithms
    /// </summary>
    /// <param name="userId">User ID to calculate score for</param>
    /// <returns>Complete reputation score with breakdown</returns>
    Task<UserReputationScoreDto?> CalculateOverallReputationScoreAsync(Guid userId);

    /// <summary>
    /// Calculate reputation score for a specific skill category
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="skillCategoryId">Skill category ID</param>
    /// <returns>Category-specific reputation score</returns>
    Task<CategoryReputationScoreDto?> CalculateCategoryReputationScoreAsync(Guid userId, Guid skillCategoryId);

    /// <summary>
    /// Get all category reputation scores for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of all category scores</returns>
    Task<List<CategoryReputationScoreDto>> GetAllCategoryScoresAsync(Guid userId);

    /// <summary>
    /// Get detailed reputation breakdown showing how score was calculated
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Detailed breakdown of reputation calculation</returns>
    Task<ReputationBreakdownDto?> GetReputationBreakdownAsync(Guid userId);

    /// <summary>
    /// Get reputation history over time
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="days">Number of days to look back</param>
    /// <param name="page">Page number for pagination</param>
    /// <param name="pageSize">Number of entries per page</param>
    /// <returns>Historical reputation data</returns>
    Task<List<ReputationHistoryDto>> GetReputationHistoryAsync(Guid userId, int days = 90, int page = 1, int pageSize = 20);

    /// <summary>
    /// Get reputation trend analysis
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="days">Period to analyze</param>
    /// <returns>Trend analysis data</returns>
    Task<ReputationTrendDto?> GetReputationTrendAsync(Guid userId, int days = 30);

    /// <summary>
    /// Calculate performance streak bonus based on consistent high ratings
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Streak bonus amount</returns>
    Task<decimal> CalculatePerformanceStreakBonusAsync(Guid userId);

    /// <summary>
    /// Calculate penalties for cancellations, disputes, and other negative events
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Total penalty amount</returns>
    Task<decimal> CalculatePenaltiesAsync(Guid userId);

    /// <summary>
    /// Calculate time decay factor for a given date
    /// </summary>
    /// <param name="date">Date to calculate decay for</param>
    /// <returns>Decay factor (0-1)</returns>
    decimal CalculateTimeDecayFactor(DateTime date);

    /// <summary>
    /// Recalculate and save reputation score to database
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Updated reputation score</returns>
    Task<UserReputationScoreDto?> RecalculateAndSaveReputationScoreAsync(Guid userId);

    /// <summary>
    /// Recalculate and save category reputation score
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="categoryId">Category ID</param>
    /// <returns>Updated category score</returns>
    Task<CategoryReputationScoreDto?> RecalculateAndSaveCategoryScoreAsync(Guid userId, Guid categoryId);

    /// <summary>
    /// Bulk recalculate reputation scores for all users
    /// </summary>
    /// <returns>Number of users processed</returns>
    Task<int> BulkRecalculateReputationScoresAsync();

    /// <summary>
    /// Trigger reputation score update when a new review is published
    /// </summary>
    /// <param name="reviewId">Review ID that was published</param>
    /// <returns>Updated reputation scores for affected users</returns>
    Task<List<UserReputationScoreDto>> UpdateReputationOnReviewPublishAsync(Guid reviewId);

    /// <summary>
    /// Trigger reputation score update when a project is completed
    /// </summary>
    /// <param name="projectId">Project ID that was completed</param>
    /// <returns>Updated reputation scores for affected users</returns>
    Task<List<UserReputationScoreDto>> UpdateReputationOnProjectCompletionAsync(Guid projectId);
}