namespace SkillLedger.Core.Enums;

/// <summary>
/// Status of a project milestone for tracking deliverable progress
/// </summary>
public enum MilestoneStatus
{
    /// <summary>
    /// Milestone has been created but work hasn't started
    /// </summary>
    NotStarted = 0,

    /// <summary>
    /// Work is currently in progress on this milestone
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// Milestone has been completed and is pending review
    /// </summary>
    PendingReview = 2,

    /// <summary>
    /// Milestone has been reviewed and approved
    /// </summary>
    Approved = 3,

    /// <summary>
    /// Milestone was rejected and needs revision
    /// </summary>
    RequiresRevision = 4,

    /// <summary>
    /// Milestone was cancelled
    /// </summary>
    Cancelled = 5
}


/// <summary>
/// Types of deliverable submissions
/// </summary>
public enum DeliverableType
{
    /// <summary>
    /// File upload or attachment
    /// </summary>
    FileUpload = 0,

    /// <summary>
    /// Text description or documentation
    /// </summary>
    TextDescription = 1,

    /// <summary>
    /// URL link to external work
    /// </summary>
    LinkSubmission = 2,

    /// <summary>
    /// Code repository or source control reference
    /// </summary>
    CodeRepository = 3
}