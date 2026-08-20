using SkillLedger.Core.Enums;

namespace SkillLedger.Core.DTOs;

/// <summary>
/// DTO for tracking user progress towards a badge
/// </summary>
public class BadgeProgressDto
{
    /// <summary>
    /// Badge type identifier
    /// </summary>
    public string BadgeType { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the badge
    /// </summary>
    public string BadgeName { get; set; } = string.Empty;

    /// <summary>
    /// Description of the badge
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Category of the badge
    /// </summary>
    public BadgeCategory Category { get; set; }

    /// <summary>
    /// URL to the badge icon
    /// </summary>
    public string? IconUrl { get; set; }

    /// <summary>
    /// Overall progress percentage (0-100)
    /// </summary>
    public decimal ProgressPercentage { get; set; }

    /// <summary>
    /// Whether the user is eligible to earn this badge
    /// </summary>
    public bool IsEligible { get; set; }

    /// <summary>
    /// Individual requirement progress
    /// </summary>
    public List<BadgeRequirementProgressDto> Requirements { get; set; } = new();

    /// <summary>
    /// Estimated time to earning (if applicable)
    /// </summary>
    public TimeSpan? EstimatedTimeToEarning { get; set; }
}

/// <summary>
/// DTO for individual badge requirement progress
/// </summary>
public class BadgeRequirementProgressDto
{
    /// <summary>
    /// Name of the requirement
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what is required
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Current value/progress
    /// </summary>
    public decimal Current { get; set; }

    /// <summary>
    /// Required value/threshold
    /// </summary>
    public decimal Required { get; set; }

    /// <summary>
    /// Unit of measurement (e.g., "projects", "rating", "days")
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// Whether this requirement is met
    /// </summary>
    public bool IsMet { get; set; }

    /// <summary>
    /// Progress percentage for this requirement (0-100)
    /// </summary>
    public decimal ProgressPercentage { get; set; }
}