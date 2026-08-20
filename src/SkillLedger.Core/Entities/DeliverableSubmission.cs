using System.ComponentModel.DataAnnotations;
using SkillLedger.Core.Enums;

namespace SkillLedger.Core.Entities;

/// <summary>
/// Deliverable submission for a project milestone
/// Tracks work submitted by providers for milestone completion
/// </summary>
public class DeliverableSubmission
{
    public DeliverableSubmission()
    {
        Id = Guid.NewGuid();
        SubmittedAt = DateTime.UtcNow;
        Type = DeliverableType.TextDescription;
        AttachedFiles = new List<UploadedFile>();
    }

    /// <summary>
    /// Unique identifier for the submission
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to the associated milestone
    /// </summary>
    public Guid MilestoneId { get; set; }

    /// <summary>
    /// Navigation property to the milestone
    /// </summary>
    public virtual ProjectMilestone Milestone { get; set; } = null!;

    /// <summary>
    /// User who submitted this deliverable
    /// </summary>
    public Guid SubmittedByUserId { get; set; }

    /// <summary>
    /// Navigation property to the user who submitted
    /// </summary>
    public virtual User SubmittedByUser { get; set; } = null!;

    /// <summary>
    /// Type of deliverable submission
    /// </summary>
    public DeliverableType Type { get; set; }

    /// <summary>
    /// Title or summary of the submission
    /// </summary>
    [Required]
    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the submitted work
    /// </summary>
    [MaxLength(5000)]
    public string? Description { get; set; }

    /// <summary>
    /// URL for link-type submissions
    /// </summary>
    [MaxLength(2000)]
    public string? SubmissionUrl { get; set; }

    /// <summary>
    /// Text content for text-type submissions
    /// </summary>
    public string? TextContent { get; set; }

    /// <summary>
    /// When the submission was made
    /// </summary>
    public DateTime SubmittedAt { get; set; }

    /// <summary>
    /// IP address from which the submission was made
    /// </summary>
    [MaxLength(45)]
    public string? SubmittedFromIP { get; set; }

    /// <summary>
    /// Optional notes from the submitter
    /// </summary>
    [MaxLength(2000)]
    public string? SubmissionNotes { get; set; }

    /// <summary>
    /// Whether this submission has been reviewed
    /// </summary>
    public bool IsReviewed { get; set; } = false;

    /// <summary>
    /// Whether this submission was approved
    /// </summary>
    public bool IsApproved { get; set; } = false;

    /// <summary>
    /// When this submission was reviewed
    /// </summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>
    /// User who reviewed this submission
    /// </summary>
    public Guid? ReviewedByUserId { get; set; }

    /// <summary>
    /// Navigation property to the user who reviewed
    /// </summary>
    public virtual User? ReviewedByUser { get; set; }

    /// <summary>
    /// Feedback from the reviewer
    /// </summary>
    [MaxLength(3000)]
    public string? ReviewFeedback { get; set; }

    /// <summary>
    /// Collection of files attached to this submission
    /// </summary>
    public virtual ICollection<UploadedFile> AttachedFiles { get; set; }

    // Calculated Properties

    /// <summary>
    /// Check if this submission can be reviewed
    /// </summary>
    public bool CanBeReviewed => !IsReviewed;

    /// <summary>
    /// Get the total size of attached files in bytes
    /// </summary>
    public long TotalFileSize => AttachedFiles?.Sum(f => f.FileSizeBytes) ?? 0;

    /// <summary>
    /// Get the number of attached files
    /// </summary>
    public int AttachmentCount => AttachedFiles?.Count ?? 0;

    // Business Methods

    /// <summary>
    /// Approve this submission
    /// </summary>
    /// <param name="reviewedByUserId">User approving the submission</param>
    /// <param name="reviewFeedback">Optional feedback</param>
    /// <returns>True if approval successful</returns>
    public bool Approve(Guid reviewedByUserId, string? reviewFeedback = null)
    {
        if (!CanBeReviewed)
            return false;

        IsReviewed = true;
        IsApproved = true;
        ReviewedAt = DateTime.UtcNow;
        ReviewedByUserId = reviewedByUserId;
        ReviewFeedback = reviewFeedback;

        return true;
    }

    /// <summary>
    /// Reject this submission
    /// </summary>
    /// <param name="reviewedByUserId">User rejecting the submission</param>
    /// <param name="reviewFeedback">Required feedback explaining the rejection</param>
    /// <returns>True if rejection successful</returns>
    public bool Reject(Guid reviewedByUserId, string reviewFeedback)
    {
        if (!CanBeReviewed || string.IsNullOrWhiteSpace(reviewFeedback))
            return false;

        IsReviewed = true;
        IsApproved = false;
        ReviewedAt = DateTime.UtcNow;
        ReviewedByUserId = reviewedByUserId;
        ReviewFeedback = reviewFeedback;

        return true;
    }

    /// <summary>
    /// Add a file attachment to this submission
    /// </summary>
    /// <param name="uploadedFile">File to attach</param>
    /// <returns>True if file added successfully</returns>
    public bool AddFileAttachment(UploadedFile uploadedFile)
    {
        if (uploadedFile == null || IsReviewed)
            return false;

        AttachedFiles.Add(uploadedFile);
        return true;
    }

    /// <summary>
    /// Remove a file attachment from this submission
    /// </summary>
    /// <param name="fileId">ID of file to remove</param>
    /// <returns>True if file removed successfully</returns>
    public bool RemoveFileAttachment(Guid fileId)
    {
        if (IsReviewed)
            return false;

        var file = AttachedFiles?.FirstOrDefault(f => f.Id == fileId);
        if (file == null)
            return false;

        AttachedFiles?.Remove(file);
        return true;
    }

    /// <summary>
    /// Validate the submission based on its type
    /// </summary>
    /// <returns>True if submission is valid</returns>
    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(Title))
            return false;

        return Type switch
        {
            DeliverableType.FileUpload => AttachmentCount > 0,
            DeliverableType.TextDescription => !string.IsNullOrWhiteSpace(TextContent),
            DeliverableType.LinkSubmission => !string.IsNullOrWhiteSpace(SubmissionUrl) && Uri.TryCreate(SubmissionUrl, UriKind.Absolute, out _),
            DeliverableType.CodeRepository => !string.IsNullOrWhiteSpace(SubmissionUrl) && Uri.TryCreate(SubmissionUrl, UriKind.Absolute, out _),
            _ => true
        };
    }
}