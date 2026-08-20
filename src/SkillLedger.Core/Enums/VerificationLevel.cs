namespace SkillLedger.Core.Enums;

/// <summary>
/// Levels of verification for badges
/// </summary>
public enum VerificationLevel
{
    /// <summary>
    /// Automatically earned based on system metrics
    /// </summary>
    Automatic,

    /// <summary>
    /// Requires manual review and approval
    /// </summary>
    Manual,

    /// <summary>
    /// Verified through external third-party services
    /// </summary>
    External
}