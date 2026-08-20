using System.ComponentModel.DataAnnotations;
using SkillLedger.Core.Enums;

namespace SkillLedger.Core.Entities;

/// <summary>
/// Represents a user's subscription to a tier
/// </summary>
public class UserSubscription
{
    /// <summary>
    /// Unique identifier for the user subscription
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// User ID of the subscriber
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Subscription tier ID
    /// </summary>
    [Required]
    public Guid SubscriptionTierId { get; set; }

    /// <summary>
    /// Current status of the subscription
    /// </summary>
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Trial;

    /// <summary>
    /// When the subscription starts
    /// </summary>
    [Required]
    public DateTime StartDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the subscription ends (for cancellations or fixed terms)
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// When the next billing date is scheduled
    /// </summary>
    public DateTime? NextBillingDate { get; set; }

    /// <summary>
    /// When the trial period ends
    /// </summary>
    public DateTime? TrialEndDate { get; set; }

    /// <summary>
    /// Whether the subscription auto-renews
    /// </summary>
    public bool AutoRenew { get; set; } = true;

    /// <summary>
    /// Payment method ID for this subscription
    /// </summary>
    public Guid? PaymentMethodId { get; set; }

    /// <summary>
    /// External subscription ID (e.g., Stripe subscription ID)
    /// </summary>
    [MaxLength(200)]
    public string? ExternalSubscriptionId { get; set; }

    /// <summary>
    /// External customer ID (e.g., Stripe customer ID)
    /// </summary>
    [MaxLength(200)]
    public string? ExternalCustomerId { get; set; }

    /// <summary>
    /// Number of billing cycles completed
    /// </summary>
    public int BillingCycleCount { get; set; } = 0;

    /// <summary>
    /// Whether this is an annual subscription
    /// </summary>
    public bool IsAnnual { get; set; } = false;

    /// <summary>
    /// Number of retry attempts for failed payments
    /// </summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// When the next retry will be attempted
    /// </summary>
    public DateTime? NextRetryAt { get; set; }

    /// <summary>
    /// When the subscription was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the subscription was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the last successful payment was recorded
    /// </summary>
    public DateTime? LastPaymentDate { get; set; }

    /// <summary>
    /// When the subscription was cancelled
    /// </summary>
    public DateTime? CancelledAt { get; set; }

    /// <summary>
    /// Reason for cancellation
    /// </summary>
    [MaxLength(500)]
    public string? CancellationReason { get; set; }

    #region Promotion Tracking

    /// <summary>
    /// Stripe coupon ID that was applied to this subscription.
    /// Example: "launch_3mo_free"
    /// </summary>
    [MaxLength(100)]
    public string? AppliedCouponId { get; set; }

    /// <summary>
    /// The user-entered promotion code (if any).
    /// Example: "LAUNCH2024"
    /// </summary>
    [MaxLength(100)]
    public string? AppliedPromoCode { get; set; }

    /// <summary>
    /// When the promotional discount period ends.
    /// After this date, regular billing resumes.
    /// </summary>
    public DateTime? DiscountEndsAt { get; set; }

    #endregion

    /// <summary>
    /// Navigation property for the user
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Navigation property for the subscription tier
    /// </summary>
    public virtual SubscriptionTier SubscriptionTier { get; set; } = null!;

    /// <summary>
    /// Navigation property for the payment method
    /// </summary>
    public virtual PaymentMethod? PaymentMethod { get; set; }

    /// <summary>
    /// Navigation property for subscription transactions
    /// </summary>
    public virtual ICollection<SubscriptionTransaction> Transactions { get; set; } = new List<SubscriptionTransaction>();
}