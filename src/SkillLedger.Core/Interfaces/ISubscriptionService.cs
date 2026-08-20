using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;

namespace SkillLedger.Core.Interfaces;

public interface ISubscriptionService
{
    /// <summary>
    /// Creates a new subscription for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="subscriptionTierId">Subscription tier ID</param>
    /// <param name="paymentMethodId">Payment method ID</param>
    /// <param name="isTrial">Whether this is a trial subscription</param>
    /// <param name="isAnnual">Whether this is an annual subscription</param>
    /// <param name="createdFromIP">IP address of the request</param>
    /// <returns>Created subscription</returns>
    Task<UserSubscription> CreateSubscriptionAsync(
        Guid userId,
        Guid subscriptionTierId,
        Guid paymentMethodId,
        bool isTrial = false,
        bool isAnnual = false,
        string? createdFromIP = null);

    /// <summary>
    /// Creates a new subscription from a Stripe Checkout session (webhook handler)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="subscriptionTierId">Subscription tier ID</param>
    /// <param name="stripeSubscriptionId">Stripe subscription ID from checkout</param>
    /// <param name="stripeCustomerId">Stripe customer ID</param>
    /// <returns>Created subscription</returns>
    Task<UserSubscription> CreateSubscriptionAsync(
        Guid userId,
        Guid subscriptionTierId,
        string? stripeSubscriptionId,
        string? stripeCustomerId);

    /// <summary>
    /// Creates a new subscription from a Stripe Checkout session with promotion info (webhook handler)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="subscriptionTierId">Subscription tier ID</param>
    /// <param name="stripeSubscriptionId">Stripe subscription ID from checkout</param>
    /// <param name="stripeCustomerId">Stripe customer ID</param>
    /// <param name="promotionInfo">Promotion/discount information from checkout</param>
    /// <returns>Created subscription</returns>
    Task<UserSubscription> CreateSubscriptionAsync(
        Guid userId,
        Guid subscriptionTierId,
        string? stripeSubscriptionId,
        string? stripeCustomerId,
        SubscriptionPromotionInfo? promotionInfo);

    /// <summary>
    /// Records a successful payment for a subscription (webhook handler)
    /// </summary>
    /// <param name="stripeSubscriptionId">Stripe subscription ID</param>
    /// <param name="amountPaid">Amount paid in cents</param>
    Task RecordPaymentAsync(string stripeSubscriptionId, long amountPaid);

    /// <summary>
    /// Gets a user's current active subscription
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Active subscription or null</returns>
    Task<UserSubscription?> GetUserActiveSubscriptionAsync(Guid userId);

    /// <summary>
    /// Gets a subscription by external Stripe ID
    /// </summary>
    /// <param name="externalSubscriptionId">External subscription ID</param>
    /// <returns>Subscription or null</returns>
    Task<UserSubscription?> GetSubscriptionByExternalIdAsync(string externalSubscriptionId);

    /// <summary>
    /// Gets all subscriptions for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paginated user subscriptions</returns>
    Task<(List<UserSubscription> subscriptions, int totalCount)> GetUserSubscriptionsAsync(
        Guid userId,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    /// Gets all available subscription tiers
    /// </summary>
    /// <returns>Available subscription tiers</returns>
    Task<List<SubscriptionTier>> GetSubscriptionTiersAsync();

    /// <summary>
    /// Gets a subscription tier by ID
    /// </summary>
    /// <param name="tierId">Tier ID</param>
    /// <returns>Subscription tier or null</returns>
    Task<SubscriptionTier?> GetSubscriptionTierAsync(Guid tierId);

    /// <summary>
    /// Upgrades a user's subscription to a higher tier
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="newTierId">New subscription tier ID</param>
    /// <param name="immediateCharge">Whether to charge immediately or wait for next billing</param>
    /// <param name="createdFromIP">IP address of the request</param>
    /// <returns>Updated subscription</returns>
    Task<UserSubscription> UpgradeSubscriptionAsync(
        Guid userId,
        Guid newTierId,
        bool immediateCharge = true,
        string? createdFromIP = null);

    /// <summary>
    /// Downgrades a user's subscription to a lower tier
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="newTierId">New subscription tier ID</param>
    /// <param name="effectiveDate">When the downgrade takes effect</param>
    /// <param name="createdFromIP">IP address of the request</param>
    /// <returns>Updated subscription</returns>
    Task<UserSubscription> DowngradeSubscriptionAsync(
        Guid userId,
        Guid newTierId,
        DateTime? effectiveDate = null,
        string? createdFromIP = null);

    /// <summary>
    /// Cancels a user's subscription
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="reason">Cancellation reason</param>
    /// <param name="immediate">Whether to cancel immediately or at period end</param>
    /// <param name="createdFromIP">IP address of the request</param>
    /// <returns>Cancelled subscription</returns>
    Task<UserSubscription> CancelSubscriptionAsync(
        Guid userId,
        string? reason = null,
        bool immediate = false,
        string? createdFromIP = null);

    /// <summary>
    /// Renews a subscription (typically called by billing service)
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <param name="createdFromIP">IP address of the request</param>
    /// <returns>Renewed subscription</returns>
    Task<UserSubscription> RenewSubscriptionAsync(Guid subscriptionId, string? createdFromIP = null);

    /// <summary>
    /// Pauses a subscription
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="pauseDuration">Duration to pause</param>
    /// <param name="createdFromIP">IP address of the request</param>
    /// <returns>Paused subscription</returns>
    Task<UserSubscription> PauseSubscriptionAsync(
        Guid userId,
        TimeSpan pauseDuration,
        string? createdFromIP = null);

    /// <summary>
    /// Resumes a paused subscription
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="createdFromIP">IP address of the request</param>
    /// <returns>Resumed subscription</returns>
    Task<UserSubscription> ResumeSubscriptionAsync(Guid userId, string? createdFromIP = null);

    /// <summary>
    /// Checks if a user has access to a specific feature based on their subscription
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="feature">Feature name to check</param>
    /// <returns>True if user has access to the feature</returns>
    Task<bool> HasFeatureAccessAsync(Guid userId, string feature);

    /// <summary>
    /// Gets a user's subscription limits (projects, credits, etc.)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Subscription limits</returns>
    Task<SubscriptionLimitsDto> GetUserSubscriptionLimitsAsync(Guid userId);

    /// <summary>
    /// Processes trial conversion to paid subscription
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="paymentMethodId">Payment method ID</param>
    /// <param name="createdFromIP">IP address of the request</param>
    /// <returns>Converted subscription</returns>
    Task<UserSubscription> ConvertTrialToPaidAsync(
        Guid userId,
        Guid paymentMethodId,
        string? createdFromIP = null);

    /// <summary>
    /// Gets subscription statistics for analytics
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <returns>Subscription statistics</returns>
    Task<SubscriptionStatisticsDto> GetSubscriptionStatisticsAsync(
        DateTime startDate,
        DateTime endDate);

    /// <summary>
    /// Gets a user's current usage statistics (projects, earnings, etc.)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>User usage statistics</returns>
    Task<UserUsageStatisticsDto> GetUserUsageStatisticsAsync(Guid userId);
}

/// <summary>
/// DTO for subscription limits
/// </summary>
public class SubscriptionLimitsDto
{
    public int MaxActiveProjects { get; set; }
    public int MaxTeamMembers { get; set; }
    public int MaxMonthlyEarnings { get; set; }
    public bool PrioritySupport { get; set; }
    public bool ApiAccess { get; set; }
    public bool AdvancedAnalytics { get; set; }
    public bool AdvancedFraudDetection { get; set; }
    public bool MultiSignature { get; set; }
    public bool CustomIntegrations { get; set; }
    public List<string> Features { get; set; } = new();
}

/// <summary>
/// DTO for subscription statistics
/// </summary>
public class SubscriptionStatisticsDto
{
    public int TotalSubscriptions { get; set; }
    public int ActiveSubscriptions { get; set; }
    public int TrialSubscriptions { get; set; }
    public int CancelledSubscriptions { get; set; }
    public int ExpiredSubscriptions { get; set; }
    public decimal MonthlyRecurringRevenue { get; set; }
    public decimal AnnualRecurringRevenue { get; set; }
    public int NewSubscriptionsThisPeriod { get; set; }
    public int ChurnedSubscriptionsThisPeriod { get; set; }
    public Dictionary<string, int> SubscriptionsByTier { get; set; } = new();
    public Dictionary<string, int> SubscriptionsByStatus { get; set; } = new();
}

/// <summary>
/// DTO for promotion/discount information from Stripe Checkout
/// </summary>
public class SubscriptionPromotionInfo
{
    /// <summary>
    /// Stripe coupon ID that was applied
    /// </summary>
    public string? CouponId { get; set; }

    /// <summary>
    /// The promotion code string entered by the user (if any)
    /// </summary>
    public string? PromoCode { get; set; }

    /// <summary>
    /// Percentage discount (0-100)
    /// </summary>
    public decimal? PercentOff { get; set; }

    /// <summary>
    /// Fixed amount discount in cents
    /// </summary>
    public long? AmountOff { get; set; }

    /// <summary>
    /// Coupon duration type: "once", "repeating", or "forever"
    /// </summary>
    public string? Duration { get; set; }

    /// <summary>
    /// Number of months for repeating discounts
    /// </summary>
    public int? DurationInMonths { get; set; }

    /// <summary>
    /// When the discount ends (calculated from duration)
    /// </summary>
    public DateTime? DiscountEndsAt { get; set; }
}

/// <summary>
/// DTO for user usage statistics
/// </summary>
public class UserUsageStatisticsDto
{
    /// <summary>
    /// Number of active projects owned by the user
    /// </summary>
    public int CurrentActiveProjects { get; set; }

    /// <summary>
    /// Number of team members the user has (across all projects)
    /// </summary>
    public int CurrentTeamMembers { get; set; }

    /// <summary>
    /// Total earnings this month (credits received)
    /// </summary>
    public decimal CurrentMonthlyEarnings { get; set; }

    /// <summary>
    /// Total credits spent this month
    /// </summary>
    public decimal CurrentMonthlySpending { get; set; }

    /// <summary>
    /// Total number of projects created (all time)
    /// </summary>
    public int TotalProjectsCreated { get; set; }

    /// <summary>
    /// Total number of applications sent/received
    /// </summary>
    public int TotalApplications { get; set; }
}