using System.ComponentModel.DataAnnotations;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Attributes;

namespace SkillLedger.Core.Entities;

/// <summary>
/// Project milestone for tracking deliverable progress and integrating with escrow payments
/// </summary>
public class ProjectMilestone
{
    public ProjectMilestone()
    {
        Id = Guid.NewGuid();
        Status = MilestoneStatus.NotStarted;
        Priority = MilestonePriority.Medium;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        Submissions = new List<DeliverableSubmission>();
    }

    /// <summary>
    /// Unique identifier for the milestone
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to the associated project
    /// </summary>
    [NotEmptyGuid]
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Navigation property to the project
    /// </summary>
    public virtual Project Project { get; set; } = null!;

    /// <summary>
    /// Optional foreign key to linked escrow milestone for payment triggers
    /// </summary>
    public Guid? EscrowMilestoneId { get; set; }

    /// <summary>
    /// Navigation property to the escrow milestone
    /// </summary>
    public virtual EscrowMilestone? EscrowMilestone { get; set; }

    /// <summary>
    /// Title of the milestone
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of milestone requirements
    /// </summary>
    [Required]
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the milestone
    /// </summary>
    public MilestoneStatus Status { get; set; }

    /// <summary>
    /// Priority level of the milestone
    /// </summary>
    public MilestonePriority Priority { get; set; }

    /// <summary>
    /// Expected due date for this milestone
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Actual completion date when milestone was approved
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Order sequence for this milestone within the project
    /// </summary>
    public int SequenceOrder { get; set; } = 1;

    /// <summary>
    /// Percentage weight of this milestone in overall project completion
    /// </summary>
    [DecimalRange(0, 100)]
    public decimal WeightPercentage { get; set; } = 0;

    /// <summary>
    /// Acceptance criteria or deliverable requirements
    /// </summary>
    [MaxLength(3000)]
    public string? AcceptanceCriteria { get; set; }

    /// <summary>
    /// Review notes from client or reviewer
    /// </summary>
    [MaxLength(2000)]
    public string? ReviewNotes { get; set; }

    /// <summary>
    /// User who created this milestone (typically project owner)
    /// </summary>
    [NotEmptyGuid]
    public Guid CreatedByUserId { get; set; }

    /// <summary>
    /// Navigation property to user who created this milestone
    /// </summary>
    public virtual User CreatedByUser { get; set; } = null!;

    /// <summary>
    /// User assigned to complete this milestone
    /// </summary>
    public Guid? AssignedToUserId { get; set; }

    /// <summary>
    /// Navigation property to user assigned to this milestone
    /// </summary>
    public virtual User? AssignedToUser { get; set; }

    /// <summary>
    /// When the milestone was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the milestone was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// IP address from which the milestone was created
    /// </summary>
    [MaxLength(45)]
    public string? CreatedFromIP { get; set; }

    /// <summary>
    /// Collection of deliverable submissions for this milestone
    /// </summary>
    public virtual ICollection<DeliverableSubmission> Submissions { get; set; }

    // Calculated Properties

    /// <summary>
    /// Check if this milestone is overdue
    /// </summary>
    public bool IsOverdue => DueDate.HasValue &&
                            Status != MilestoneStatus.Approved &&
                            Status != MilestoneStatus.Cancelled &&
                            DateTime.UtcNow > DueDate.Value;

    /// <summary>
    /// Check if this milestone can be started
    /// </summary>
    public bool CanBeStarted => Status == MilestoneStatus.NotStarted && AssignedToUserId.HasValue;

    /// <summary>
    /// Check if this milestone can be submitted for review
    /// </summary>
    public bool CanBeSubmitted => Status == MilestoneStatus.InProgress;

    /// <summary>
    /// Check if this milestone can be approved
    /// </summary>
    public bool CanBeApproved => Status == MilestoneStatus.PendingReview;

    /// <summary>
    /// Get the latest submission for this milestone
    /// </summary>
    public DeliverableSubmission? LatestSubmission =>
        Submissions?.OrderByDescending(s => s.SubmittedAt).FirstOrDefault();

    /// <summary>
    /// Get days until due (negative if overdue)
    /// </summary>
    public int? DaysUntilDue => DueDate?.Subtract(DateTime.UtcNow).Days;

    // Business Methods

    /// <summary>
    /// Start work on this milestone
    /// </summary>
    /// <param name="userId">User assigned to work on this milestone</param>
    /// <returns>True if milestone was started successfully</returns>
    public bool StartWork(Guid userId)
    {
        if (!CanBeStarted)
            return false;

        Status = MilestoneStatus.InProgress;
        AssignedToUserId = userId;
        UpdatedAt = DateTime.UtcNow;

        return true;
    }

    /// <summary>
    /// Submit milestone for review
    /// </summary>
    /// <returns>True if milestone was submitted successfully</returns>
    public bool SubmitForReview()
    {
        if (!CanBeSubmitted)
            return false;

        Status = MilestoneStatus.PendingReview;
        UpdatedAt = DateTime.UtcNow;

        return true;
    }

    /// <summary>
    /// Approve the milestone
    /// </summary>
    /// <param name="reviewNotes">Optional notes from the reviewer</param>
    /// <returns>True if milestone was approved successfully</returns>
    public bool Approve(string? reviewNotes = null)
    {
        if (!CanBeApproved)
            return false;

        Status = MilestoneStatus.Approved;
        CompletedAt = DateTime.UtcNow;
        ReviewNotes = reviewNotes;
        UpdatedAt = DateTime.UtcNow;

        return true;
    }

    /// <summary>
    /// Reject the milestone and request revisions
    /// </summary>
    /// <param name="reviewNotes">Required notes explaining what needs to be revised</param>
    /// <returns>True if milestone was rejected successfully</returns>
    public bool RequestRevision(string reviewNotes)
    {
        if (Status != MilestoneStatus.PendingReview || string.IsNullOrWhiteSpace(reviewNotes))
            return false;

        Status = MilestoneStatus.RequiresRevision;
        ReviewNotes = reviewNotes;
        UpdatedAt = DateTime.UtcNow;

        return true;
    }

    /// <summary>
    /// Cancel the milestone
    /// </summary>
    /// <param name="reason">Reason for cancellation</param>
    /// <returns>True if milestone was cancelled successfully</returns>
    public bool Cancel(string? reason = null)
    {
        if (Status == MilestoneStatus.Approved)
            return false;

        Status = MilestoneStatus.Cancelled;
        ReviewNotes = reason;
        UpdatedAt = DateTime.UtcNow;

        return true;
    }

    /// <summary>
    /// Update the milestone's timestamp
    /// </summary>
    public void UpdateTimestamp()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}