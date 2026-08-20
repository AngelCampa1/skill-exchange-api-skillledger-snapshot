using System.ComponentModel.DataAnnotations;
using SkillLedger.Core.Enums;

namespace SkillLedger.Core.Entities;

/// <summary>
/// Represents a badge earned by a user
/// </summary>
public class UserBadge
{
    /// <summary>
    /// Unique identifier for the badge instance
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// ID of the user who earned the badge
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Type/identifier of the badge (e.g., "HIGH_PERFORMER", "VERIFIED_IDENTITY")
    /// </summary>
    [MaxLength(100)]
    public string BadgeType { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the badge
    /// </summary>
    [MaxLength(200)]
    public string BadgeName { get; set; } = string.Empty;

    /// <summary>
    /// Description of what the badge represents
    /// </summary>
    [MaxLength(500)]
    public string BadgeDescription { get; set; } = string.Empty;

    /// <summary>
    /// Category of the badge
    /// </summary>
    public BadgeCategory Category { get; set; }

    /// <summary>
    /// URL to the badge icon
    /// </summary>
    [MaxLength(500)]
    public string? IconUrl { get; set; }

    /// <summary>
    /// When the badge was earned
    /// </summary>
    public DateTime EarnedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the badge expires (null for permanent badges)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Whether the badge is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Level of verification required for this badge
    /// </summary>
    public VerificationLevel VerificationLevel { get; set; }

    /// <summary>
    /// JSON string containing evidence/proof for badge earning
    /// </summary>
    public string? VerificationEvidence { get; set; }

    /// <summary>
    /// User ID of who verified the badge (for manual verification)
    /// </summary>
    public Guid? VerifiedBy { get; set; }

    /// <summary>
    /// When the badge was verified
    /// </summary>
    public DateTime? VerifiedAt { get; set; }

    /// <summary>
    /// Cryptographic hash to ensure badge integrity
    /// </summary>
    [MaxLength(256)]
    public string? IntegrityHash { get; set; }

    /// <summary>
    /// Navigation property to the user who earned the badge
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Navigation property to the user who verified the badge
    /// </summary>
    public virtual User? VerifierUser { get; set; }
}