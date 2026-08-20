namespace SkillLedger.Core.Enums;

/// <summary>
/// Types of project reviews available in the system
/// </summary>
public enum ProjectReviewType
{
    /// <summary>
    /// Review by project client of service provider's work
    /// </summary>
    ClientToProvider = 0,

    /// <summary>
    /// Review by service provider of project client/project experience
    /// </summary>
    ProviderToClient = 1
}

/// <summary>
/// Status of a project review in the blind review system
/// </summary>
public enum ProjectReviewStatus
{
    /// <summary>
    /// Review is pending submission (initial state)
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Review has been submitted but counterpart hasn't submitted yet (temporal lock)
    /// </summary>
    SubmittedBlind = 1,

    /// <summary>
    /// Both parties have submitted reviews - now visible to both
    /// </summary>
    Published = 2,

    /// <summary>
    /// Review was flagged and is under moderation
    /// </summary>
    UnderModeration = 3,

    /// <summary>
    /// Review was rejected by moderation
    /// </summary>
    Rejected = 4,

    /// <summary>
    /// Review was retracted by the reviewer (only allowed before counterpart submits)
    /// </summary>
    Retracted = 5
}