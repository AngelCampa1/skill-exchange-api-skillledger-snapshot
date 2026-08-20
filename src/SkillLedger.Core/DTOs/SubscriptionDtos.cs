using System.ComponentModel.DataAnnotations;
using SkillLedger.Core.Enums;

namespace SkillLedger.Core.DTOs;

/// <summary>
/// DTO for creating a new subscription
/// </summary>
public class CreateSubscriptionDto
{
    /// <summary>
    /// Subscription tier ID
    /// </summary>
    [Required]
    public Guid SubscriptionTierId { get; set; }

    /// <summary>
    /// Payment method ID
    /// </summary>
    [Required]
    public Guid PaymentMethodId { get; set; }

    /// <summary>
    /// Whether to start with a trial period
    /// </summary>
    public bool IsTrial { get; set; } = false;

    /// <summary>
    /// Whether this is an annual subscription
    /// </summary>
    public bool IsAnnual { get; set; } = false;
}

/// <summary>
/// DTO for upgrading/downgrading a subscription
/// </summary>
public class ChangeSubscriptionTierDto
{
    /// <summary>
    /// New subscription tier ID
    /// </summary>
    [Required]
    public Guid NewTierId { get; set; }

    /// <summary>
    /// Whether to charge immediately for upgrades
    /// </summary>
    public bool ImmediateCharge { get; set; } = true;

    /// <summary>
    /// When the change should take effect (for downgrades)
    /// </summary>
    public DateTime? EffectiveDate { get; set; }
}

/// <summary>
/// DTO for cancelling a subscription
/// </summary>
public class CancelSubscriptionDto
{
    /// <summary>
    /// Reason for cancellation
    /// </summary>
    [MaxLength(500)]
    public string? Reason { get; set; }

    /// <summary>
    /// Whether to cancel immediately or at period end
    /// </summary>
    public bool Immediate { get; set; } = false;
}

/// <summary>
/// DTO for pausing a subscription
/// </summary>
public class PauseSubscriptionDto
{
    /// <summary>
    /// Duration to pause the subscription
    /// </summary>
    [Required]
    public TimeSpan PauseDuration { get; set; }
}

/// <summary>
/// DTO for creating a payment method
/// </summary>
public class CreatePaymentMethodDto
{
    /// <summary>
    /// Payment provider (e.g., 'stripe')
    /// </summary>
    [Required]
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Payment method token from provider
    /// </summary>
    [Required]
    public string PaymentMethodToken { get; set; } = string.Empty;

    /// <summary>
    /// Whether this should be the default payment method
    /// </summary>
    public bool IsDefault { get; set; } = false;
}

/// <summary>
/// DTO for processing a one-time payment
/// </summary>
public class ProcessPaymentDto
{
    /// <summary>
    /// Payment method ID
    /// </summary>
    [Required]
    public Guid PaymentMethodId { get; set; }

    /// <summary>
    /// Amount to charge
    /// </summary>
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    /// <summary>
    /// Currency code
    /// </summary>
    [Required]
    [StringLength(3)]
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Payment description
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }
}

/// <summary>
/// DTO for subscription tier display
/// </summary>
public class SubscriptionTierDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal? AnnualPrice { get; set; }
    public int CreditBonus { get; set; }
    public int MaxActiveProjects { get; set; }
    public int MaxTeamMembers { get; set; }
    public bool PrioritySupport { get; set; }
    public bool ApiAccess { get; set; }
    public bool AdvancedAnalytics { get; set; }
    public bool AdvancedFraudDetection { get; set; }
    public bool MultiSignature { get; set; }
    public bool CustomIntegrations { get; set; }
    public int MaxMonthlyEarnings { get; set; }
    public List<string> Features { get; set; } = new();
    public int SortOrder { get; set; }
}

/// <summary>
/// DTO for user subscription display
/// </summary>
public class UserSubscriptionDto
{
    public Guid Id { get; set; }
    public SubscriptionStatus Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? NextBillingDate { get; set; }
    public DateTime? TrialEndDate { get; set; }
    public bool AutoRenew { get; set; }
    public bool IsAnnual { get; set; }
    public int BillingCycleCount { get; set; }
    public int RetryCount { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public SubscriptionTierDto Tier { get; set; } = new();
    public PaymentMethodDto? PaymentMethod { get; set; }
    public List<SubscriptionTransactionDto> RecentTransactions { get; set; } = new();
}

/// <summary>
/// DTO for payment method display
/// </summary>
public class PaymentMethodDto
{
    public Guid Id { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Last4Digits { get; set; }
    public string? Brand { get; set; }
    public string? ExpiryDate { get; set; }
    public string? CardholderName { get; set; }
    public string? BillingCountry { get; set; }
    public string? BillingPostalCode { get; set; }
    public bool IsDefault { get; set; }
    public bool IsValid { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
}

/// <summary>
/// DTO for subscription transaction display
/// </summary>
public class SubscriptionTransactionDto
{
    public Guid Id { get; set; }
    public SubscriptionTransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? ExternalTransactionId { get; set; }
    public TransactionStatus Status { get; set; }
    public string? Description { get; set; }
    public string? FailureReason { get; set; }
    public int RetryCount { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public DateTime? RefundedAt { get; set; }
    public decimal? RefundAmount { get; set; }
}

/// <summary>
/// DTO for subscription limits
/// </summary>
public class SubscriptionLimitsResponseDto
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
    public int CurrentProjects { get; set; }
    public int CurrentTeamMembers { get; set; }
    public int CurrentMonthlyEarnings { get; set; }
}

/// <summary>
/// DTO for payment result
/// </summary>
public class PaymentResultDto
{
    public bool Success { get; set; }
    public string? TransactionId { get; set; }
    public string? ExternalTransactionId { get; set; }
    public string? ErrorMessage { get; set; }
    public TransactionStatus Status { get; set; }
    public bool RequiresAction { get; set; }
    public string? ClientSecret { get; set; }
    public string? NextActionUrl { get; set; }
}

/// <summary>
/// DTO for refund result
/// </summary>
public class RefundResultDto
{
    public bool Success { get; set; }
    public string? RefundTransactionId { get; set; }
    public string? ExternalRefundId { get; set; }
    public string? ErrorMessage { get; set; }
    public TransactionStatus Status { get; set; }
    public decimal RefundedAmount { get; set; }
}

/// <summary>
/// DTO for subscription statistics
/// </summary>
public class SubscriptionStatisticsResponseDto
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
/// DTO for paginated subscription list
/// </summary>
public class SubscriptionListDto
{
    public List<UserSubscriptionDto> Subscriptions { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }  // Renamed from CurrentPage for API consistency
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

/// <summary>
/// DTO for billing analytics
/// </summary>
public class BillingAnalyticsDto
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal GrossRevenue { get; set; }
    public decimal NetRevenue { get; set; }
    public int NewSubscriptions { get; set; }
    public int CancelledSubscriptions { get; set; }
    public int UpgradedSubscriptions { get; set; }
    public int DowngradedSubscriptions { get; set; }
    public decimal AverageRevenuePerUser { get; set; }
    public decimal CustomerLifetimeValue { get; set; }
    public List<DailyRevenueDto> DailyRevenue { get; set; } = new();
}

/// <summary>
/// DTO for daily revenue
/// </summary>
public class DailyRevenueDto
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
    public int Transactions { get; set; }
    public int NewSubscriptions { get; set; }
    public int CancelledSubscriptions { get; set; }
}