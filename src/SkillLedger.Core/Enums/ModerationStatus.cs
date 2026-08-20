namespace SkillLedger.Core.Enums;

public enum ModerationStatus
{
    /// <summary>
    /// Content is pending moderation review
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Content has been approved for publication
    /// </summary>
    Approved = 1,

    /// <summary>
    /// Content was rejected during moderation
    /// </summary>
    Rejected = 2,

    /// <summary>
    /// Content is flagged for human review
    /// </summary>
    Flagged = 3,

    /// <summary>
    /// Content was automatically approved (bypassed moderation)
    /// </summary>
    AutoApproved = 4
}