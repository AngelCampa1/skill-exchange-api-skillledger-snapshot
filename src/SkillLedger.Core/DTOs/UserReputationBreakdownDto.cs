namespace SkillLedger.Core.DTOs;

/// <summary>
/// Detailed breakdown of user reputation scores with explanations and trends
/// </summary>
public class UserReputationBreakdownDto
{
    /// <summary>
    /// User ID this breakdown belongs to
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Overall weighted reputation score
    /// </summary>
    public decimal OverallScore { get; set; }

    /// <summary>
    /// Component scores with their weights and values
    /// </summary>
    public Dictionary<string, decimal> ComponentScores { get; set; } = new();

    /// <summary>
    /// Component weights used in calculation
    /// </summary>
    public Dictionary<string, decimal> ComponentWeights { get; set; } = new();

    /// <summary>
    /// Recent trend data (last 6 months)
    /// </summary>
    public List<ReputationTrendDataDto> RecentTrend { get; set; } = new();

    /// <summary>
    /// Performance streaks and bonuses
    /// </summary>
    public StreakInfoDto StreakInfo { get; set; } = new();

    /// <summary>
    /// Project completion statistics
    /// </summary>
    public CompletionStatsDto CompletionStats { get; set; } = new();

    /// <summary>
    /// Category-specific expertise areas
    /// </summary>
    public List<CategoryExpertiseDto> CategoryExpertise { get; set; } = new();

    /// <summary>
    /// Factors influencing the current score
    /// </summary>
    public List<ScoreFactorDto> ScoreFactors { get; set; } = new();

    /// <summary>
    /// Recommendations for improving reputation
    /// </summary>
    public List<string> ImprovementRecommendations { get; set; } = new();
}

/// <summary>
/// Reputation trend data point for charts
/// </summary>
public class ReputationTrendDataDto
{
    public DateTime Date { get; set; }
    public decimal Score { get; set; }
    public int ReviewCount { get; set; }
    public string Period { get; set; } = null!;
}

/// <summary>
/// Performance streak information
/// </summary>
public class StreakInfoDto
{
    public int CurrentStreak { get; set; }
    public int MaxStreak { get; set; }
    public decimal StreakBonus { get; set; }
    public DateTime? LastSuccessfulProject { get; set; }
}

/// <summary>
/// Project completion statistics
/// </summary>
public class CompletionStatsDto
{
    public int TotalProjects { get; set; }
    public int CompletedProjects { get; set; }
    public decimal CompletionRate { get; set; }
    public decimal AverageResponseTimeHours { get; set; }
    public int OnTimeDeliveries { get; set; }
}

/// <summary>
/// Category-specific expertise information
/// </summary>
public class CategoryExpertiseDto
{
    public Guid SkillId { get; set; }
    public string SkillName { get; set; } = null!;
    public decimal AverageScore { get; set; }
    public int ProjectCount { get; set; }
    public int ReviewCount { get; set; }
    public bool IsExpert { get; set; }
    public string ExpertiseLevel { get; set; } = null!;
}

/// <summary>
/// Factor influencing reputation score
/// </summary>
public class ScoreFactorDto
{
    public string Factor { get; set; } = null!;
    public decimal Impact { get; set; }
    public string Description { get; set; } = null!;
    public bool IsPositive { get; set; }
}