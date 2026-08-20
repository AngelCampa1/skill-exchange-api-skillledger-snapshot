using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.Entities;

/// <summary>
/// Entity tracking historical changes to a user's reputation score
/// </summary>
public class ReputationHistory
{
    /// <summary>
    /// Unique identifier for the history record
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the user whose reputation changed
    /// </summary>
    [Required(ErrorMessage = "UserId is required")]
    public Guid UserId { get; set; }

    /// <summary>
    /// Date when the reputation change occurred
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Reputation score at this point in time
    /// </summary>
    [Range(0, 5, ErrorMessage = "Score must be between 0 and 5")]
    public decimal Score { get; set; }

    /// <summary>
    /// Description of what caused the change
    /// </summary>
    [Required(ErrorMessage = "ChangeReason is required")]
    [MaxLength(500, ErrorMessage = "Change reason cannot exceed 500 characters")]
    public string ChangeReason { get; set; } = null!;

    /// <summary>
    /// Reference to related project (if applicable)
    /// </summary>
    public Guid? ProjectId { get; set; }

    /// <summary>
    /// Reference to related review (if applicable)
    /// </summary>
    public Guid? ReviewId { get; set; }

    /// <summary>
    /// Navigation property to the user
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Navigation property to the related project
    /// </summary>
    public virtual Project? Project { get; set; }

    /// <summary>
    /// Navigation property to the related review
    /// </summary>
    public virtual ProjectReview? Review { get; set; }

    public ReputationHistory()
    {
        Id = Guid.NewGuid();
        Date = DateTime.UtcNow;
    }
}