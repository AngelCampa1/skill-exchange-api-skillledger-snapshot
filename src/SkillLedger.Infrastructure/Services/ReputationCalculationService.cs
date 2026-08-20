using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;

namespace SkillLedger.Infrastructure.Services;

/// <summary>
/// Service for calculating user reputation scores using weighted algorithms,
/// time decay, performance streaks, and penalty systems
/// </summary>
public class ReputationCalculationService : IReputationCalculationService
{
    private readonly SkillLedgerDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly IDistributedLockService _distributedLockService;
    private readonly ILogger<ReputationCalculationService> _logger;

    // Algorithm constants
    private const decimal QualityWeight = 0.4m;
    private const decimal CommunicationWeight = 0.2m;
    private const decimal TimelinessWeight = 0.2m;
    private const decimal ProfessionalismWeight = 0.2m;
    private const decimal BaseScore = 3.0m;
    private const decimal MaxStreakBonus = 0.5m;
    private const decimal MaxPenalty = 1.0m;
    private const int DaysForTimeDecay = 365;

    public ReputationCalculationService(
        SkillLedgerDbContext context,
        IAuditLogService auditLogService,
        IDistributedLockService distributedLockService,
        ILogger<ReputationCalculationService> logger)
    {
        _context = context;
        _auditLogService = auditLogService;
        _distributedLockService = distributedLockService;
        _logger = logger;
    }

    public async Task<UserReputationScoreDto?> CalculateOverallReputationScoreAsync(Guid userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found for reputation calculation: {UserId}", userId);
                return null;
            }

            // PERFORMANCE FIX: Use AsNoTracking for read-only reputation calculation queries
            var reviews = await _context.ProjectReviews
                .AsNoTracking()
                .Where(r => r.RevieweeId == userId && r.Status == ProjectReviewStatus.Published)
                .OrderByDescending(r => r.PublishedAt)
                .ToListAsync();

            // Calculate base score from reviews
            var reviewsScore = await CalculateReviewsScoreAsync(reviews);

            // Calculate completion rate
            var completionRate = await CalculateProjectCompletionRateAsync(userId);

            // Calculate performance streak bonus
            var streakBonus = await CalculatePerformanceStreakBonusAsync(userId);

            // Calculate penalties
            var penalties = await CalculatePenaltiesAsync(userId);

            // Calculate average response time (mock implementation)
            var avgResponseTime = await CalculateAverageResponseTimeAsync(userId);

            // Calculate final score
            var finalScore = Math.Max(0m, Math.Min(5.0m,
                reviewsScore + streakBonus - penalties));

            // PERFORMANCE FIX: Use AsNoTracking for read-only count queries
            var totalCompleted = await _context.Projects
                .AsNoTracking()
                .Where(p => p.Status == ProjectStatus.Completed)
                .Where(p => p.ClientId == userId || p.ProviderId == userId)
                .CountAsync();

            return new UserReputationScoreDto
            {
                UserId = userId,
                OverallScore = Math.Round(finalScore, 2),
                ProjectCompletionRate = Math.Round(completionRate, 2),
                AverageResponseTime = avgResponseTime,
                TotalProjectsCompleted = totalCompleted,
                LastUpdated = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating reputation score for user {UserId}", userId);
            throw;
        }
    }

    public async Task<CategoryReputationScoreDto?> CalculateCategoryReputationScoreAsync(Guid userId, Guid skillId)
    {
        try
        {
            var skill = await _context.Skills.FindAsync(skillId);
            if (skill == null)
            {
                return null;
            }

            // PERFORMANCE FIX: Use AsNoTracking + AsSplitQuery for read-only category queries with Include
            var categoryReviews = await _context.ProjectReviews
                .AsNoTracking()
                .Where(r => r.RevieweeId == userId && r.Status == ProjectReviewStatus.Published)
                .Where(r => r.Project.ProjectSkills.Any(ps => ps.SkillId == skillId))
                .Include(r => r.Project)
                .AsSplitQuery()
                .ToListAsync();

            if (!categoryReviews.Any())
            {
                return new CategoryReputationScoreDto
                {
                    UserId = userId,
                    SkillId = skillId,
                    SkillName = skill.Name,
                    Score = BaseScore,
                    ProjectCount = 0,
                    LastProjectAt = null
                };
            }

            var score = await CalculateReviewsScoreAsync(categoryReviews);
            var lastProject = categoryReviews.Max(r => r.Project.CompletedAt);

            return new CategoryReputationScoreDto
            {
                UserId = userId,
                SkillId = skillId,
                SkillName = skill.Name,
                Score = Math.Round(score, 2),
                ProjectCount = categoryReviews.Count,
                LastProjectAt = lastProject
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating category reputation score for user {UserId}, skill {SkillId}", userId, skillId);
            throw;
        }
    }

    public async Task<List<CategoryReputationScoreDto>> GetAllCategoryScoresAsync(Guid userId)
    {
        try
        {
            // PERFORMANCE FIX: Use AsNoTracking for read-only skill ID collection
            var skillIds = await _context.ProjectReviews
                .AsNoTracking()
                .Where(r => r.RevieweeId == userId && r.Status == ProjectReviewStatus.Published)
                .SelectMany(r => r.Project.ProjectSkills.Select(ps => ps.SkillId))
                .Distinct()
                .ToListAsync();

            var categoryScores = new List<CategoryReputationScoreDto>();
            foreach (var skillId in skillIds)
            {
                var score = await CalculateCategoryReputationScoreAsync(userId, skillId);
                if (score != null)
                {
                    categoryScores.Add(score);
                }
            }

            return categoryScores.OrderByDescending(cs => cs.Score).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all category scores for user {UserId}", userId);
            throw;
        }
    }

    public async Task<ReputationBreakdownDto?> GetReputationBreakdownAsync(Guid userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return null;
            }

            // PERFORMANCE FIX: Use AsNoTracking for read-only breakdown queries
            var reviews = await _context.ProjectReviews
                .AsNoTracking()
                .Where(r => r.RevieweeId == userId && r.Status == ProjectReviewStatus.Published)
                .ToListAsync();

            var reviewsScore = await CalculateReviewsScoreAsync(reviews);
            var completionRate = await CalculateProjectCompletionRateAsync(userId);
            var streakBonus = await CalculatePerformanceStreakBonusAsync(userId);
            var penalties = await CalculatePenaltiesAsync(userId);

            // PERFORMANCE FIX: Calculate individual rating averages at database level instead of in-memory
            var averageQualityRating = await _context.ProjectReviews
                .AsNoTracking()
                .Where(r => r.RevieweeId == userId && r.Status == ProjectReviewStatus.Published && r.QualityRating.HasValue)
                .AverageAsync(r => (decimal?)r.QualityRating) ?? 0m;

            var averageCommunicationRating = await _context.ProjectReviews
                .AsNoTracking()
                .Where(r => r.RevieweeId == userId && r.Status == ProjectReviewStatus.Published && r.CommunicationRating.HasValue)
                .AverageAsync(r => (decimal?)r.CommunicationRating) ?? 0m;

            var averageTimelinessRating = await _context.ProjectReviews
                .AsNoTracking()
                .Where(r => r.RevieweeId == userId && r.Status == ProjectReviewStatus.Published && r.TimelinessRating.HasValue)
                .AverageAsync(r => (decimal?)r.TimelinessRating) ?? 0m;

            var averageProfessionalismRating = await _context.ProjectReviews
                .AsNoTracking()
                .Where(r => r.RevieweeId == userId && r.Status == ProjectReviewStatus.Published && r.ProfessionalismRating.HasValue)
                .AverageAsync(r => (decimal?)r.ProfessionalismRating) ?? 0m;

            var completionBonus = Math.Max(0m, (completionRate - 0.8m) * 0.5m); // Bonus for >80% completion

            // LOW-PRIORITY FIX: Calculate actual time decay based on recent activity
            var timeDecayFactor = await CalculateTimeDecayFactorAsync(userId, reviews);
            var decayAdjustedScore = reviewsScore * timeDecayFactor;

            var finalScore = Math.Max(0m, Math.Min(5.0m, decayAdjustedScore + streakBonus + completionBonus - penalties));

            var explanation = GenerateScoreExplanation(decayAdjustedScore, completionRate, streakBonus, penalties, finalScore);

            return new ReputationBreakdownDto
            {
                UserId = userId,
                BaseScore = BaseScore,
                StreakBonus = Math.Round(streakBonus, 2),
                Penalties = Math.Round(penalties, 2),
                TimeDecayFactor = Math.Round(timeDecayFactor, 2), // LOW-PRIORITY FIX: Actual time decay
                Components = new ReputationComponentsDto
                {
                    QualityRating = Math.Round(averageQualityRating, 2),
                    CommunicationRating = Math.Round(averageCommunicationRating, 2),
                    TimelinessRating = Math.Round(averageTimelinessRating, 2),
                    ProfessionalismRating = Math.Round(averageProfessionalismRating, 2),
                    WeightedContributions = new ReputationWeightsDto
                    {
                        QualityContribution = Math.Round(averageQualityRating * QualityWeight, 2),
                        CommunicationContribution = Math.Round(averageCommunicationRating * CommunicationWeight, 2),
                        TimelinessContribution = Math.Round(averageTimelinessRating * TimelinessWeight, 2),
                        ProfessionalismContribution = Math.Round(averageProfessionalismRating * ProfessionalismWeight, 2)
                    }
                },
                CalculatedAt = DateTime.UtcNow,
                FinalScore = Math.Round(finalScore, 2),
                Explanation = explanation
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reputation breakdown for user {UserId}", userId);
            throw;
        }
    }

    public async Task<List<ReputationHistoryDto>> GetReputationHistoryAsync(Guid userId, int days = 90, int page = 1, int pageSize = 20)
    {
        try
        {
            var startDate = DateTime.UtcNow.AddDays(-days);

            var history = await _context.ReputationHistories
                .Where(h => h.UserId == userId && h.Date >= startDate)
                .OrderByDescending(h => h.Date)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(h => new ReputationHistoryDto
                {
                    UserId = h.UserId,
                    Date = h.Date,
                    Score = h.Score,
                    ChangeReason = h.ChangeReason,
                    ProjectId = h.ProjectId,
                    ReviewId = h.ReviewId
                })
                .ToListAsync();

            return history;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reputation history for user {UserId}", userId);
            throw;
        }
    }

    public async Task<ReputationTrendDto?> GetReputationTrendAsync(Guid userId, int days = 30)
    {
        try
        {
            var currentScore = await CalculateOverallReputationScoreAsync(userId);
            if (currentScore == null)
            {
                return null;
            }

            var startDate = DateTime.UtcNow.AddDays(-days);
            var previousHistory = await _context.ReputationHistories
                .Where(h => h.UserId == userId && h.Date <= startDate)
                .OrderByDescending(h => h.Date)
                .FirstOrDefaultAsync();

            var previousScore = previousHistory?.Score ?? BaseScore;
            var change = currentScore.OverallScore - previousScore;
            var changePercent = previousScore > 0 ? (change / previousScore) * 100 : 0;

            var trend = change > 0.1m ? ReputationTrend.Improving :
                       change < -0.1m ? ReputationTrend.Declining :
                       ReputationTrend.Stable;

            // PERFORMANCE FIX: Use AsNoTracking for read-only count queries
            var totalReviews = await _context.ProjectReviews
                .AsNoTracking()
                .Where(r => r.RevieweeId == userId && r.Status == ProjectReviewStatus.Published)
                .CountAsync();

            var recentReviews = await _context.ProjectReviews
                .AsNoTracking()
                .Where(r => r.RevieweeId == userId && r.Status == ProjectReviewStatus.Published)
                .Where(r => r.PublishedAt >= startDate)
                .CountAsync();

            var user = await _context.Users.FindAsync(userId);
            var daysActive = user != null ? (int)(DateTime.UtcNow - user.CreatedAt).TotalDays : 0;

            return new ReputationTrendDto
            {
                UserId = userId,
                CurrentScore = currentScore.OverallScore,
                PreviousScore = previousScore,
                Trend = trend,
                TrendPercentage = Math.Round(changePercent, 2),
                DaysActive = Math.Max(1, daysActive),
                TotalReviews = totalReviews,
                RecentReviews = recentReviews
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reputation trend for user {UserId}", userId);
            throw;
        }
    }

    public async Task<decimal> CalculatePerformanceStreakBonusAsync(Guid userId)
    {
        try
        {
            // PERFORMANCE FIX: Use AsNoTracking for read-only streak calculation
            var recentReviews = await _context.ProjectReviews
                .AsNoTracking()
                .Where(r => r.RevieweeId == userId && r.Status == ProjectReviewStatus.Published)
                .Where(r => r.PublishedAt >= DateTime.UtcNow.AddDays(-180)) // Last 6 months
                .OrderByDescending(r => r.PublishedAt)
                .Take(10) // Check last 10 reviews
                .ToListAsync();

            if (recentReviews.Count < 3)
            {
                return 0m; // Need at least 3 reviews for a streak
            }

            var consecutiveHighRatings = 0;
            foreach (var review in recentReviews)
            {
                var avgRating = CalculateReviewAverage(review);
                if (avgRating >= 8.0m) // High rating threshold
                {
                    consecutiveHighRatings++;
                }
                else
                {
                    break; // Streak broken
                }
            }

            if (consecutiveHighRatings >= 5)
            {
                return Math.Min(MaxStreakBonus, consecutiveHighRatings * 0.05m);
            }

            return 0m;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating performance streak bonus for user {UserId}", userId);
            return 0m;
        }
    }

    /// <summary>
    /// LOW-PRIORITY FIX: Calculate time decay factor based on recent activity
    /// More recent reviews have higher weight, older reviews gradually lose influence
    /// </summary>
    private async Task<decimal> CalculateTimeDecayFactorAsync(Guid userId, List<ProjectReview> reviews)
    {
        try
        {
            if (!reviews.Any())
            {
                // Check if user has ever had projects
                var hasProjects = await _context.Projects
                    .AnyAsync(p => p.ClientId == userId || p.ProviderId == userId);

                // New users get full score, inactive users get penalty
                return hasProjects ? 0.7m : 1.0m;
            }

            // Calculate weighted average age of reviews
            var now = DateTime.UtcNow;
            var totalWeight = 0m;
            var weightedSum = 0m;

            foreach (var review in reviews)
            {
                var ageInDays = (now - review.CreatedAt).TotalDays;

                // Exponential decay: newer reviews weighted more heavily
                // 100% weight for reviews < 30 days old
                // 90% weight at 90 days
                // 75% weight at 180 days
                // 50% weight at 365 days
                var decayFactor = ageInDays <= 30 ? 1.0m :
                                  ageInDays <= 90 ? 0.9m :
                                  ageInDays <= 180 ? 0.75m :
                                  ageInDays <= 365 ? 0.5m :
                                  0.3m; // Older than 1 year

                weightedSum += decayFactor;
                totalWeight += 1m;
            }

            // Return average decay factor across all reviews
            var avgDecay = totalWeight > 0 ? weightedSum / totalWeight : 1.0m;

            // Ensure decay factor is between 0.3 and 1.0
            return Math.Max(0.3m, Math.Min(1.0m, avgDecay));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating time decay factor for user {UserId}", userId);
            return 1.0m; // Default to no decay on error
        }
    }

    public async Task<decimal> CalculatePenaltiesAsync(Guid userId)
    {
        try
        {
            var totalPenalty = 0m;

            // Cancellation penalties
            // PERFORMANCE FIX: Use AsNoTracking for read-only penalty calculations
            var recentCancellations = await _context.Projects
                .AsNoTracking()
                .Where(p => (p.ClientId == userId || p.ProviderId == userId) &&
                           p.Status == ProjectStatus.Cancelled &&
                           p.CancelledAt >= DateTime.UtcNow.AddYears(-1))
                .CountAsync();

            totalPenalty += Math.Min(0.5m, recentCancellations * 0.1m);

            // Dispute penalties
            var recentDisputes = await _context.Projects
                .AsNoTracking()
                .Where(p => (p.ClientId == userId || p.ProviderId == userId) &&
                           p.Status == ProjectStatus.Disputed &&
                           p.CreatedAt >= DateTime.UtcNow.AddYears(-1))
                .CountAsync();

            totalPenalty += Math.Min(0.5m, recentDisputes * 0.2m);

            return Math.Min(MaxPenalty, totalPenalty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating penalties for user {UserId}", userId);
            return 0m;
        }
    }

    public decimal CalculateTimeDecayFactor(DateTime date)
    {
        var daysAgo = (DateTime.UtcNow - date).TotalDays;
        if (daysAgo <= 0) return 1.0m;
        if (daysAgo >= DaysForTimeDecay * 2) return 0.1m; // Minimum weight

        // Exponential decay: weight = e^(-days/365)
        var decayRate = (decimal)Math.Exp(-daysAgo / DaysForTimeDecay);
        return Math.Max(0.1m, decayRate);
    }

    public async Task<UserReputationScoreDto?> RecalculateAndSaveReputationScoreAsync(Guid userId)
    {
        // VULN-012 FIX: Add distributed lock per user to prevent lost updates
        // When multiple reviews are published simultaneously for the same user,
        // concurrent reputation calculations could cause lost writes (last-writer-wins)
        var lockKey = $"reputation:calc:{userId}";
        await using var lockHandle = await _distributedLockService.AcquireLockAsync(
            lockKey,
            TimeSpan.FromSeconds(30),   // Lock expiration
            TimeSpan.FromSeconds(10),    // Wait up to 10 seconds
            TimeSpan.FromMilliseconds(100)); // Retry every 100ms

        if (!lockHandle.IsAcquired)
        {
            _logger.LogWarning("Could not acquire lock for reputation calculation: user {UserId}. Another calculation may be in progress.", userId);
            throw new InvalidOperationException($"Reputation calculation already in progress for user {userId}. Please try again shortly.");
        }

        try
        {
            _logger.LogInformation("Acquired distributed lock for reputation calculation: user {UserId}", userId);

            var calculatedScore = await CalculateOverallReputationScoreAsync(userId);
            if (calculatedScore == null)
            {
                return null;
            }

            // Find existing record or create new one
            var existingScore = await _context.UserReputationScores
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (existingScore != null)
            {
                existingScore.OverallScore = calculatedScore.OverallScore;
                existingScore.ProjectCompletionRate = calculatedScore.ProjectCompletionRate;
                existingScore.AverageResponseTime = calculatedScore.AverageResponseTime;
                existingScore.TotalProjectsCompleted = calculatedScore.TotalProjectsCompleted;
                existingScore.LastUpdated = DateTime.UtcNow;
            }
            else
            {
                var newScore = new UserReputationScore
                {
                    UserId = userId,
                    OverallScore = calculatedScore.OverallScore,
                    ProjectCompletionRate = calculatedScore.ProjectCompletionRate,
                    AverageResponseTime = calculatedScore.AverageResponseTime,
                    TotalProjectsCompleted = calculatedScore.TotalProjectsCompleted,
                    LastUpdated = DateTime.UtcNow
                };
                _context.UserReputationScores.Add(newScore);
            }

            // Add history entry
            var historyEntry = new ReputationHistory
            {
                UserId = userId,
                Date = DateTime.UtcNow,
                Score = calculatedScore.OverallScore,
                ChangeReason = "Reputation score recalculated"
            };
            _context.ReputationHistories.Add(historyEntry);

            await _context.SaveChangesAsync();

            await _auditLogService.LogEventAsync(
                userId,
                "ReputationRecalculated",
                "System",
                null,
                true,
                System.Text.Json.JsonSerializer.Serialize(new { calculatedScore.OverallScore, calculatedScore.TotalProjectsCompleted }),
                null);

            _logger.LogInformation("Successfully recalculated reputation for user {UserId}. New score: {Score}", userId, calculatedScore.OverallScore);

            return calculatedScore;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating and saving reputation score for user {UserId}", userId);
            throw;
        }
    }

    public async Task<CategoryReputationScoreDto?> RecalculateAndSaveCategoryScoreAsync(Guid userId, Guid skillId)
    {
        try
        {
            var calculatedScore = await CalculateCategoryReputationScoreAsync(userId, skillId);
            if (calculatedScore == null)
            {
                return null;
            }

            var existingScore = await _context.CategoryReputationScores
                .FirstOrDefaultAsync(s => s.UserId == userId && s.SkillId == skillId);

            if (existingScore != null)
            {
                existingScore.Score = calculatedScore.Score;
                existingScore.ProjectCount = calculatedScore.ProjectCount;
                existingScore.LastProjectAt = calculatedScore.LastProjectAt;
            }
            else
            {
                var newScore = new CategoryReputationScore
                {
                    UserId = userId,
                    SkillId = skillId,
                    Score = calculatedScore.Score,
                    ProjectCount = calculatedScore.ProjectCount,
                    LastProjectAt = calculatedScore.LastProjectAt
                };
                _context.CategoryReputationScores.Add(newScore);
            }

            await _context.SaveChangesAsync();
            return calculatedScore;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating category score for user {UserId}, skill {SkillId}", userId, skillId);
            throw;
        }
    }

    public async Task<int> BulkRecalculateReputationScoresAsync()
    {
        try
        {
            // PERFORMANCE FIX: Use AsNoTracking for bulk reputation recalculation user list
            var userIds = await _context.Users
                .AsNoTracking()
                .Where(u => u.Status == UserStatus.TaxCompliant || u.Status == UserStatus.Active)
                .Select(u => u.Id)
                .ToListAsync();

            var processedCount = 0;
            foreach (var userId in userIds)
            {
                try
                {
                    await RecalculateAndSaveReputationScoreAsync(userId);
                    processedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to recalculate reputation for user {UserId}", userId);
                }
            }

            _logger.LogInformation("Bulk reputation recalculation completed. Processed {Count} users", processedCount);
            return processedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk reputation recalculation");
            throw;
        }
    }

    public async Task<List<UserReputationScoreDto>> UpdateReputationOnReviewPublishAsync(Guid reviewId)
    {
        try
        {
            var review = await _context.ProjectReviews
                .Include(r => r.Project)
                .FirstOrDefaultAsync(r => r.Id == reviewId);

            if (review == null)
            {
                return new List<UserReputationScoreDto>();
            }

            var updatedScores = new List<UserReputationScoreDto>();

            // Update reviewee's reputation
            var revieweeScore = await RecalculateAndSaveReputationScoreAsync(review.RevieweeId);
            if (revieweeScore != null)
            {
                updatedScores.Add(revieweeScore);
            }

            return updatedScores;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating reputation on review publish {ReviewId}", reviewId);
            throw;
        }
    }

    public async Task<List<UserReputationScoreDto>> UpdateReputationOnProjectCompletionAsync(Guid projectId)
    {
        try
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null)
            {
                return new List<UserReputationScoreDto>();
            }

            var updatedScores = new List<UserReputationScoreDto>();

            // Update both client and provider reputation
            var clientScore = await RecalculateAndSaveReputationScoreAsync(project.ClientId);
            if (clientScore != null)
            {
                updatedScores.Add(clientScore);
            }

            if (project.ProviderId.HasValue)
            {
                var providerScore = await RecalculateAndSaveReputationScoreAsync(project.ProviderId.Value);
                if (providerScore != null)
                {
                    updatedScores.Add(providerScore);
                }
            }

            return updatedScores;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating reputation on project completion {ProjectId}", projectId);
            throw;
        }
    }

    #region Private Helper Methods

    private async Task<decimal> CalculateReviewsScoreAsync(List<ProjectReview> reviews)
    {
        if (!reviews.Any())
        {
            return BaseScore;
        }

        var weightedScores = new List<(decimal score, decimal weight)>();

        foreach (var review in reviews)
        {
            var reviewScore = CalculateWeightedReviewScore(review);
            var timeWeight = CalculateTimeDecayFactor(review.PublishedAt ?? review.CreatedAt);
            weightedScores.Add((reviewScore, timeWeight));
        }

        var totalWeightedScore = weightedScores.Sum(ws => ws.score * ws.weight);
        var totalWeight = weightedScores.Sum(ws => ws.weight);

        return totalWeight > 0 ? totalWeightedScore / totalWeight : BaseScore;
    }

    private decimal CalculateWeightedReviewScore(ProjectReview review)
    {
        // Use dimensional ratings if available, otherwise fall back to overall rating
        if (review.QualityRating.HasValue && review.CommunicationRating.HasValue &&
            review.TimelinessRating.HasValue && review.ProfessionalismRating.HasValue)
        {
            var weightedScore = (review.QualityRating.Value * QualityWeight) +
                               (review.CommunicationRating.Value * CommunicationWeight) +
                               (review.TimelinessRating.Value * TimelinessWeight) +
                               (review.ProfessionalismRating.Value * ProfessionalismWeight);

            return Math.Min(5.0m, weightedScore / 2m); // Convert from 10-point to 5-point scale
        }

        return Math.Min(5.0m, review.OverallRating / 2m); // Convert from 10-point to 5-point scale
    }

    private decimal CalculateReviewAverage(ProjectReview review)
    {
        if (review.QualityRating.HasValue && review.CommunicationRating.HasValue &&
            review.TimelinessRating.HasValue && review.ProfessionalismRating.HasValue)
        {
            return (review.QualityRating.Value + review.CommunicationRating.Value +
                   review.TimelinessRating.Value + review.ProfessionalismRating.Value) / 4m;
        }

        return review.OverallRating;
    }

    private async Task<decimal> CalculateProjectCompletionRateAsync(Guid userId)
    {
        // PERFORMANCE FIX: Use AsNoTracking for read-only completion rate calculations
        var totalProjects = await _context.Projects
            .AsNoTracking()
            .Where(p => p.ClientId == userId || p.ProviderId == userId)
            .Where(p => p.Status == ProjectStatus.Completed || p.Status == ProjectStatus.Cancelled)
            .CountAsync();

        if (totalProjects == 0)
        {
            return 0m;
        }

        var completedProjects = await _context.Projects
            .AsNoTracking()
            .Where(p => p.ClientId == userId || p.ProviderId == userId)
            .Where(p => p.Status == ProjectStatus.Completed)
            .CountAsync();

        return (decimal)completedProjects / totalProjects;
    }

    private async Task<int> CalculateAverageResponseTimeAsync(Guid userId)
    {
        // Mock implementation - would need message/communication tracking
        // For now, return a reasonable default based on user activity
        // PERFORMANCE FIX: Use AsNoTracking for read-only count query
        var recentReviews = await _context.ProjectReviews
            .AsNoTracking()
            .Where(r => r.ReviewerId == userId && r.Status == ProjectReviewStatus.Published)
            .Where(r => r.PublishedAt >= DateTime.UtcNow.AddDays(-90))
            .CountAsync();

        // More active users tend to respond faster
        return recentReviews > 10 ? 12 : recentReviews > 5 ? 24 : 48;
    }

    private string GenerateScoreExplanation(decimal reviewsScore, decimal completionRate,
        decimal streakBonus, decimal penalties, decimal finalScore)
    {
        var explanation = $"Score based on {Math.Round(reviewsScore, 1)}/5.0 from reviews";

        if (completionRate > 0.8m)
        {
            explanation += $", excellent {completionRate:P0} completion rate";
        }
        else if (completionRate > 0.6m)
        {
            explanation += $", good {completionRate:P0} completion rate";
        }
        else if (completionRate > 0)
        {
            explanation += $", {completionRate:P0} completion rate needs improvement";
        }

        if (streakBonus > 0)
        {
            explanation += ", performance streak bonus applied";
        }

        if (penalties > 0.1m)
        {
            explanation += ", penalties applied for cancellations/disputes";
        }

        explanation += $". Final score: {finalScore:F1}/5.0";

        return explanation;
    }

    #endregion
}