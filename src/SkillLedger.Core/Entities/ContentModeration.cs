using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.Entities;

/// <summary>
/// Log of content moderation activities
/// </summary>
public class ContentModerationLog
{
    public ContentModerationLog()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User who created the content
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Type of content moderated
    /// </summary>
    public int ContentType { get; set; }

    /// <summary>
    /// Whether content was approved
    /// </summary>
    public bool WasApproved { get; set; }

    /// <summary>
    /// Risk level assessed
    /// </summary>
    public int RiskLevel { get; set; }

    /// <summary>
    /// Whether human review was required
    /// </summary>
    public bool RequiredHumanReview { get; set; }

    /// <summary>
    /// Categories that were flagged (JSON array)
    /// </summary>
    public string? FlaggedCategories { get; set; }

    /// <summary>
    /// Moderation scores (JSON object)
    /// </summary>
    public string? ModerationScores { get; set; }

    /// <summary>
    /// Terms that were blocked (JSON array)
    /// </summary>
    public string? BlockedTerms { get; set; }

    /// <summary>
    /// Reason for rejection (if applicable)
    /// </summary>
    [MaxLength(500)]
    public string? ReasonForRejection { get; set; }

    /// <summary>
    /// Analysis ID from moderation service
    /// </summary>
    [MaxLength(100)]
    public string? AnalysisId { get; set; }

    /// <summary>
    /// When moderation was performed
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to user
    /// </summary>
    public virtual User User { get; set; } = null!;
}

/// <summary>
/// Custom blocklist terms for organization
/// </summary>
public class CustomBlocklistTerm
{
    public CustomBlocklistTerm()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Blocked term or phrase
    /// </summary>
    [MaxLength(200)]
    public string Term { get; set; } = string.Empty;

    /// <summary>
    /// User who added this term
    /// </summary>
    public Guid AddedByUserId { get; set; }

    /// <summary>
    /// Whether term is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When term was added
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When term expires (optional)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Navigation property to user who added term
    /// </summary>
    public virtual User AddedByUser { get; set; } = null!;
}

/// <summary>
/// Content pending human review
/// </summary>
public class ContentReviewQueue
{
    public ContentReviewQueue()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User who created the content
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Type of content
    /// </summary>
    public int ContentType { get; set; }

    /// <summary>
    /// Content text (if applicable)
    /// </summary>
    public string? ContentText { get; set; }

    /// <summary>
    /// Content URL (if applicable - image, document, etc.)
    /// </summary>
    [MaxLength(500)]
    public string? ContentUrl { get; set; }

    /// <summary>
    /// Initial moderation result (JSON)
    /// </summary>
    public string? ModerationResult { get; set; }

    /// <summary>
    /// Priority for review (1-5, 5 = highest)
    /// </summary>
    public int ReviewPriority { get; set; } = 3;

    /// <summary>
    /// Status of review
    /// </summary>
    public ReviewStatus Status { get; set; } = ReviewStatus.Pending;

    /// <summary>
    /// User assigned to review
    /// </summary>
    public Guid? AssignedReviewerId { get; set; }

    /// <summary>
    /// Final decision
    /// </summary>
    public ReviewDecision? Decision { get; set; }

    /// <summary>
    /// Reviewer's comments
    /// </summary>
    public string? ReviewComments { get; set; }

    /// <summary>
    /// When content was submitted for review
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When review was completed
    /// </summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>
    /// Navigation property to user
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Navigation property to assigned reviewer
    /// </summary>
    public virtual User? AssignedReviewer { get; set; }
}

/// <summary>
/// Status of content review
/// </summary>
public enum ReviewStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Escalated = 3
}

/// <summary>
/// Final decision on content review
/// </summary>
public enum ReviewDecision
{
    Approved = 0,
    Rejected = 1,
    ApprovedWithEdits = 2,
    RequiresMoreInfo = 3
}