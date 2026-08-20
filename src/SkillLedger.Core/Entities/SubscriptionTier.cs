using System.ComponentModel.DataAnnotations;
using SkillLedger.Core.Enums;

namespace SkillLedger.Core.Entities;

/// <summary>
/// Represents a subscription tier with pricing and feature limits
/// </summary>
public class SubscriptionTier
{
    /// <summary>
    /// Unique identifier for the subscription tier
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Type of subscription tier
    /// </summary>
    public SubscriptionTierType Type { get; set; }

    /// <summary>
    /// Name of the subscription tier
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the subscription tier
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Monthly price in USD
    /// </summary>
    [Required]
    public decimal Price { get; set; }

    /// <summary>
    /// Annual price in USD (if discounted from monthly)
    /// </summary>
    public decimal? AnnualPrice { get; set; }

    /// <summary>
    /// Number of credits included in the subscription
    /// </summary>
    public int CreditBonus { get; set; }

    /// <summary>
    /// Maximum number of active projects allowed
    /// </summary>
    public int MaxActiveProjects { get; set; }

    /// <summary>
    /// Maximum number of team members allowed
    /// </summary>
    public int MaxTeamMembers { get; set; }

    /// <summary>
    /// Whether priority support is included
    /// </summary>
    public bool PrioritySupport { get; set; }

    /// <summary>
    /// Whether API access is included
    /// </summary>
    public bool ApiAccess { get; set; }

    /// <summary>
    /// Whether advanced analytics are included
    /// </summary>
    public bool AdvancedAnalytics { get; set; }

    /// <summary>
    /// Whether advanced fraud detection is included
    /// </summary>
    public bool AdvancedFraudDetection { get; set; }

    /// <summary>
    /// Whether multi-signature transactions are included
    /// </summary>
    public bool MultiSignature { get; set; }

    /// <summary>
    /// Whether custom integrations are included
    /// </summary>
    public bool CustomIntegrations { get; set; }

    /// <summary>
    /// Maximum monthly earnings in credits
    /// </summary>
    public int MaxMonthlyEarnings { get; set; }

    /// <summary>
    /// Additional features as JSON array
    /// </summary>
    public string? Features { get; set; }

    /// <summary>
    /// Whether this tier is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Sort order for display
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// When the subscription tier was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the subscription tier was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property for user subscriptions
    /// </summary>
    public virtual ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();
}