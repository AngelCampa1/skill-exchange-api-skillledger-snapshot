using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.Entities;

/// <summary>
/// Represents a user's overall reputation scores across all categories
/// </summary>
public class UserReputationScores
{
    public UserReputationScores()
    {
        Id = Guid.NewGuid();
        UserId = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Unique identifier for this reputation record
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the user these scores belong to
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Overall weighted reputation score (0-10 scale)
    /// </summary>
    [Range(0, 10, ErrorMessage = "Overall score must be between 0 and 10")]
    public decimal OverallScore { get; set; } = 0.0m;

    /// <summary>
    /// Average score for work quality (0-10 scale)
    /// </summary>
    [Range(0, 10, ErrorMessage = "Quality score must be between 0 and 10")]
    public decimal QualityScore { get; set; } = 0.0m;

    /// <summary>
    /// Average score for communication effectiveness (0-10 scale)
    /// </summary>
    [Range(0, 10, ErrorMessage = "Communication score must be between 0 and 10")]
    public decimal CommunicationScore { get; set; } = 0.0m;

    /// <summary>
    /// Average score for meeting deadlines and timeliness (0-10 scale)
    /// </summary>
    [Range(0, 10, ErrorMessage = "Timeliness score must be between 0 and 10")]
    public decimal TimelinessScore { get; set; } = 0.0m;

    /// <summary>
    /// Average score for professionalism and conduct (0-10 scale)
    /// </summary>
    [Range(0, 10, ErrorMessage = "Professionalism score must be between 0 and 10")]
    public decimal ProfessionalismScore { get; set; } = 0.0m;

    /// <summary>
    /// Total number of reviews received by this user
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Total reviews received cannot be negative")]
    public int TotalReviewsReceived { get; set; } = 0;

    /// <summary>
    /// Total number of projects completed successfully
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Completed projects count cannot be negative")]
    public int CompletedProjectsCount { get; set; } = 0;

    /// <summary>
    /// Project completion rate (0.0 to 1.0)
    /// </summary>
    [Range(0.0, 1.0, ErrorMessage = "Completion rate must be between 0.0 and 1.0")]
    public decimal CompletionRate { get; set; } = 0.0m;

    /// <summary>
    /// Average response time in hours
    /// </summary>
    [Range(0.0, double.MaxValue, ErrorMessage = "Response time cannot be negative")]
    public decimal ResponseTimeHours { get; set; } = 0.0m;

    /// <summary>
    /// Current consecutive streak of successful projects
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Current streak cannot be negative")]
    public int CurrentStreak { get; set; } = 0;

    /// <summary>
    /// Maximum consecutive streak achieved
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Max streak cannot be negative")]
    public int MaxStreak { get; set; } = 0;

    /// <summary>
    /// When this reputation record was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this reputation record was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Navigation property to the user
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Navigation property for category-specific reputation scores
    /// </summary>
    public virtual ICollection<CategoryReputationScores> CategoryScores { get; set; } = new List<CategoryReputationScores>();

    // Helper properties and methods

    /// <summary>
    /// Indicates if this is a new user with no completed projects
    /// </summary>
    public bool IsNewUser => CompletedProjectsCount == 0;

    /// <summary>
    /// Calculates the weighted overall score from component scores
    /// Quality: 35%, Communication: 25%, Timeliness: 25%, Professionalism: 15%
    /// </summary>
    public decimal CalculateWeightedScore()
    {
        const decimal qualityWeight = 0.35m;
        const decimal communicationWeight = 0.25m;
        const decimal timelinessWeight = 0.25m;
        const decimal professionalismWeight = 0.15m;

        return (QualityScore * qualityWeight) +
               (CommunicationScore * communicationWeight) +
               (TimelinessScore * timelinessWeight) +
               (ProfessionalismScore * professionalismWeight);
    }

    /// <summary>
    /// Updates the timestamp to current UTC time
    /// </summary>
    public void UpdateTimestamp()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}