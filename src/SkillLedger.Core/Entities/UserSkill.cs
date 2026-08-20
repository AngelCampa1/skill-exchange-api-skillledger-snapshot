using System.ComponentModel.DataAnnotations;
using SkillLedger.Core.Enums;

namespace SkillLedger.Core.Entities;

public class UserSkill
{
    public UserSkill()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Unique identifier for the user skill
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Associated user ID (foreign key)
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Associated skill ID (foreign key)
    /// </summary>
    public Guid SkillId { get; set; }

    /// <summary>
    /// User's proficiency level with this skill
    /// </summary>
    public SkillProficiency Proficiency { get; set; } = SkillProficiency.Beginner;

    /// <summary>
    /// Years of experience with this skill
    /// </summary>
    public int YearsOfExperience { get; set; } = 0;

    /// <summary>
    /// User's self-assessment notes about their experience with this skill
    /// </summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Whether this skill is featured prominently on the user's profile
    /// </summary>
    public bool IsFeatured { get; set; } = false;

    /// <summary>
    /// Whether this skill is visible to other users
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// When the user added this skill
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the user last updated this skill
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the associated user
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Navigation property to the associated skill
    /// </summary>
    public virtual Skill Skill { get; set; } = null!;

    /// <summary>
    /// Navigation property for endorsements received for this user skill
    /// </summary>
    public virtual ICollection<SkillEndorsement> Endorsements { get; set; } = new List<SkillEndorsement>();
}