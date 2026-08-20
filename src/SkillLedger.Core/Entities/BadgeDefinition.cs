using System.ComponentModel.DataAnnotations;
using SkillLedger.Core.Enums;

namespace SkillLedger.Core.Entities;

/// <summary>
/// Defines the template and requirements for a type of badge
/// </summary>
public class BadgeDefinition
{
    /// <summary>
    /// Unique identifier for the badge definition
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Type/identifier of the badge
    /// </summary>
    [MaxLength(100)]
    public string BadgeType { get; set; } = string.Empty;

    /// <summary>
    /// Category of the badge
    /// </summary>
    public BadgeCategory Category { get; set; }

    /// <summary>
    /// Display name of the badge
    /// </summary>
    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Description of what the badge represents
    /// </summary>
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// URL to the badge icon
    /// </summary>
    [MaxLength(500)]
    public string? IconUrl { get; set; }

    /// <summary>
    /// Level of verification required for this badge
    /// </summary>
    public VerificationLevel RequiredVerification { get; set; }

    /// <summary>
    /// How long the badge is valid for (null for permanent badges)
    /// </summary>
    public TimeSpan? ExpirationPeriod { get; set; }

    /// <summary>
    /// Whether this badge definition is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Priority order for badge display (lower numbers = higher priority)
    /// </summary>
    public int DisplayPriority { get; set; } = 0;

    /// <summary>
    /// When this badge definition was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this badge definition was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the criteria for earning this badge
    /// </summary>
    public virtual ICollection<BadgeCriteria> Criteria { get; set; } = new List<BadgeCriteria>();
}