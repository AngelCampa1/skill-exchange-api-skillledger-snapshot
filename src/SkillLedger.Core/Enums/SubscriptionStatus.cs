namespace SkillLedger.Core.Enums;

/// <summary>
/// Represents the status of a user subscription
/// </summary>
public enum SubscriptionStatus
{
    /// <summary>
    /// Subscription is active and user has full access
    /// </summary>
    Active = 1,

    /// <summary>
    /// Subscription is in trial period
    /// </summary>
    Trial = 2,

    /// <summary>
    /// Subscription has been cancelled by user
    /// </summary>
    Cancelled = 3,

    /// <summary>
    /// Subscription has expired and not renewed
    /// </summary>
    Expired = 4,

    /// <summary>
    /// Subscription is suspended due to payment issues
    /// </summary>
    Suspended = 5,

    /// <summary>
    /// Subscription is past due for payment
    /// </summary>
    PastDue = 6
}