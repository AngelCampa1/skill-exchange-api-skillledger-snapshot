using System.ComponentModel.DataAnnotations;
using SkillLedger.Core.Enums;

namespace SkillLedger.Core.Entities;

/// <summary>
/// Represents a transaction related to subscription billing
/// </summary>
public class SubscriptionTransaction
{
    /// <summary>
    /// Unique identifier for the subscription transaction
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// User ID for this transaction
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Subscription ID for this transaction
    /// </summary>
    [Required]
    public Guid SubscriptionId { get; set; }

    /// <summary>
    /// Type of subscription transaction
    /// </summary>
    public SubscriptionTransactionType Type { get; set; }

    /// <summary>
    /// Transaction amount in USD
    /// </summary>
    [Required]
    public decimal Amount { get; set; }

    /// <summary>
    /// Currency code (e.g., 'USD')
    /// </summary>
    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Payment method ID used for this transaction
    /// </summary>
    public Guid? PaymentMethodId { get; set; }

    /// <summary>
    /// External transaction ID from payment provider
    /// </summary>
    [MaxLength(200)]
    public string? ExternalTransactionId { get; set; }

    /// <summary>
    /// External charge ID from payment provider
    /// </summary>
    [MaxLength(200)]
    public string? ExternalChargeId { get; set; }

    /// <summary>
    /// Current status of the transaction
    /// </summary>
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

    /// <summary>
    /// Description of the transaction
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Failure reason if transaction failed
    /// </summary>
    [MaxLength(500)]
    public string? FailureReason { get; set; }

    /// <summary>
    /// Number of retry attempts
    /// </summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// When the next retry will be attempted
    /// </summary>
    public DateTime? NextRetryAt { get; set; }

    /// <summary>
    /// When the transaction was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the transaction was processed
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// When the transaction was completed
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// When the transaction failed
    /// </summary>
    public DateTime? FailedAt { get; set; }

    /// <summary>
    /// When the transaction was refunded
    /// </summary>
    public DateTime? RefundedAt { get; set; }

    /// <summary>
    /// Refund amount if applicable
    /// </summary>
    public decimal? RefundAmount { get; set; }

    /// <summary>
    /// IP address from which the transaction was initiated
    /// </summary>
    [MaxLength(45)]
    public string? CreatedFromIP { get; set; }

    /// <summary>
    /// User agent for the transaction
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Navigation property for the user
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Navigation property for the subscription
    /// </summary>
    public virtual UserSubscription Subscription { get; set; } = null!;

    /// <summary>
    /// Navigation property for the payment method
    /// </summary>
    public virtual PaymentMethod? PaymentMethod { get; set; }
}