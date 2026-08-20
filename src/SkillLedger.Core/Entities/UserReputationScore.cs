using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.Entities;

/// <summary>
/// Entity representing a user's overall reputation score
/// </summary>
public class UserReputationScore
{
    /// <summary>
    /// Unique identifier for the reputation score record
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the user this score belongs to
    /// </summary>
    [Required(ErrorMessage = "UserId is required")]
    public Guid UserId { get; set; }

    /// <summary>
    /// Overall reputation score on 0-5 scale
    /// </summary>
    [Range(0, 5, ErrorMessage = "Overall score must be between 0 and 5")]
    public decimal OverallScore { get; set; }

    /// <summary>
    /// Project completion rate (0.00 to 1.00)
    /// </summary>
    [Range(0, 1, ErrorMessage = "Completion rate must be between 0 and 1")]
    public decimal ProjectCompletionRate { get; set; }

    /// <summary>
    /// Average response time in hours
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Response time cannot be negative")]
    public int AverageResponseTime { get; set; }

    /// <summary>
    /// Total number of projects completed
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Project count cannot be negative")]
    public int TotalProjectsCompleted { get; set; }

    /// <summary>
    /// When this score was last calculated and updated
    /// </summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// Navigation property to the user
    /// </summary>
    public virtual User User { get; set; } = null!;

    public UserReputationScore()
    {
        Id = Guid.NewGuid();
        OverallScore = 3.0m; // Default starting score
        ProjectCompletionRate = 0.0m;
        AverageResponseTime = 0;
        TotalProjectsCompleted = 0;
        LastUpdated = DateTime.UtcNow;
    }
}