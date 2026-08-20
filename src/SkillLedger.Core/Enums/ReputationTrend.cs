namespace SkillLedger.Core.Enums;

/// <summary>
/// Enumeration representing the trend direction of a user's reputation score
/// </summary>
public enum ReputationTrend
{
    /// <summary>
    /// Reputation score is declining
    /// </summary>
    Declining = 0,

    /// <summary>
    /// Reputation score is stable (minimal change)
    /// </summary>
    Stable = 1,

    /// <summary>
    /// Reputation score is improving
    /// </summary>
    Improving = 2
}