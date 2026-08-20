using SkillLedger.Core.DTOs;

namespace SkillLedger.Core.Interfaces;

/// <summary>
/// Service for managing Stripe coupons and promotion codes.
/// This is a thin wrapper around Stripe's native coupon/promotion code APIs.
/// </summary>
public interface IStripePromotionService
{
    #region Coupon Management

    /// <summary>
    /// Creates a new coupon in Stripe.
    /// </summary>
    /// <param name="request">Coupon creation request</param>
    /// <returns>Created coupon details</returns>
    Task<StripeCouponResult> CreateCouponAsync(CreateCouponRequest request);

    /// <summary>
    /// Retrieves a coupon by ID from Stripe.
    /// </summary>
    /// <param name="couponId">Stripe coupon ID</param>
    /// <returns>Coupon details or null if not found</returns>
    Task<StripeCouponResult?> GetCouponAsync(string couponId);

    /// <summary>
    /// Lists all coupons from Stripe.
    /// </summary>
    /// <param name="activeOnly">If true, only return active coupons</param>
    /// <param name="limit">Maximum number of coupons to return (default 100)</param>
    /// <returns>List of coupons</returns>
    Task<IReadOnlyList<StripeCouponResult>> ListCouponsAsync(bool activeOnly = true, int limit = 100);

    /// <summary>
    /// Deactivates (deletes) a coupon in Stripe.
    /// Note: Deleting a coupon does not affect any customers who have already applied the coupon.
    /// </summary>
    /// <param name="couponId">Stripe coupon ID</param>
    /// <returns>True if successfully deleted</returns>
    Task<bool> DeactivateCouponAsync(string couponId);

    #endregion

    #region Promotion Code Management

    /// <summary>
    /// Creates a new promotion code linked to an existing coupon.
    /// </summary>
    /// <param name="request">Promotion code creation request</param>
    /// <returns>Created promotion code details</returns>
    Task<StripePromoCodeResult> CreatePromotionCodeAsync(CreatePromoCodeRequest request);

    /// <summary>
    /// Retrieves a promotion code by code string.
    /// </summary>
    /// <param name="code">The promotion code string (e.g., "LAUNCH2024")</param>
    /// <returns>Promotion code details or null if not found</returns>
    Task<StripePromoCodeResult?> GetPromotionCodeByCodeAsync(string code);

    /// <summary>
    /// Retrieves a promotion code by Stripe ID.
    /// </summary>
    /// <param name="promoCodeId">Stripe promotion code ID</param>
    /// <returns>Promotion code details or null if not found</returns>
    Task<StripePromoCodeResult?> GetPromotionCodeByIdAsync(string promoCodeId);

    /// <summary>
    /// Lists promotion codes, optionally filtered by coupon.
    /// </summary>
    /// <param name="couponId">Optional coupon ID to filter by</param>
    /// <param name="activeOnly">If true, only return active promotion codes</param>
    /// <param name="limit">Maximum number of promotion codes to return (default 100)</param>
    /// <returns>List of promotion codes</returns>
    Task<IReadOnlyList<StripePromoCodeResult>> ListPromotionCodesAsync(
        string? couponId = null,
        bool activeOnly = true,
        int limit = 100);

    /// <summary>
    /// Deactivates a promotion code in Stripe.
    /// </summary>
    /// <param name="promoCodeId">Stripe promotion code ID</param>
    /// <returns>True if successfully deactivated</returns>
    Task<bool> DeactivatePromotionCodeAsync(string promoCodeId);

    #endregion

    #region Validation

    /// <summary>
    /// Validates a promotion code for a specific user.
    /// Checks if the code exists, is active, hasn't reached max redemptions, etc.
    /// </summary>
    /// <param name="code">The promotion code to validate</param>
    /// <param name="userId">The user attempting to use the code</param>
    /// <returns>Validation result with details</returns>
    Task<PromoValidationResult> ValidatePromotionCodeAsync(string code, Guid userId);

    #endregion

    #region Statistics

    /// <summary>
    /// Gets usage statistics for a coupon.
    /// </summary>
    /// <param name="couponId">Stripe coupon ID</param>
    /// <returns>Coupon statistics</returns>
    Task<CouponStatsResult> GetCouponStatsAsync(string couponId);

    /// <summary>
    /// Gets overall promotion statistics.
    /// </summary>
    /// <returns>Aggregate promotion statistics</returns>
    Task<PromotionStatsResult> GetPromotionStatsAsync();

    #endregion
}
