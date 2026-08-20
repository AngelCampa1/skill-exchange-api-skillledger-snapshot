namespace SkillLedger.Core.Enums;

/// <summary>
/// Represents the type of subscription transaction
/// </summary>
public enum SubscriptionTransactionType
{
    /// <summary>
    /// Initial subscription purchase
    /// </summary>
    Purchase = 1,

    /// <summary>
    /// Recurring subscription renewal
    /// </summary>
    Renewal = 2,

    /// <summary>
    /// Subscription upgrade to higher tier
    /// </summary>
    Upgrade = 3,

    /// <summary>
    /// Subscription downgrade to lower tier
    /// </summary>
    Downgrade = 4,

    /// <summary>
    /// Subscription cancellation
    /// </summary>
    Cancellation = 5,

    /// <summary>
    /// Refund for subscription
    /// </summary>
    Refund = 6,

    /// <summary>
    /// Trial start
    /// </summary>
    TrialStart = 7,

    /// <summary>
    /// Trial conversion to paid subscription
    /// </summary>
    TrialConversion = 8,

    /// <summary>
    /// Payment failure and retry
    /// </summary>
    PaymentFailure = 9,

    /// <summary>
    /// Subscription pause
    /// </summary>
    Pause = 10,

    /// <summary>
    /// Subscription resume
    /// </summary>
    Resume = 11
}