using System.ComponentModel.DataAnnotations;
using SkillLedger.Core.Enums;

namespace SkillLedger.Core.Entities;

public class Experience
{
    public Experience()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Unique identifier for the experience
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Associated user ID (foreign key)
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Type of experience (Work, Education, Project, Volunteer, etc.)
    /// </summary>
    public ExperienceType Type { get; set; } = ExperienceType.Work;

    /// <summary>
    /// Job title, degree, project role, etc.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = null!;

    /// <summary>
    /// Company, school, organization, or project name
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Organization { get; set; } = null!;

    /// <summary>
    /// Location where this experience took place
    /// </summary>
    [MaxLength(100)]
    public string? Location { get; set; }

    /// <summary>
    /// Detailed description of the experience, responsibilities, achievements
    /// </summary>
    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>
    /// Start date of the experience
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date of the experience (null if current/ongoing)
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Whether this experience is currently ongoing
    /// </summary>
    public bool IsCurrent { get; set; } = false;

    /// <summary>
    /// Whether this experience is visible on the user's public profile
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Whether this experience is featured prominently on the user's profile
    /// </summary>
    public bool IsFeatured { get; set; } = false;

    /// <summary>
    /// Display order for this experience in lists
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// When the experience entry was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the experience entry was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the associated user
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Navigation property for skills used in this experience
    /// </summary>
    public virtual ICollection<ExperienceSkill> ExperienceSkills { get; set; } = new List<ExperienceSkill>();
}