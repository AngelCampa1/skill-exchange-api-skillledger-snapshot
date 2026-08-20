namespace SkillLedger.Core.Enums;

/// <summary>
/// Represents the type of subscription tier
/// </summary>
public enum SubscriptionTierType
{
    /// <summary>
    /// Free tier with basic features — removed. All users must select a paid subscription.
    /// Kept as a serialized int value for DB backward compatibility only.
    /// </summary>
    [Obsolete("Free tier has been removed. All users must select a paid subscription with a 30-day trial.")]
    Free = 1,

    /// <summary>
    /// Professional tier for individual users
    /// </summary>
    Professional = 2,

    /// <summary>
    /// Business tier for small businesses and teams
    /// </summary>
    Business = 3,

    /// <summary>
    /// Enterprise tier for large organizations
    /// </summary>
    Enterprise = 4
}