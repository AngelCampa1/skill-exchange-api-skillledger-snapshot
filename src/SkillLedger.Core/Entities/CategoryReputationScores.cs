using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.Entities;

/// <summary>
/// Represents a user's reputation scores within a specific skill category
/// </summary>
public class CategoryReputationScores
{
    public CategoryReputationScores()
    {
        Id = Guid.NewGuid();
        UserReputationScoresId = Guid.NewGuid();
        SkillId = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Unique identifier for this category reputation record
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the parent user reputation scores record
    /// </summary>
    [Required]
    public Guid UserReputationScoresId { get; set; }

    /// <summary>
    /// Reference to the skill category this score represents
    /// </summary>
    [Required]
    public Guid SkillId { get; set; }

    /// <summary>
    /// Average reputation score for this skill category (0-10 scale)
    /// </summary>
    [Range(0, 10, ErrorMessage = "Average score must be between 0 and 10")]
    public decimal AverageScore { get; set; } = 0.0m;

    /// <summary>
    /// Number of projects completed in this skill category
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Project count cannot be negative")]
    public int ProjectCount { get; set; } = 0;

    /// <summary>
    /// Number of reviews received for this skill category
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Review count cannot be negative")]
    public int ReviewCount { get; set; } = 0;

    /// <summary>
    /// When this category reputation record was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this category reputation record was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Navigation property to the parent user reputation scores
    /// </summary>
    public virtual UserReputationScores UserReputationScores { get; set; } = null!;

    /// <summary>
    /// Navigation property to the skill category
    /// </summary>
    public virtual Skill Skill { get; set; } = null!;

    // Helper properties and methods

    /// <summary>
    /// Indicates if this category has enough data for reliable scoring (minimum 3 reviews)
    /// </summary>
    public bool HasEnoughData => ReviewCount >= 3;

    /// <summary>
    /// Indicates if the user is considered an expert in this category (8+ average score with 5+ projects)
    /// </summary>
    public bool IsExpert => AverageScore >= 8.0m && ProjectCount >= 5;

    /// <summary>
    /// Updates the timestamp to current UTC time
    /// </summary>
    public void UpdateTimestamp()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}