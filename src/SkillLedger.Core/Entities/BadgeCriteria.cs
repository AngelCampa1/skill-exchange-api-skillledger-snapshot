using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.Entities;

/// <summary>
/// Defines criteria and rules for earning badges
/// </summary>
public class BadgeCriteria
{
    /// <summary>
    /// Unique identifier for the badge criteria
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Type/identifier of the badge this criteria applies to
    /// </summary>
    [MaxLength(100)]
    public string BadgeType { get; set; } = string.Empty;

    /// <summary>
    /// Name of the specific criteria
    /// </summary>
    [MaxLength(200)]
    public string CriteriaName { get; set; } = string.Empty;

    /// <summary>
    /// Value or threshold for the criteria
    /// </summary>
    [MaxLength(500)]
    public string CriteriaValue { get; set; } = string.Empty;

    /// <summary>
    /// JSON logic expression for complex criteria evaluation
    /// </summary>
    public string? CriteriaExpression { get; set; }

    /// <summary>
    /// Whether this criteria is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Priority order for evaluation (lower numbers = higher priority)
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// When this criteria was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this criteria was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}