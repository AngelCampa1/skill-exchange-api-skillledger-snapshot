namespace SkillLedger.Core.Enums;

/// <summary>
/// Status values for project applications
/// </summary>
public enum ApplicationStatus
{
    /// <summary>
    /// Application submitted and awaiting review
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Application is being actively reviewed by client
    /// </summary>
    UnderReview = 1,

    /// <summary>
    /// Application has been accepted by client
    /// </summary>
    Accepted = 2,

    /// <summary>
    /// Application has been rejected by client
    /// </summary>
    Rejected = 3,

    /// <summary>
    /// Application was withdrawn by the provider
    /// </summary>
    Withdrawn = 4,

    /// <summary>
    /// Application expired due to inactivity
    /// </summary>
    Expired = 5
}