namespace SkillLedger.Core.Enums;

/// <summary>
/// Categories for different types of badges
/// </summary>
public enum BadgeCategory
{
    /// <summary>
    /// Based on ratings and reviews
    /// </summary>
    Performance,

    /// <summary>
    /// Based on project completion count
    /// </summary>
    Volume,

    /// <summary>
    /// Skill-specific certifications
    /// </summary>
    Expertise,

    /// <summary>
    /// Identity and credential verification
    /// </summary>
    Trust,

    /// <summary>
    /// Platform engagement and helpfulness
    /// </summary>
    Community,

    /// <summary>
    /// Special accomplishments
    /// </summary>
    Achievement
}