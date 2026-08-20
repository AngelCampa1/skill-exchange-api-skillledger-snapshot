using System.ComponentModel.DataAnnotations;
using SkillLedger.Core.Enums;

namespace SkillLedger.Core.Entities;

/// <summary>
/// Represents a bidirectional review system for completed projects with blind review capabilities
/// </summary>
public class ProjectReview
{
    public ProjectReview()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        Status = ProjectReviewStatus.Pending;
        ModerationStatus = ModerationStatus.Pending;
    }

    /// <summary>
    /// Unique identifier for the review
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the project being reviewed
    /// </summary>
    [Required(ErrorMessage = "ProjectId is required")]
    public Guid ProjectId { get; set; }

    /// <summary>
    /// User who is writing the review
    /// </summary>
    [Required(ErrorMessage = "ReviewerId is required")]
    public Guid ReviewerId { get; set; }

    /// <summary>
    /// User who is being reviewed
    /// </summary>
    [Required(ErrorMessage = "RevieweeId is required")]
    public Guid RevieweeId { get; set; }

    /// <summary>
    /// Type of review (client reviewing provider or vice versa)
    /// </summary>
    [Required]
    public ProjectReviewType Type { get; set; }

    /// <summary>
    /// Overall rating on a scale of 1-10
    /// </summary>
    [Range(1, 10, ErrorMessage = "Overall rating must be between 1 and 10")]
    public int OverallRating { get; set; }

    /// <summary>
    /// Rating for work quality (1-10 scale)
    /// </summary>
    [Range(1, 10, ErrorMessage = "Quality rating must be between 1 and 10")]
    public int? QualityRating { get; set; }

    /// <summary>
    /// Rating for communication effectiveness (1-10 scale)
    /// </summary>
    [Range(1, 10, ErrorMessage = "Communication rating must be between 1 and 10")]
    public int? CommunicationRating { get; set; }

    /// <summary>
    /// Rating for meeting deadlines and timeliness (1-10 scale)
    /// </summary>
    [Range(1, 10, ErrorMessage = "Timeliness rating must be between 1 and 10")]
    public int? TimelinessRating { get; set; }

    /// <summary>
    /// Rating for professionalism and conduct (1-10 scale)
    /// </summary>
    [Range(1, 10, ErrorMessage = "Professionalism rating must be between 1 and 10")]
    public int? ProfessionalismRating { get; set; }

    /// <summary>
    /// Detailed written review (minimum 25 characters, maximum 2000)
    /// </summary>
    [Required]
    [MinLength(25, ErrorMessage = "Review text must be at least 25 characters long")]
    [MaxLength(2000, ErrorMessage = "Review text cannot exceed 2000 characters")]
    public string ReviewText { get; set; } = null!;

    /// <summary>
    /// Optional response from the reviewee (maximum 1000 characters)
    /// </summary>
    [MaxLength(1000, ErrorMessage = "Response text cannot exceed 1000 characters")]
    public string? ResponseText { get; set; }

    /// <summary>
    /// Current status of the review in the blind review system
    /// </summary>
    public ProjectReviewStatus Status { get; set; }

    /// <summary>
    /// Content moderation status
    /// </summary>
    public ModerationStatus ModerationStatus { get; set; }

    /// <summary>
    /// Optional moderation notes if review was flagged
    /// </summary>
    [MaxLength(1000)]
    public string? ModerationNotes { get; set; }

    /// <summary>
    /// When the review was initially created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the review was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// When the review was submitted (moved from Pending to SubmittedBlind)
    /// </summary>
    public DateTime? SubmittedAt { get; set; }

    /// <summary>
    /// When the review became visible (moved to Published status)
    /// </summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// IP address from which the review was submitted (for audit purposes)
    /// </summary>
    [MaxLength(45)] // IPv6 max length
    public string? SubmittedFromIP { get; set; }

    /// <summary>
    /// Indicates if photos were attached to this review
    /// </summary>
    public bool HasPhotoAttachments { get; set; } = false;

    /// <summary>
    /// Number of photo attachments (for performance optimization)
    /// </summary>
    public int PhotoAttachmentCount { get; set; } = 0;

    /// <summary>
    /// Navigation property to the project being reviewed
    /// </summary>
    public virtual Project Project { get; set; } = null!;

    /// <summary>
    /// Navigation property to the user writing the review
    /// </summary>
    public virtual User Reviewer { get; set; } = null!;

    /// <summary>
    /// Navigation property to the user being reviewed
    /// </summary>
    public virtual User Reviewee { get; set; } = null!;

    /// <summary>
    /// Navigation property for photo attachments
    /// </summary>
    public virtual ICollection<UploadedFile> PhotoAttachments { get; set; } = new List<UploadedFile>();

    /// <summary>
    /// Navigation property for audit logs related to this review
    /// </summary>
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    // Helper properties for business logic

    /// <summary>
    /// Calculated average rating from dimensional ratings (if provided)
    /// </summary>
    public double CalculatedAverageRating
    {
        get
        {
            var ratings = new List<int>();

            if (QualityRating.HasValue) ratings.Add(QualityRating.Value);
            if (CommunicationRating.HasValue) ratings.Add(CommunicationRating.Value);
            if (TimelinessRating.HasValue) ratings.Add(TimelinessRating.Value);
            if (ProfessionalismRating.HasValue) ratings.Add(ProfessionalismRating.Value);

            return ratings.Count > 0 ? ratings.Average() : OverallRating;
        }
    }

    /// <summary>
    /// Indicates if this is a self-review (should be prevented by business rules)
    /// </summary>
    public bool IsSelfReview => ReviewerId == RevieweeId;

    /// <summary>
    /// Indicates if the review can be edited (only when in Pending status)
    /// </summary>
    public bool IsEditable => Status == ProjectReviewStatus.Pending;

    /// <summary>
    /// Indicates if the review can be retracted (only when in SubmittedBlind status)
    /// </summary>
    public bool CanBeRetracted => Status == ProjectReviewStatus.SubmittedBlind;

    /// <summary>
    /// Indicates if the review is visible to both parties (Published status)
    /// </summary>
    public bool IsVisible => Status == ProjectReviewStatus.Published;

    /// <summary>
    /// Indicates if the review is under content moderation
    /// </summary>
    public bool IsUnderModeration => Status == ProjectReviewStatus.UnderModeration ||
                                   ModerationStatus == ModerationStatus.Pending;

    /// <summary>
    /// Helper method to submit the review (changes status to SubmittedBlind)
    /// </summary>
    public void Submit(string ipAddress)
    {
        if (Status != ProjectReviewStatus.Pending)
            throw new InvalidOperationException("Review can only be submitted when in Pending status");

        if (IsSelfReview)
            throw new InvalidOperationException("Self-reviews are not allowed");

        Status = ProjectReviewStatus.SubmittedBlind;
        SubmittedAt = DateTime.UtcNow;
        SubmittedFromIP = ipAddress;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Helper method to publish the review (makes it visible to both parties)
    /// </summary>
    public void Publish()
    {
        if (Status != ProjectReviewStatus.SubmittedBlind)
            throw new InvalidOperationException("Review can only be published when in SubmittedBlind status");

        Status = ProjectReviewStatus.Published;
        PublishedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Helper method to retract the review (only allowed before counterpart submits)
    /// </summary>
    public void Retract()
    {
        if (!CanBeRetracted)
            throw new InvalidOperationException("Review can only be retracted when in SubmittedBlind status");

        Status = ProjectReviewStatus.Retracted;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Helper method to flag review for moderation
    /// </summary>
    public void FlagForModeration(string reason)
    {
        Status = ProjectReviewStatus.UnderModeration;
        ModerationStatus = ModerationStatus.Pending;
        ModerationNotes = reason;
        UpdatedAt = DateTime.UtcNow;
    }
}