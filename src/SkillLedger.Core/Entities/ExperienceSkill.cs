using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.Entities;

public class ExperienceSkill
{
    public ExperienceSkill()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Unique identifier for the experience skill
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Associated experience ID (foreign key)
    /// </summary>
    public Guid ExperienceId { get; set; }

    /// <summary>
    /// Associated skill ID (foreign key)
    /// </summary>
    public Guid SkillId { get; set; }

    /// <summary>
    /// Optional notes about how this skill was used in this experience
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }

    /// <summary>
    /// When this skill was added to the experience
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the associated experience
    /// </summary>
    public virtual Experience Experience { get; set; } = null!;

    /// <summary>
    /// Navigation property to the associated skill
    /// </summary>
    public virtual Skill Skill { get; set; } = null!;
}