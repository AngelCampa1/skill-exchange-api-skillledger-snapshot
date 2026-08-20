using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Configuration;

namespace SkillLedger.Infrastructure.Services;

/// <summary>
/// Service for managing Stripe coupons and promotion codes.
/// Wraps Stripe's native APIs to provide a clean interface for promotion management.
/// </summary>
public class StripePromotionService : IStripePromotionService
{
    private readonly ILogger<StripePromotionService> _logger;
    private readonly StripeSettings _stripeSettings;
    private readonly CouponService _couponService;
    private readonly PromotionCodeService _promotionCodeService;

    public StripePromotionService(
        ILogger<StripePromotionService> logger,
        IOptions<StripeSettings> stripeSettings)
    {
        _logger = logger;
        _stripeSettings = stripeSettings.Value;

        // Configure Stripe API key
        StripeConfiguration.ApiKey = _stripeSettings.SecretKey;

        // Initialize Stripe services
        _couponService = new CouponService();
        _promotionCodeService = new PromotionCodeService();
    }

    #region Coupon Management

    /// <inheritdoc />
    public async Task<StripeCouponResult> CreateCouponAsync(CreateCouponRequest request)
    {
        try
        {
            _logger.LogInformation("Creating Stripe coupon: {Name}", request.Name);

            var options = new CouponCreateOptions
            {
                Id = request.Id,
                Name = request.Name,
                Duration = request.Duration,
                DurationInMonths = request.Duration == "repeating" ? request.DurationInMonths : null,
                MaxRedemptions = request.MaxRedemptions,
                RedeemBy = request.RedeemBy,
                Metadata = request.Metadata ?? new Dictionary<string, string>()
            };

            // Set discount type (percent or amount)
            if (request.PercentOff.HasValue)
            {
                options.PercentOff = request.PercentOff.Value;
            }
            else if (request.AmountOffCents.HasValue)
            {
                options.AmountOff = request.AmountOffCents.Value;
                options.Currency = request.Currency.ToLower();
            }
            else
            {
                throw new ArgumentException("Either PercentOff or AmountOffCents must be provided");
            }

            // Set product restrictions if specified
            if (request.AppliesTo?.Any() == true)
            {
                options.AppliesTo = new CouponAppliesToOptions
                {
                    Products = request.AppliesTo
                };
            }

            var coupon = await _couponService.CreateAsync(options);

            _logger.LogInformation("Created Stripe coupon {CouponId}: {Name}", coupon.Id, coupon.Name);

            return MapToCouponResult(coupon);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to create Stripe coupon: {Name}", request.Name);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<StripeCouponResult?> GetCouponAsync(string couponId)
    {
        try
        {
            var coupon = await _couponService.GetAsync(couponId);
            return MapToCouponResult(coupon);
        }
        catch (StripeException ex) when (ex.StripeError?.Code == "resource_missing")
        {
            _logger.LogWarning("Coupon not found: {CouponId}", couponId);
            return null;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to get Stripe coupon: {CouponId}", couponId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StripeCouponResult>> ListCouponsAsync(bool activeOnly = true, int limit = 100)
    {
        try
        {
            var options = new CouponListOptions
            {
                Limit = Math.Min(limit, 100) // Stripe max is 100
            };

            var coupons = await _couponService.ListAsync(options);
            var results = coupons.Data
                .Where(c => !activeOnly || c.Valid)
                .Select(MapToCouponResult)
                .ToList();

            return results;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to list Stripe coupons");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeactivateCouponAsync(string couponId)
    {
        try
        {
            _logger.LogInformation("Deactivating Stripe coupon: {CouponId}", couponId);

            await _couponService.DeleteAsync(couponId);

            _logger.LogInformation("Deactivated Stripe coupon: {CouponId}", couponId);
            return true;
        }
        catch (StripeException ex) when (ex.StripeError?.Code == "resource_missing")
        {
            _logger.LogWarning("Coupon not found for deactivation: {CouponId}", couponId);
            return false;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to deactivate Stripe coupon: {CouponId}", couponId);
            throw;
        }
    }

    #endregion

    #region Promotion Code Management

    /// <inheritdoc />
    public async Task<StripePromoCodeResult> CreatePromotionCodeAsync(CreatePromoCodeRequest request)
    {
        try
        {
            _logger.LogInformation("Creating Stripe promotion code for coupon: {CouponId}", request.CouponId);

            var options = new PromotionCodeCreateOptions
            {
                // In Stripe.NET v49, coupon is set via Promotion.Coupon
                Promotion = new PromotionCodePromotionOptions
                {
                    Coupon = request.CouponId
                },
                Code = request.Code,
                MaxRedemptions = request.MaxRedemptions,
                ExpiresAt = request.ExpiresAt,
                Active = request.Active,
                Metadata = request.Metadata ?? new Dictionary<string, string>(),
                Restrictions = new PromotionCodeRestrictionsOptions
                {
                    FirstTimeTransaction = request.FirstTimeTransactionOnly
                }
            };

            // Set minimum amount if specified
            if (request.MinimumAmountCents.HasValue)
            {
                options.Restrictions.MinimumAmount = request.MinimumAmountCents.Value;
                options.Restrictions.MinimumAmountCurrency = request.MinimumAmountCurrency ?? "usd";
            }

            // Set customer restriction if specified
            if (!string.IsNullOrEmpty(request.CustomerId))
            {
                options.Customer = request.CustomerId;
            }

            var promoCode = await _promotionCodeService.CreateAsync(options);

            _logger.LogInformation("Created Stripe promotion code {Code} (ID: {Id})", promoCode.Code, promoCode.Id);

            return await MapToPromoCodeResultAsync(promoCode);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to create Stripe promotion code for coupon: {CouponId}", request.CouponId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<StripePromoCodeResult?> GetPromotionCodeByCodeAsync(string code)
    {
        try
        {
            // Search for promotion code by code string
            var options = new PromotionCodeListOptions
            {
                Code = code,
                Limit = 1,
                Expand = new List<string> { "data.coupon" }
            };

            var promoCodes = await _promotionCodeService.ListAsync(options);
            var promoCode = promoCodes.Data.FirstOrDefault();

            if (promoCode == null)
            {
                _logger.LogWarning("Promotion code not found: {Code}", code);
                return null;
            }

            return await MapToPromoCodeResultAsync(promoCode);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to get Stripe promotion code: {Code}", code);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<StripePromoCodeResult?> GetPromotionCodeByIdAsync(string promoCodeId)
    {
        try
        {
            var promoCode = await _promotionCodeService.GetAsync(promoCodeId);
            return await MapToPromoCodeResultAsync(promoCode);
        }
        catch (StripeException ex) when (ex.StripeError?.Code == "resource_missing")
        {
            _logger.LogWarning("Promotion code not found by ID: {PromoCodeId}", promoCodeId);
            return null;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to get Stripe promotion code by ID: {PromoCodeId}", promoCodeId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StripePromoCodeResult>> ListPromotionCodesAsync(
        string? couponId = null,
        bool activeOnly = true,
        int limit = 100)
    {
        try
        {
            var options = new PromotionCodeListOptions
            {
                Limit = Math.Min(limit, 100),
                Active = activeOnly ? true : null,
                Expand = new List<string> { "data.coupon" }
            };

            if (!string.IsNullOrEmpty(couponId))
            {
                options.Coupon = couponId;
            }

            var promoCodes = await _promotionCodeService.ListAsync(options);
            var results = new List<StripePromoCodeResult>();

            foreach (var promoCode in promoCodes.Data)
            {
                results.Add(await MapToPromoCodeResultAsync(promoCode));
            }

            return results;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to list Stripe promotion codes");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeactivatePromotionCodeAsync(string promoCodeId)
    {
        try
        {
            _logger.LogInformation("Deactivating Stripe promotion code: {PromoCodeId}", promoCodeId);

            var options = new PromotionCodeUpdateOptions
            {
                Active = false
            };

            await _promotionCodeService.UpdateAsync(promoCodeId, options);

            _logger.LogInformation("Deactivated Stripe promotion code: {PromoCodeId}", promoCodeId);
            return true;
        }
        catch (StripeException ex) when (ex.StripeError?.Code == "resource_missing")
        {
            _logger.LogWarning("Promotion code not found for deactivation: {PromoCodeId}", promoCodeId);
            return false;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to deactivate Stripe promotion code: {PromoCodeId}", promoCodeId);
            throw;
        }
    }

    #endregion

    #region Validation

    /// <inheritdoc />
    public async Task<PromoValidationResult> ValidatePromotionCodeAsync(string code, Guid userId)
    {
        try
        {
            var promoCode = await GetPromotionCodeByCodeAsync(code);

            if (promoCode == null)
            {
                return PromoValidationResult.Failure(
                    "Promotion code not found",
                    "CODE_NOT_FOUND");
            }

            if (!promoCode.Active)
            {
                return PromoValidationResult.Failure(
                    "This promotion code is no longer active",
                    "CODE_INACTIVE");
            }

            if (promoCode.ExpiresAt.HasValue && promoCode.ExpiresAt.Value < DateTime.UtcNow)
            {
                return PromoValidationResult.Failure(
                    "This promotion code has expired",
                    "CODE_EXPIRED");
            }

            if (promoCode.MaxRedemptions.HasValue && promoCode.TimesRedeemed >= promoCode.MaxRedemptions.Value)
            {
                return PromoValidationResult.Failure(
                    "This promotion code has reached its maximum redemptions",
                    "CODE_MAX_REDEMPTIONS");
            }

            // Check if the underlying coupon is still valid
            if (promoCode.Coupon != null && !promoCode.Coupon.IsValid)
            {
                return PromoValidationResult.Failure(
                    "The coupon associated with this code is no longer valid",
                    "COUPON_INVALID");
            }

            // Note: First-time transaction validation is handled by Stripe at checkout
            // We can't easily validate it here without checking the customer's subscription history

            return PromoValidationResult.Success(promoCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating promotion code: {Code}", code);
            return PromoValidationResult.Failure(
                "An error occurred while validating the promotion code",
                "VALIDATION_ERROR");
        }
    }

    #endregion

    #region Statistics

    /// <inheritdoc />
    public async Task<CouponStatsResult> GetCouponStatsAsync(string couponId)
    {
        try
        {
            var coupon = await GetCouponAsync(couponId);
            if (coupon == null)
            {
                throw new InvalidOperationException($"Coupon not found: {couponId}");
            }

            var promoCodes = await ListPromotionCodesAsync(couponId, activeOnly: false);

            return new CouponStatsResult
            {
                CouponId = coupon.Id,
                CouponName = coupon.Name,
                TotalRedemptions = coupon.TimesRedeemed,
                MaxRedemptions = coupon.MaxRedemptions,
                ActivePromotionCodes = promoCodes.Count(pc => pc.Active),
                Created = coupon.Created,
                ExpiresAt = coupon.RedeemBy,
                IsValid = coupon.IsValid
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get coupon stats: {CouponId}", couponId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<PromotionStatsResult> GetPromotionStatsAsync()
    {
        try
        {
            var coupons = await ListCouponsAsync(activeOnly: false);
            var promoCodes = await ListPromotionCodesAsync(activeOnly: false);

            var couponStats = new List<CouponStatsResult>();
            foreach (var coupon in coupons)
            {
                var codesForCoupon = promoCodes.Where(pc => pc.Coupon?.Id == coupon.Id).ToList();
                couponStats.Add(new CouponStatsResult
                {
                    CouponId = coupon.Id,
                    CouponName = coupon.Name,
                    TotalRedemptions = coupon.TimesRedeemed,
                    MaxRedemptions = coupon.MaxRedemptions,
                    ActivePromotionCodes = codesForCoupon.Count(pc => pc.Active),
                    Created = coupon.Created,
                    ExpiresAt = coupon.RedeemBy,
                    IsValid = coupon.IsValid
                });
            }

            return new PromotionStatsResult
            {
                TotalCoupons = coupons.Count,
                ActiveCoupons = coupons.Count(c => c.IsValid),
                TotalPromotionCodes = promoCodes.Count,
                ActivePromotionCodes = promoCodes.Count(pc => pc.Active),
                TotalRedemptions = coupons.Sum(c => c.TimesRedeemed),
                CouponStats = couponStats
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get promotion stats");
            throw;
        }
    }

    #endregion

    #region Private Helpers

    private static StripeCouponResult MapToCouponResult(Coupon coupon)
    {
        return new StripeCouponResult
        {
            Id = coupon.Id,
            Name = coupon.Name,
            PercentOff = coupon.PercentOff,
            AmountOff = coupon.AmountOff,
            Currency = coupon.Currency,
            Duration = coupon.Duration,
            DurationInMonths = (int?)coupon.DurationInMonths,
            MaxRedemptions = (int?)coupon.MaxRedemptions,
            TimesRedeemed = (int)coupon.TimesRedeemed,
            IsValid = coupon.Valid,
            RedeemBy = coupon.RedeemBy,
            Created = coupon.Created,
            AppliesTo = coupon.AppliesTo?.Products?.ToList() ?? new List<string>(),
            Metadata = coupon.Metadata?.ToDictionary(kv => kv.Key, kv => kv.Value) ?? new Dictionary<string, string>()
        };
    }

    private async Task<StripePromoCodeResult> MapToPromoCodeResultAsync(PromotionCode promoCode)
    {
        // In Stripe.NET v49+, coupon is accessed via Promotion.Coupon
        StripeCouponResult? couponResult = null;

        // Try to get from expanded Promotion.Coupon object first
        var promotion = promoCode.Promotion;
        if (promotion?.Coupon != null)
        {
            couponResult = MapToCouponResult(promotion.Coupon);
        }
        else if (!string.IsNullOrEmpty(promotion?.CouponId))
        {
            // Coupon ID is available but not expanded - fetch it
            couponResult = await GetCouponAsync(promotion.CouponId);
        }

        return new StripePromoCodeResult
        {
            Id = promoCode.Id,
            Code = promoCode.Code,
            Coupon = couponResult,
            Active = promoCode.Active,
            MaxRedemptions = (int?)promoCode.MaxRedemptions,
            TimesRedeemed = (int)promoCode.TimesRedeemed,
            ExpiresAt = promoCode.ExpiresAt,
            FirstTimeTransactionOnly = promoCode.Restrictions?.FirstTimeTransaction ?? false,
            MinimumAmount = promoCode.Restrictions?.MinimumAmount,
            MinimumAmountCurrency = promoCode.Restrictions?.MinimumAmountCurrency,
            CustomerId = promoCode.CustomerId,
            Created = promoCode.Created,
            Metadata = promoCode.Metadata?.ToDictionary(kv => kv.Key, kv => kv.Value) ?? new Dictionary<string, string>()
        };
    }

    #endregion
}
