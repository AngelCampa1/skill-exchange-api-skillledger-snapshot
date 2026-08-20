using System.ComponentModel.DataAnnotations;
using SkillLedger.Core.Enums;

namespace SkillLedger.Core.Entities;

public class ProjectSkill
{
    /// <summary>
    /// Reference to the project
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Reference to the skill
    /// </summary>
    public Guid SkillId { get; set; }

    /// <summary>
    /// Required proficiency level for this skill (1-5 scale)
    /// </summary>
    [Range(1, 5)]
    public SkillProficiency ProficiencyRequired { get; set; }

    /// <summary>
    /// Optional weight/importance of this skill for the project (1-5 scale)
    /// Higher weight means more important for project success
    /// </summary>
    [Range(1, 5)]
    public int Weight { get; set; } = 3;

    /// <summary>
    /// When this skill requirement was added to the project
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the project
    /// </summary>
    public virtual Project Project { get; set; } = null!;

    /// <summary>
    /// Navigation property to the skill
    /// </summary>
    public virtual Skill Skill { get; set; } = null!;
}