using SkillLedger.Core.Entities;
using SkillLedger.Core.DTOs;

namespace SkillLedger.Core.Interfaces;

/// <summary>
/// Service interface for managing user reputation scoring and calculations
/// </summary>
public interface IReputationService
{
    /// <summary>
    /// Calculates comprehensive reputation scores for a user based on all their reviews
    /// </summary>
    /// <param name="userId">The user to calculate reputation for</param>
    /// <returns>Complete user reputation scores including component breakdowns</returns>
    Task<UserReputationScores> CalculateUserReputationAsync(Guid userId);

    /// <summary>
    /// Calculates reputation scores for a specific skill category
    /// </summary>
    /// <param name="userId">The user to calculate reputation for</param>
    /// <param name="skillId">The specific skill category</param>
    /// <returns>Category-specific reputation scores</returns>
    Task<CategoryReputationScores> CalculateCategoryReputationAsync(Guid userId, Guid skillId);

    /// <summary>
    /// Recalculates and updates existing user reputation scores
    /// </summary>
    /// <param name="userId">The user to recalculate reputation for</param>
    /// <returns>Updated user reputation scores</returns>
    Task<UserReputationScores> RecalculateUserReputationAsync(Guid userId);

    /// <summary>
    /// Gets detailed breakdown of reputation components and trends
    /// </summary>
    /// <param name="userId">The user to get breakdown for</param>
    /// <returns>Detailed reputation breakdown with trends and explanations</returns>
    Task<UserReputationBreakdownDto> GetUserReputationBreakdownAsync(Guid userId);

    /// <summary>
    /// Applies performance streak bonus to reputation scores
    /// </summary>
    /// <param name="userReputation">The user reputation to apply bonus to</param>
    /// <returns>True if bonus was applied</returns>
    Task<bool> ApplyStreakBonusAsync(UserReputationScores userReputation);

    /// <summary>
    /// Applies penalty to user reputation for negative actions
    /// </summary>
    /// <param name="userId">The user to apply penalty to</param>
    /// <param name="penaltyType">Type of penalty (cancellation, dispute, etc.)</param>
    /// <param name="severity">Severity level of the penalty (1-5)</param>
    /// <returns>True if penalty was applied</returns>
    Task<bool> ApplyPenaltyAsync(Guid userId, string penaltyType, int severity);

    /// <summary>
    /// Gets top performing users by reputation score
    /// </summary>
    /// <param name="count">Number of top performers to return</param>
    /// <param name="skillId">Optional: filter by specific skill category</param>
    /// <returns>List of top performing users with their scores</returns>
    Task<List<UserReputationScores>> GetTopPerformersAsync(int count, Guid? skillId = null);

    /// <summary>
    /// Triggers real-time recalculation when new review is submitted
    /// </summary>
    /// <param name="reviewId">The new review that was submitted</param>
    /// <returns>Updated reputation scores</returns>
    Task<UserReputationScores> OnReviewSubmittedAsync(Guid reviewId);

    /// <summary>
    /// Gets reputation trend data for charts and analytics
    /// </summary>
    /// <param name="userId">The user to get trends for</param>
    /// <param name="periodMonths">Number of months to include in trend</param>
    /// <returns>Reputation trend data over time</returns>
    Task<List<ReputationTrendDataDto>> GetReputationTrendAsync(Guid userId, int periodMonths = 12);
}