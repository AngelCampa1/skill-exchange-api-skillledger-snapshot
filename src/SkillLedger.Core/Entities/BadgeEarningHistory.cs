using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.Entities;

/// <summary>
/// Tracks the history of badge earning, revocation, and other actions
/// </summary>
public class BadgeEarningHistory
{
    /// <summary>
    /// Unique identifier for the history entry
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// ID of the user this action applies to
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// ID of the badge this action applies to
    /// </summary>
    public Guid BadgeId { get; set; }

    /// <summary>
    /// Type of action performed (Earned, Revoked, Expired, Renewed)
    /// </summary>
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Reason for the action
    /// </summary>
    [MaxLength(500)]
    public string? Reason { get; set; }

    /// <summary>
    /// JSON string containing supporting evidence/data
    /// </summary>
    public string? Evidence { get; set; }

    /// <summary>
    /// User ID of who performed the action
    /// </summary>
    public Guid? ActionBy { get; set; }

    /// <summary>
    /// When the action was performed
    /// </summary>
    public DateTime ActionAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the user this action applies to
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Navigation property to the badge this action applies to
    /// </summary>
    public virtual UserBadge Badge { get; set; } = null!;

    /// <summary>
    /// Navigation property to the user who performed the action
    /// </summary>
    public virtual User? ActionByUser { get; set; }
}