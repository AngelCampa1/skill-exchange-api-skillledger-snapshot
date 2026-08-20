using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillLedger.Core.Entities;

/// <summary>
/// Represents a user's payment method for subscriptions
/// </summary>
public class PaymentMethod
{
    /// <summary>
    /// Unique identifier for the payment method
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// User ID who owns this payment method
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Payment provider (e.g., 'stripe', 'paypal')
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Type of payment method (e.g., 'card', 'bank_account')
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Tokenized payment method identifier from provider
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Last 4 digits for display purposes
    /// </summary>
    [MaxLength(4)]
    public string? Last4Digits { get; set; }

    /// <summary>
    /// Card brand or bank name
    /// </summary>
    [MaxLength(100)]
    public string? Brand { get; set; }

    /// <summary>
    /// Expiry date (for cards) in MM/YYYY format
    /// </summary>
    [MaxLength(7)]
    public string? ExpiryDate { get; set; }

    /// <summary>
    /// Cardholder name
    /// </summary>
    [MaxLength(200)]
    public string? CardholderName { get; set; }

    /// <summary>
    /// Billing country
    /// </summary>
    [MaxLength(2)]
    public string? BillingCountry { get; set; }

    /// <summary>
    /// Billing postal code
    /// </summary>
    [MaxLength(20)]
    public string? BillingPostalCode { get; set; }

    /// <summary>
    /// Whether this is the default payment method
    /// </summary>
    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// Whether the payment method is currently valid
    /// </summary>
    public bool IsValid { get; set; } = true;

    /// <summary>
    /// When the payment method expires
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// When the payment method was added
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the payment method was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the payment method was last used
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Navigation property for the user
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Navigation property for user subscriptions using this payment method
    /// </summary>
    public virtual ICollection<UserSubscription> Subscriptions { get; set; } = new List<UserSubscription>();
}