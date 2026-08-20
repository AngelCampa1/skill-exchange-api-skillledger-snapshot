using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.Entities;

/// <summary>
/// Entity representing a user's reputation score for a specific skill category
/// </summary>
public class CategoryReputationScore
{
    /// <summary>
    /// Unique identifier for the category reputation score record
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the user this score belongs to
    /// </summary>
    [Required(ErrorMessage = "UserId is required")]
    public Guid UserId { get; set; }

    /// <summary>
    /// Reference to the skill
    /// </summary>
    [Required(ErrorMessage = "SkillId is required")]
    public Guid SkillId { get; set; }

    /// <summary>
    /// Reputation score for this category on 0-5 scale
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
    /// Navigation property to the user
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Navigation property to the skill
    /// </summary>
    public virtual Skill Skill { get; set; } = null!;

    public CategoryReputationScore()
    {
        Id = Guid.NewGuid();
        Score = 3.0m; // Default starting score
        ProjectCount = 0;
    }
}