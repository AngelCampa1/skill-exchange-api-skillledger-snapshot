using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.Entities;

public class SkillEndorsement
{
    public SkillEndorsement()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Unique identifier for the skill endorsement
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Associated user skill ID (foreign key) - the skill being endorsed
    /// </summary>
    public Guid UserSkillId { get; set; }

    /// <summary>
    /// User ID who gave the endorsement (foreign key)
    /// </summary>
    public Guid EndorsedByUserId { get; set; }

    /// <summary>
    /// Optional comment or note about the endorsement
    /// </summary>
    [MaxLength(500)]
    public string? Comment { get; set; }

    /// <summary>
    /// Review text for the endorsement (alias for Comment for backward compatibility)
    /// </summary>
    public string? ReviewText
    {
        get => Comment;
        set => Comment = value;
    }

    /// <summary>
    /// Whether this endorsement is visible to other users
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// When the endorsement was given
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the user skill being endorsed
    /// </summary>
    public virtual UserSkill UserSkill { get; set; } = null!;

    /// <summary>
    /// Navigation property to the user who gave the endorsement
    /// </summary>
    public virtual User EndorsedByUser { get; set; } = null!;
}