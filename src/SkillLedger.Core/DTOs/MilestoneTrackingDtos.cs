using System.ComponentModel.DataAnnotations;
using SkillLedger.Core.Enums;

namespace SkillLedger.Core.DTOs;

#region Request DTOs

/// <summary>
/// DTO for creating a new project milestone
/// </summary>
public class CreateMilestoneRequestDto
{
    /// <summary>
    /// Project ID this milestone belongs to
    /// </summary>
    [Required]
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Title of the milestone
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of milestone requirements
    /// </summary>
    [Required]
    [StringLength(2000, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Priority level of the milestone
    /// </summary>
    public MilestonePriority Priority { get; set; } = MilestonePriority.Medium;

    /// <summary>
    /// Expected due date for this milestone
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Order sequence for this milestone within the project
    /// </summary>
    [Range(1, int.MaxValue)]
    public int SequenceOrder { get; set; } = 1;

    /// <summary>
    /// Percentage weight of this milestone in overall project completion
    /// </summary>
    [Range(0, 100)]
    public decimal WeightPercentage { get; set; } = 0;

    /// <summary>
    /// Acceptance criteria or deliverable requirements
    /// </summary>
    [StringLength(3000)]
    public string? AcceptanceCriteria { get; set; }

    /// <summary>
    /// User assigned to complete this milestone
    /// </summary>
    public Guid? AssignedToUserId { get; set; }

    /// <summary>
    /// Optional escrow milestone ID to link for payment triggers
    /// </summary>
    public Guid? EscrowMilestoneId { get; set; }
}

/// <summary>
/// DTO for updating an existing milestone
/// </summary>
public class UpdateMilestoneRequestDto
{
    /// <summary>
    /// Title of the milestone
    /// </summary>
    [StringLength(200, MinimumLength = 3)]
    public string? Title { get; set; }

    /// <summary>
    /// Detailed description of milestone requirements
    /// </summary>
    [StringLength(2000, MinimumLength = 10)]
    public string? Description { get; set; }

    /// <summary>
    /// Priority level of the milestone
    /// </summary>
    public MilestonePriority? Priority { get; set; }

    /// <summary>
    /// Expected due date for this milestone
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Order sequence for this milestone within the project
    /// </summary>
    [Range(1, int.MaxValue)]
    public int? SequenceOrder { get; set; }

    /// <summary>
    /// Percentage weight of this milestone in overall project completion
    /// </summary>
    [Range(0, 100)]
    public decimal? WeightPercentage { get; set; }

    /// <summary>
    /// Acceptance criteria or deliverable requirements
    /// </summary>
    [StringLength(3000)]
    public string? AcceptanceCriteria { get; set; }

    /// <summary>
    /// User assigned to complete this milestone
    /// </summary>
    public Guid? AssignedToUserId { get; set; }
}

/// <summary>
/// DTO for creating a deliverable submission
/// </summary>
public class CreateSubmissionRequestDto
{
    /// <summary>
    /// Milestone ID this submission is for
    /// </summary>
    [Required]
    public Guid MilestoneId { get; set; }

    /// <summary>
    /// Type of deliverable submission
    /// </summary>
    [Required]
    public DeliverableType Type { get; set; }

    /// <summary>
    /// Title or summary of the submission
    /// </summary>
    [Required]
    [StringLength(300, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the submitted work
    /// </summary>
    [StringLength(5000)]
    public string? Description { get; set; }

    /// <summary>
    /// URL for link-type submissions
    /// </summary>
    [StringLength(2000)]
    [Url]
    public string? SubmissionUrl { get; set; }

    /// <summary>
    /// Text content for text-type submissions
    /// </summary>
    public string? TextContent { get; set; }

    /// <summary>
    /// Optional notes from the submitter
    /// </summary>
    [StringLength(2000)]
    public string? SubmissionNotes { get; set; }

    /// <summary>
    /// File IDs for attached files
    /// </summary>
    public List<Guid>? AttachedFileIds { get; set; }
}

/// <summary>
/// DTO for reviewing a deliverable submission
/// </summary>
public class ReviewSubmissionRequestDto
{
    /// <summary>
    /// Whether to approve or reject the submission
    /// </summary>
    [Required]
    public bool IsApproved { get; set; }

    /// <summary>
    /// Feedback from the reviewer
    /// </summary>
    [Required]
    [StringLength(3000, MinimumLength = 10)]
    public string ReviewFeedback { get; set; } = string.Empty;
}

#endregion

#region Response DTOs

/// <summary>
/// DTO for milestone information
/// </summary>
public class MilestoneResponseDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? EscrowMilestoneId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public MilestoneStatus Status { get; set; }
    public MilestonePriority Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int SequenceOrder { get; set; }
    public decimal WeightPercentage { get; set; }
    public string? AcceptanceCriteria { get; set; }
    public string? ReviewNotes { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string CreatedByUserName { get; set; } = string.Empty;
    public Guid? AssignedToUserId { get; set; }
    public string? AssignedToUserName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Calculated properties
    public bool IsOverdue { get; set; }
    public bool CanBeStarted { get; set; }
    public bool CanBeSubmitted { get; set; }
    public bool CanBeApproved { get; set; }
    public int? DaysUntilDue { get; set; }

    // Related data
    public List<SubmissionSummaryDto> Submissions { get; set; } = new List<SubmissionSummaryDto>();
}

/// <summary>
/// DTO for deliverable submission information
/// </summary>
public class SubmissionResponseDto
{
    public Guid Id { get; set; }
    public Guid MilestoneId { get; set; }
    public Guid SubmittedByUserId { get; set; }
    public string SubmittedByUserName { get; set; } = string.Empty;
    public DeliverableType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SubmissionUrl { get; set; }
    public string? TextContent { get; set; }
    public DateTime SubmittedAt { get; set; }
    public string? SubmissionNotes { get; set; }
    public bool IsReviewed { get; set; }
    public bool IsApproved { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public string? ReviewedByUserName { get; set; }
    public string? ReviewFeedback { get; set; }
    public List<AttachedFileDto> AttachedFiles { get; set; } = new List<AttachedFileDto>();

    // Calculated properties
    public bool CanBeReviewed { get; set; }
    public long TotalFileSize { get; set; }
    public int AttachmentCount { get; set; }
}

/// <summary>
/// DTO for submission summary information
/// </summary>
public class SubmissionSummaryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DeliverableType Type { get; set; }
    public DateTime SubmittedAt { get; set; }
    public bool IsReviewed { get; set; }
    public bool IsApproved { get; set; }
    public int AttachmentCount { get; set; }
    public long TotalFileSize { get; set; }
}

/// <summary>
/// DTO for attached file information
/// </summary>
public class AttachedFileDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
    public string FileUrl { get; set; } = string.Empty;
}

/// <summary>
/// DTO for project milestone progress summary
/// </summary>
public class ProjectProgressDto
{
    public Guid ProjectId { get; set; }
    public int TotalMilestones { get; set; }
    public int CompletedMilestones { get; set; }
    public int InProgressMilestones { get; set; }
    public int OverdueMilestones { get; set; }
    public decimal OverallProgressPercentage { get; set; }
    public DateTime? NextMilestoneDue { get; set; }
    public List<MilestoneResponseDto> UpcomingMilestones { get; set; } = new List<MilestoneResponseDto>();
    public List<MilestoneResponseDto> OverdueMilestonesList { get; set; } = new List<MilestoneResponseDto>();
}

#endregion

#region Filter DTOs

/// <summary>
/// DTO for filtering milestones
/// </summary>
public class MilestoneFilterDto
{
    /// <summary>
    /// Filter by project ID
    /// </summary>
    public Guid? ProjectId { get; set; }

    /// <summary>
    /// Filter by milestone status
    /// </summary>
    public MilestoneStatus? Status { get; set; }

    /// <summary>
    /// Filter by priority level
    /// </summary>
    public MilestonePriority? Priority { get; set; }

    /// <summary>
    /// Filter by assigned user
    /// </summary>
    public Guid? AssignedToUserId { get; set; }

    /// <summary>
    /// Filter by created by user
    /// </summary>
    public Guid? CreatedByUserId { get; set; }

    /// <summary>
    /// Filter by due date range - start
    /// </summary>
    public DateTime? DueDateFrom { get; set; }

    /// <summary>
    /// Filter by due date range - end
    /// </summary>
    public DateTime? DueDateTo { get; set; }

    /// <summary>
    /// Show only overdue milestones
    /// </summary>
    public bool? OverdueOnly { get; set; }

    /// <summary>
    /// Page number for pagination
    /// </summary>
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Page size for pagination
    /// </summary>
    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Sort field
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Sort direction (asc/desc)
    /// </summary>
    public string? SortDirection { get; set; } = "asc";
}

/// <summary>
/// DTO for paginated milestone results
/// </summary>
public class PaginatedMilestonesDto
{
    public List<MilestoneResponseDto> Items { get; set; } = new List<MilestoneResponseDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
}

#endregion