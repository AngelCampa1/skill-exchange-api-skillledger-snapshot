using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.Entities;

public class Skill
{
    public Skill()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Unique identifier for the skill
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Name of the skill
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Optional description of what this skill entails
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Category this skill belongs to (e.g., "Programming", "Design", "Marketing")
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = null!;

    /// <summary>
    /// Whether this skill is pre-approved and managed by the system
    /// </summary>
    public bool IsSystemManaged { get; set; } = false;

    /// <summary>
    /// Whether this skill is active and available for use
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the skill was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the skill was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property for user skills
    /// </summary>
    public virtual ICollection<UserSkill> UserSkills { get; set; } = new List<UserSkill>();

    /// <summary>
    /// Navigation property for skill endorsements
    /// </summary>
    public virtual ICollection<SkillEndorsement> SkillEndorsements { get; set; } = new List<SkillEndorsement>();

    /// <summary>
    /// Navigation property for projects that require this skill
    /// </summary>
    public virtual ICollection<ProjectSkill> ProjectSkills { get; set; } = new List<ProjectSkill>();
}