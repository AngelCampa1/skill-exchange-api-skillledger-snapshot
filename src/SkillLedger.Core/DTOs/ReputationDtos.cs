using System.ComponentModel.DataAnnotations;
using SkillLedger.Core.Enums;

namespace SkillLedger.Core.DTOs;

/// <summary>
/// DTO for user's overall reputation score with comprehensive metrics
/// </summary>
public class UserReputationScoreDto
{
    /// <summary>
    /// User's unique identifier
    /// </summary>
    [Required(ErrorMessage = "UserId is required")]
    public Guid UserId { get; set; }

    /// <summary>
    /// Overall reputation score on 0-5 scale
    /// </summary>
    [Range(0, 5, ErrorMessage = "Overall score must be between 0 and 5")]
    public decimal OverallScore { get; set; }

    /// <summary>
    /// Project completion rate as percentage (0-100)
    /// </summary>
    [Range(0, 100, ErrorMessage = "Completion rate must be between 0 and 100")]
    public decimal ProjectCompletionRate { get; set; }

    /// <summary>
    /// Average response time in hours
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Response time cannot be negative")]
    public int AverageResponseTime { get; set; }

    /// <summary>
    /// Total number of projects completed successfully
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Project count cannot be negative")]
    public int TotalProjectsCompleted { get; set; }

    /// <summary>
    /// Performance streak bonus points
    /// </summary>
    [Range(0, 1000, ErrorMessage = "Streak bonus must be between 0 and 1000")]
    public decimal PerformanceStreakBonus { get; set; }

    /// <summary>
    /// Total penalty points from cancellations and disputes
    /// </summary>
    [Range(0, 1000, ErrorMessage = "Total penalties must be between 0 and 1000")]
    public decimal TotalPenalties { get; set; }

    /// <summary>
    /// When the score was last calculated
    /// </summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// Number of active disputes
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Active disputes cannot be negative")]
    public int ActiveDisputes { get; set; }

    /// <summary>
    /// Average quality rating from completed projects
    /// </summary>
    [Range(0, 10, ErrorMessage = "Quality rating must be between 0 and 10")]
    public decimal AverageQualityRating { get; set; }

    /// <summary>
    /// Average communication rating from completed projects
    /// </summary>
    [Range(0, 10, ErrorMessage = "Communication rating must be between 0 and 10")]
    public decimal AverageCommunicationRating { get; set; }

    /// <summary>
    /// Average timeliness rating from completed projects
    /// </summary>
    [Range(0, 10, ErrorMessage = "Timeliness rating must be between 0 and 10")]
    public decimal AverageTimelinessRating { get; set; }

    /// <summary>
    /// Average professionalism rating from completed projects
    /// </summary>
    [Range(0, 10, ErrorMessage = "Professionalism rating must be between 0 and 10")]
    public decimal AverageProfessionalismRating { get; set; }
}

/// <summary>
/// DTO for category-specific reputation scores
/// </summary>
public class CategoryReputationScoreDto
{
    /// <summary>
    /// User's unique identifier
    /// </summary>
    [Required(ErrorMessage = "UserId is required")]
    public Guid UserId { get; set; }

    /// <summary>
    /// Skill category identifier
    /// </summary>
    [Required(ErrorMessage = "SkillId is required")]
    public Guid SkillId { get; set; }

    /// <summary>
    /// Skill category name
    /// </summary>
    [Required(ErrorMessage = "Skill name is required")]
    [StringLength(200, ErrorMessage = "Skill name cannot exceed 200 characters")]
    public string SkillName { get; set; } = null!;

    /// <summary>
    /// Category reputation score on 0-5 scale
    /// </summary>
    [Range(0, 5, ErrorMessage = "Score must be between 0 and 5")]
    public decimal Score { get; set; }

    /// <summary>
    /// Number of projects completed in this category
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Project count cannot be negative")]
    public int ProjectCount { get; set; }

    /// <summary>
    /// Date of the most recent project in this category
    /// </summary>
    public DateTime? LastProjectAt { get; set; }

    /// <summary>
    /// Average rating for this category from all completed projects
    /// </summary>
    [Range(0, 10, ErrorMessage = "Average rating must be between 0 and 10")]
    public decimal AverageRating { get; set; }

    /// <summary>
    /// Time decay factor applied to this category score
    /// </summary>
    [Range(0, 1, ErrorMessage = "Time decay factor must be between 0 and 1")]
    public decimal TimeDecayFactor { get; set; }
}

/// <summary>
/// DTO for detailed reputation breakdown showing calculation components
/// </summary>
public class ReputationBreakdownDto
{
    /// <summary>
    /// User's unique identifier
    /// </summary>
    [Required(ErrorMessage = "UserId is required")]
    public Guid UserId { get; set; }

    /// <summary>
    /// Final calculated reputation score
    /// </summary>
    [Range(0, 5, ErrorMessage = "Final score must be between 0 and 5")]
    public decimal FinalScore { get; set; }

    /// <summary>
    /// Base score from weighted ratings
    /// </summary>
    [Range(0, 5, ErrorMessage = "Base score must be between 0 and 5")]
    public decimal BaseScore { get; set; }

    /// <summary>
    /// Performance streak bonus applied
    /// </summary>
    [Range(0, 1000, ErrorMessage = "Streak bonus must be between 0 and 1000")]
    public decimal StreakBonus { get; set; }

    /// <summary>
    /// Total penalties applied
    /// </summary>
    [Range(0, 1000, ErrorMessage = "Penalties must be between 0 and 1000")]
    public decimal Penalties { get; set; }

    /// <summary>
    /// Time decay factor applied to recent activity
    /// </summary>
    [Range(0, 1, ErrorMessage = "Time decay factor must be between 0 and 1")]
    public decimal TimeDecayFactor { get; set; }

    /// <summary>
    /// Breakdown of individual rating components
    /// </summary>
    [Required(ErrorMessage = "Components breakdown is required")]
    public ReputationComponentsDto Components { get; set; } = null!;

    /// <summary>
    /// Human-readable explanation of how the score was calculated
    /// </summary>
    [Required(ErrorMessage = "Explanation is required")]
    [StringLength(2000, ErrorMessage = "Explanation cannot exceed 2000 characters")]
    public string Explanation { get; set; } = null!;

    /// <summary>
    /// When this breakdown was calculated
    /// </summary>
    public DateTime CalculatedAt { get; set; }
}

/// <summary>
/// DTO for individual reputation components with weights
/// </summary>
public class ReputationComponentsDto
{
    /// <summary>
    /// Quality rating component (weighted 40%)
    /// </summary>
    [Range(0, 10, ErrorMessage = "Quality rating must be between 0 and 10")]
    public decimal QualityRating { get; set; }

    /// <summary>
    /// Communication rating component (weighted 20%)
    /// </summary>
    [Range(0, 10, ErrorMessage = "Communication rating must be between 0 and 10")]
    public decimal CommunicationRating { get; set; }

    /// <summary>
    /// Timeliness rating component (weighted 20%)
    /// </summary>
    [Range(0, 10, ErrorMessage = "Timeliness rating must be between 0 and 10")]
    public decimal TimelinessRating { get; set; }

    /// <summary>
    /// Professionalism rating component (weighted 20%)
    /// </summary>
    [Range(0, 10, ErrorMessage = "Professionalism rating must be between 0 and 10")]
    public decimal ProfessionalismRating { get; set; }

    /// <summary>
    /// Weighted contributions of each component to final score
    /// </summary>
    [Required(ErrorMessage = "Weight contributions are required")]
    public ReputationWeightsDto WeightedContributions { get; set; } = null!;
}

/// <summary>
/// DTO for weighted contributions of each reputation component
/// </summary>
public class ReputationWeightsDto
{
    /// <summary>
    /// Quality component contribution (rating * 0.4)
    /// </summary>
    [Range(0, 4, ErrorMessage = "Quality contribution must be between 0 and 4")]
    public decimal QualityContribution { get; set; }

    /// <summary>
    /// Communication component contribution (rating * 0.2)
    /// </summary>
    [Range(0, 2, ErrorMessage = "Communication contribution must be between 0 and 2")]
    public decimal CommunicationContribution { get; set; }

    /// <summary>
    /// Timeliness component contribution (rating * 0.2)
    /// </summary>
    [Range(0, 2, ErrorMessage = "Timeliness contribution must be between 0 and 2")]
    public decimal TimelinessContribution { get; set; }

    /// <summary>
    /// Professionalism component contribution (rating * 0.2)
    /// </summary>
    [Range(0, 2, ErrorMessage = "Professionalism contribution must be between 0 and 2")]
    public decimal ProfessionalismContribution { get; set; }
}

/// <summary>
/// DTO for historical reputation data points
/// </summary>
public class ReputationHistoryDto
{
    /// <summary>
    /// User's unique identifier
    /// </summary>
    [Required(ErrorMessage = "UserId is required")]
    public Guid UserId { get; set; }

    /// <summary>
    /// Date of this reputation data point
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Reputation score at this point in time
    /// </summary>
    [Range(0, 5, ErrorMessage = "Score must be between 0 and 5")]
    public decimal Score { get; set; }

    /// <summary>
    /// Number of projects completed by this date
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Project count cannot be negative")]
    public int ProjectsCompleted { get; set; }

    /// <summary>
    /// Type of event that triggered this history entry
    /// </summary>
    [Required(ErrorMessage = "Event type is required")]
    [StringLength(100, ErrorMessage = "Event type cannot exceed 100 characters")]
    public string EventType { get; set; } = null!;

    /// <summary>
    /// Description of what changed
    /// </summary>
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// Score change from previous entry
    /// </summary>
    public decimal ScoreChange { get; set; }

    /// <summary>
    /// Reason for the reputation change
    /// </summary>
    [StringLength(200, ErrorMessage = "Change reason cannot exceed 200 characters")]
    public string? ChangeReason { get; set; }

    /// <summary>
    /// Associated project ID if change was project-related
    /// </summary>
    public Guid? ProjectId { get; set; }

    /// <summary>
    /// Associated review ID if change was review-related
    /// </summary>
    public Guid? ReviewId { get; set; }
}

/// <summary>
/// DTO for reputation trend analysis
/// </summary>
public class ReputationTrendDto
{
    /// <summary>
    /// User's unique identifier
    /// </summary>
    [Required(ErrorMessage = "UserId is required")]
    public Guid UserId { get; set; }

    /// <summary>
    /// Period analyzed in days
    /// </summary>
    [Range(1, 365, ErrorMessage = "Period must be between 1 and 365 days")]
    public int PeriodDays { get; set; }

    /// <summary>
    /// Overall trend direction
    /// </summary>
    [Required(ErrorMessage = "Trend direction is required")]
    public ReputationTrend TrendDirection { get; set; }

    /// <summary>
    /// Average score change per day
    /// </summary>
    public decimal AverageChangePerDay { get; set; }

    /// <summary>
    /// Total score change over the period
    /// </summary>
    public decimal TotalChange { get; set; }

    /// <summary>
    /// Score at the start of the period
    /// </summary>
    [Range(0, 5, ErrorMessage = "Starting score must be between 0 and 5")]
    public decimal StartingScore { get; set; }

    /// <summary>
    /// Current score
    /// </summary>
    [Range(0, 5, ErrorMessage = "Current score must be between 0 and 5")]
    public decimal CurrentScore { get; set; }

    /// <summary>
    /// Number of projects completed during this period
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Projects in period cannot be negative")]
    public int ProjectsInPeriod { get; set; }

    /// <summary>
    /// Peak score achieved during the period
    /// </summary>
    [Range(0, 5, ErrorMessage = "Peak score must be between 0 and 5")]
    public decimal PeakScore { get; set; }

    /// <summary>
    /// Lowest score during the period
    /// </summary>
    [Range(0, 5, ErrorMessage = "Lowest score must be between 0 and 5")]
    public decimal LowestScore { get; set; }

    /// <summary>
    /// Trend analysis summary
    /// </summary>
    [Required(ErrorMessage = "Summary is required")]
    [StringLength(1000, ErrorMessage = "Summary cannot exceed 1000 characters")]
    public string Summary { get; set; } = null!;

    /// <summary>
    /// When this trend analysis was calculated
    /// </summary>
    public DateTime CalculatedAt { get; set; }

    /// <summary>
    /// Previous score at the start of the period for comparison
    /// </summary>
    [Range(0, 5, ErrorMessage = "Previous score must be between 0 and 5")]
    public decimal PreviousScore { get; set; }

    /// <summary>
    /// Trend direction as an enumeration
    /// </summary>
    public ReputationTrend Trend { get; set; }

    /// <summary>
    /// Percentage change over the period
    /// </summary>
    public decimal TrendPercentage { get; set; }

    /// <summary>
    /// Number of days user was active during the period
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Days active cannot be negative")]
    public int DaysActive { get; set; }

    /// <summary>
    /// Total number of reviews received (all time)
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Total reviews cannot be negative")]
    public int TotalReviews { get; set; }

    /// <summary>
    /// Number of reviews received during this period
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Recent reviews cannot be negative")]
    public int RecentReviews { get; set; }
}

