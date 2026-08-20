using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.Entities;

/// <summary>
/// Represents a request for manual badge verification
/// </summary>
public class VerificationRequest
{
    /// <summary>
    /// Unique identifier for the verification request
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// ID of the user requesting verification
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Type of badge being requested for verification
    /// </summary>
    [MaxLength(100)]
    public string BadgeType { get; set; } = string.Empty;

    /// <summary>
    /// When the verification was requested
    /// </summary>
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Current status of the verification request
    /// </summary>
    [MaxLength(50)]
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// JSON string containing evidence submitted by the user
    /// </summary>
    public string? SubmittedEvidence { get; set; }

    /// <summary>
    /// User ID of who reviewed the request
    /// </summary>
    public Guid? ReviewedBy { get; set; }

    /// <summary>
    /// When the request was reviewed
    /// </summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>
    /// Notes from the reviewer
    /// </summary>
    [MaxLength(2000)]
    public string? ReviewNotes { get; set; }

    /// <summary>
    /// Navigation property to the user requesting verification
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Navigation property to the user who reviewed the request
    /// </summary>
    public virtual User? ReviewerUser { get; set; }
}