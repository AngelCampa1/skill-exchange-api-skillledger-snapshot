using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.DTOs;

#region Request DTOs

/// <summary>
/// Request to create a new Stripe coupon.
/// </summary>
public class CreateCouponRequest
{
    /// <summary>
    /// Custom coupon ID. If not provided, Stripe will auto-generate one.
    /// Example: "launch_3mo_free"
    /// </summary>
    [StringLength(50, MinimumLength = 1)]
    public string? Id { get; set; }

    /// <summary>
    /// Name of the coupon displayed to customers on invoices.
    /// Example: "Launch Promotion - 3 Months Free"
    /// </summary>
    [Required]
    [StringLength(40, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Percentage off (0-100). Use 100 for completely free.
    /// Either PercentOff or AmountOffCents must be provided, but not both.
    /// </summary>
    [Range(0.01, 100)]
    public decimal? PercentOff { get; set; }

    /// <summary>
    /// Fixed amount off in cents. Use with Currency.
    /// Either PercentOff or AmountOffCents must be provided, but not both.
    /// </summary>
    [Range(1, long.MaxValue)]
    public long? AmountOffCents { get; set; }

    /// <summary>
    /// Three-letter ISO currency code. Required when using AmountOffCents.
    /// </summary>
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = "usd";

    /// <summary>
    /// How long the coupon applies: "once", "repeating", or "forever".
    /// - "once": Applies only to the first invoice
    /// - "repeating": Applies to multiple invoices (requires DurationInMonths)
    /// - "forever": Applies to all invoices indefinitely
    /// </summary>
    [Required]
    [RegularExpression("^(once|repeating|forever)$")]
    public string Duration { get; set; } = "once";

    /// <summary>
    /// Number of months the coupon applies. Required when Duration is "repeating".
    /// </summary>
    [Range(1, 120)]
    public int? DurationInMonths { get; set; }

    /// <summary>
    /// Maximum number of times this coupon can be redeemed across all customers.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int? MaxRedemptions { get; set; }

    /// <summary>
    /// Date after which the coupon can no longer be redeemed.
    /// </summary>
    public DateTime? RedeemBy { get; set; }

    /// <summary>
    /// List of Stripe Product IDs this coupon applies to.
    /// If empty, applies to all products.
    /// </summary>
    public List<string>? AppliesTo { get; set; }

    /// <summary>
    /// Optional metadata key-value pairs.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Request to create a new Stripe promotion code.
/// </summary>
public class CreatePromoCodeRequest
{
    /// <summary>
    /// The Stripe coupon ID this promotion code maps to.
    /// </summary>
    [Required]
    public string CouponId { get; set; } = string.Empty;

    /// <summary>
    /// Custom promotion code. If not provided, Stripe will auto-generate one.
    /// Example: "LAUNCH2024"
    /// </summary>
    [StringLength(50, MinimumLength = 1)]
    [RegularExpression("^[a-zA-Z0-9]*$", ErrorMessage = "Code must be alphanumeric only")]
    public string? Code { get; set; }

    /// <summary>
    /// Maximum number of times this promotion code can be redeemed.
    /// Cannot exceed the coupon's max_redemptions.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int? MaxRedemptions { get; set; }

    /// <summary>
    /// Date after which the promotion code can no longer be redeemed.
    /// Must be before the coupon's redeem_by date if set.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// If true, the code can only be used by customers who have never had a subscription.
    /// </summary>
    public bool FirstTimeTransactionOnly { get; set; }

    /// <summary>
    /// Minimum order amount in cents required to use this code.
    /// </summary>
    [Range(0, long.MaxValue)]
    public long? MinimumAmountCents { get; set; }

    /// <summary>
    /// Currency for minimum amount. Required if MinimumAmountCents is set.
    /// </summary>
    [StringLength(3, MinimumLength = 3)]
    public string? MinimumAmountCurrency { get; set; }

    /// <summary>
    /// Restrict this promotion code to a specific Stripe customer ID.
    /// </summary>
    public string? CustomerId { get; set; }

    /// <summary>
    /// Whether the promotion code is currently active.
    /// </summary>
    public bool Active { get; set; } = true;

    /// <summary>
    /// Optional metadata key-value pairs.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Request to update a promotion code.
/// </summary>
public class UpdatePromoCodeRequest
{
    /// <summary>
    /// Whether the promotion code is active.
    /// </summary>
    public bool? Active { get; set; }

    /// <summary>
    /// Updated metadata.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}

#endregion

#region Response DTOs

/// <summary>
/// Result of a Stripe coupon operation.
/// </summary>
public class StripeCouponResult
{
    /// <summary>
    /// Stripe coupon ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the coupon.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Percentage off (0-100), or null if amount-based discount.
    /// </summary>
    public decimal? PercentOff { get; set; }

    /// <summary>
    /// Amount off in cents, or null if percentage-based discount.
    /// </summary>
    public long? AmountOff { get; set; }

    /// <summary>
    /// Currency for amount-based discounts.
    /// </summary>
    public string? Currency { get; set; }

    /// <summary>
    /// Duration type: "once", "repeating", or "forever".
    /// </summary>
    public string Duration { get; set; } = string.Empty;

    /// <summary>
    /// Number of months for "repeating" duration.
    /// </summary>
    public int? DurationInMonths { get; set; }

    /// <summary>
    /// Maximum redemptions allowed, or null for unlimited.
    /// </summary>
    public int? MaxRedemptions { get; set; }

    /// <summary>
    /// Number of times this coupon has been redeemed.
    /// </summary>
    public int TimesRedeemed { get; set; }

    /// <summary>
    /// Remaining redemptions available.
    /// </summary>
    public int? RemainingRedemptions => MaxRedemptions.HasValue
        ? MaxRedemptions.Value - TimesRedeemed
        : null;

    /// <summary>
    /// Whether the coupon is still valid (not deleted/expired).
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Date after which the coupon cannot be redeemed.
    /// </summary>
    public DateTime? RedeemBy { get; set; }

    /// <summary>
    /// When the coupon was created.
    /// </summary>
    public DateTime Created { get; set; }

    /// <summary>
    /// Product IDs this coupon applies to (empty = all products).
    /// </summary>
    public List<string> AppliesTo { get; set; } = new();

    /// <summary>
    /// Custom metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Human-readable description of the discount.
    /// </summary>
    public string DiscountDescription
    {
        get
        {
            var discount = PercentOff.HasValue
                ? $"{PercentOff:F0}% off"
                : $"${(AmountOff ?? 0) / 100m:F2} off";

            var duration = Duration switch
            {
                "once" => "first payment",
                "forever" => "forever",
                "repeating" => $"for {DurationInMonths} months",
                _ => ""
            };

            return $"{discount} {duration}".Trim();
        }
    }
}

/// <summary>
/// Result of a Stripe promotion code operation.
/// </summary>
public class StripePromoCodeResult
{
    /// <summary>
    /// Stripe promotion code ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The customer-facing code string.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// The coupon this promotion code maps to.
    /// </summary>
    public StripeCouponResult? Coupon { get; set; }

    /// <summary>
    /// Whether the promotion code is currently active.
    /// </summary>
    public bool Active { get; set; }

    /// <summary>
    /// Maximum redemptions allowed for this code.
    /// </summary>
    public int? MaxRedemptions { get; set; }

    /// <summary>
    /// Number of times this code has been redeemed.
    /// </summary>
    public int TimesRedeemed { get; set; }

    /// <summary>
    /// Remaining redemptions available.
    /// </summary>
    public int? RemainingRedemptions => MaxRedemptions.HasValue
        ? MaxRedemptions.Value - TimesRedeemed
        : null;

    /// <summary>
    /// When this promotion code expires.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Whether this code is restricted to first-time customers.
    /// </summary>
    public bool FirstTimeTransactionOnly { get; set; }

    /// <summary>
    /// Minimum order amount required (in cents).
    /// </summary>
    public long? MinimumAmount { get; set; }

    /// <summary>
    /// Currency for minimum amount.
    /// </summary>
    public string? MinimumAmountCurrency { get; set; }

    /// <summary>
    /// Customer ID this code is restricted to, if any.
    /// </summary>
    public string? CustomerId { get; set; }

    /// <summary>
    /// When the promotion code was created.
    /// </summary>
    public DateTime Created { get; set; }

    /// <summary>
    /// Custom metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Whether this code is currently usable.
    /// </summary>
    public bool IsUsable
    {
        get
        {
            if (!Active) return false;
            if (ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow) return false;
            if (MaxRedemptions.HasValue && TimesRedeemed >= MaxRedemptions.Value) return false;
            if (Coupon != null && !Coupon.IsValid) return false;
            return true;
        }
    }
}

/// <summary>
/// Result of validating a promotion code.
/// </summary>
public class PromoValidationResult
{
    /// <summary>
    /// Whether the promotion code is valid and can be used.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Error message if validation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Error code for programmatic handling.
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// The validated promotion code details (if valid).
    /// </summary>
    public StripePromoCodeResult? PromoCode { get; set; }

    /// <summary>
    /// Human-readable description of the discount.
    /// </summary>
    public string? DiscountDescription { get; set; }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static PromoValidationResult Success(StripePromoCodeResult promoCode) => new()
    {
        IsValid = true,
        PromoCode = promoCode,
        DiscountDescription = promoCode.Coupon?.DiscountDescription
    };

    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    public static PromoValidationResult Failure(string errorMessage, string errorCode) => new()
    {
        IsValid = false,
        ErrorMessage = errorMessage,
        ErrorCode = errorCode
    };
}

/// <summary>
/// Statistics for a specific coupon.
/// </summary>
public class CouponStatsResult
{
    /// <summary>
    /// Coupon ID.
    /// </summary>
    public string CouponId { get; set; } = string.Empty;

    /// <summary>
    /// Coupon name.
    /// </summary>
    public string? CouponName { get; set; }

    /// <summary>
    /// Total times redeemed.
    /// </summary>
    public int TotalRedemptions { get; set; }

    /// <summary>
    /// Maximum redemptions allowed.
    /// </summary>
    public int? MaxRedemptions { get; set; }

    /// <summary>
    /// Remaining redemptions.
    /// </summary>
    public int? RemainingRedemptions => MaxRedemptions.HasValue
        ? MaxRedemptions.Value - TotalRedemptions
        : null;

    /// <summary>
    /// Percentage of redemptions used.
    /// </summary>
    public decimal? UsagePercentage => MaxRedemptions.HasValue && MaxRedemptions.Value > 0
        ? (decimal)TotalRedemptions / MaxRedemptions.Value * 100
        : null;

    /// <summary>
    /// Number of active promotion codes for this coupon.
    /// </summary>
    public int ActivePromotionCodes { get; set; }

    /// <summary>
    /// When the coupon was created.
    /// </summary>
    public DateTime Created { get; set; }

    /// <summary>
    /// When the coupon expires.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Whether the coupon is still valid.
    /// </summary>
    public bool IsValid { get; set; }
}

/// <summary>
/// Overall promotion statistics.
/// </summary>
public class PromotionStatsResult
{
    /// <summary>
    /// Total number of coupons.
    /// </summary>
    public int TotalCoupons { get; set; }

    /// <summary>
    /// Number of active coupons.
    /// </summary>
    public int ActiveCoupons { get; set; }

    /// <summary>
    /// Total number of promotion codes.
    /// </summary>
    public int TotalPromotionCodes { get; set; }

    /// <summary>
    /// Number of active promotion codes.
    /// </summary>
    public int ActivePromotionCodes { get; set; }

    /// <summary>
    /// Total redemptions across all coupons.
    /// </summary>
    public int TotalRedemptions { get; set; }

    /// <summary>
    /// Stats for individual coupons.
    /// </summary>
    public List<CouponStatsResult> CouponStats { get; set; } = new();
}

#endregion
