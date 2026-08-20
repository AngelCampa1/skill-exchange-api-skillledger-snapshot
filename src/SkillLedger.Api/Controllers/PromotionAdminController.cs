using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Interfaces;

namespace SkillLedger.Api.Controllers;

/// <summary>
/// Admin controller for managing Stripe coupons and promotion codes.
/// These endpoints wrap Stripe's native API for promotion management.
/// </summary>
[ApiController]
[Route("api/admin/promotions")]
[Authorize(Roles = "Admin")]
public class PromotionAdminController : BaseApiController
{
    private readonly IStripePromotionService _promotionService;
    private readonly ILogger<PromotionAdminController> _logger;
    private readonly IAuditLogService _auditLogService;

    public PromotionAdminController(
        IStripePromotionService promotionService,
        ILogger<PromotionAdminController> logger,
        IAuditLogService auditLogService)
    {
        _promotionService = promotionService;
        _logger = logger;
        _auditLogService = auditLogService;
    }

    #region Coupon Endpoints

    /// <summary>
    /// Create a new Stripe coupon.
    /// </summary>
    /// <param name="request">Coupon creation request</param>
    /// <returns>Created coupon details</returns>
    [HttpPost("coupons")]
    [ProducesResponseType(typeof(StripeCouponResult), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<StripeCouponResult>> CreateCoupon([FromBody] CreateCouponRequest request)
    {
        try
        {
            // Validate discount type
            if (!request.PercentOff.HasValue && !request.AmountOffCents.HasValue)
            {
                return BadRequest("Either PercentOff or AmountOffCents must be provided");
            }

            if (request.PercentOff.HasValue && request.AmountOffCents.HasValue)
            {
                return BadRequest("Only one of PercentOff or AmountOffCents can be provided");
            }

            // Validate duration
            if (request.Duration == "repeating" && !request.DurationInMonths.HasValue)
            {
                return BadRequest("DurationInMonths is required when Duration is 'repeating'");
            }

            var coupon = await _promotionService.CreateCouponAsync(request);

            await _auditLogService.LogEventAsync(
                GetCurrentUserId(),
                "COUPON_CREATED",
                GetClientIPAddress(),
                "PromotionAdminController",
                true,
                $"Created coupon: {coupon.Id} - {coupon.Name}");

            _logger.LogInformation("Admin {UserId} created coupon {CouponId}: {CouponName}",
                GetCurrentUserId(), coupon.Id, coupon.Name);

            return CreatedAtAction(nameof(GetCoupon), new { couponId = coupon.Id }, coupon);
        }
        catch (Stripe.StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating coupon");
            return BadRequest($"Stripe error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating coupon");
            return StatusCode(500, "An error occurred while creating the coupon");
        }
    }

    /// <summary>
    /// Get all coupons.
    /// </summary>
    /// <param name="activeOnly">If true, only return active coupons</param>
    /// <param name="limit">Maximum number of coupons to return (max 100)</param>
    /// <returns>List of coupons</returns>
    [HttpGet("coupons")]
    [ProducesResponseType(typeof(IReadOnlyList<StripeCouponResult>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<IReadOnlyList<StripeCouponResult>>> ListCoupons(
        [FromQuery] bool activeOnly = true,
        [FromQuery] int limit = 100)
    {
        try
        {
            var coupons = await _promotionService.ListCouponsAsync(activeOnly, limit);
            return Ok(coupons);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing coupons");
            return StatusCode(500, "An error occurred while listing coupons");
        }
    }

    /// <summary>
    /// Get a coupon by ID.
    /// </summary>
    /// <param name="couponId">Stripe coupon ID</param>
    /// <returns>Coupon details</returns>
    [HttpGet("coupons/{couponId}")]
    [ProducesResponseType(typeof(StripeCouponResult), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<StripeCouponResult>> GetCoupon(string couponId)
    {
        try
        {
            var coupon = await _promotionService.GetCouponAsync(couponId);
            if (coupon == null)
            {
                return NotFound($"Coupon not found: {couponId}");
            }

            return Ok(coupon);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting coupon {CouponId}", couponId);
            return StatusCode(500, "An error occurred while retrieving the coupon");
        }
    }

    /// <summary>
    /// Get statistics for a coupon.
    /// </summary>
    /// <param name="couponId">Stripe coupon ID</param>
    /// <returns>Coupon statistics</returns>
    [HttpGet("coupons/{couponId}/stats")]
    [ProducesResponseType(typeof(CouponStatsResult), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<CouponStatsResult>> GetCouponStats(string couponId)
    {
        try
        {
            var stats = await _promotionService.GetCouponStatsAsync(couponId);
            return Ok(stats);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound($"Coupon not found: {couponId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting coupon stats {CouponId}", couponId);
            return StatusCode(500, "An error occurred while retrieving coupon statistics");
        }
    }

    /// <summary>
    /// Deactivate (delete) a coupon.
    /// Note: This does not affect customers who have already applied the coupon.
    /// </summary>
    /// <param name="couponId">Stripe coupon ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("coupons/{couponId}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> DeactivateCoupon(string couponId)
    {
        try
        {
            var success = await _promotionService.DeactivateCouponAsync(couponId);
            if (!success)
            {
                return NotFound($"Coupon not found: {couponId}");
            }

            await _auditLogService.LogEventAsync(
                GetCurrentUserId(),
                "COUPON_DEACTIVATED",
                GetClientIPAddress(),
                "PromotionAdminController",
                true,
                $"Deactivated coupon: {couponId}");

            _logger.LogInformation("Admin {UserId} deactivated coupon {CouponId}",
                GetCurrentUserId(), couponId);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating coupon {CouponId}", couponId);
            return StatusCode(500, "An error occurred while deactivating the coupon");
        }
    }

    #endregion

    #region Promotion Code Endpoints

    /// <summary>
    /// Create a new promotion code for an existing coupon.
    /// </summary>
    /// <param name="request">Promotion code creation request</param>
    /// <returns>Created promotion code details</returns>
    [HttpPost("codes")]
    [ProducesResponseType(typeof(StripePromoCodeResult), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<StripePromoCodeResult>> CreatePromotionCode([FromBody] CreatePromoCodeRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.CouponId))
            {
                return BadRequest("CouponId is required");
            }

            var promoCode = await _promotionService.CreatePromotionCodeAsync(request);

            await _auditLogService.LogEventAsync(
                GetCurrentUserId(),
                "PROMO_CODE_CREATED",
                GetClientIPAddress(),
                "PromotionAdminController",
                true,
                $"Created promotion code: {promoCode.Code} for coupon {request.CouponId}");

            _logger.LogInformation("Admin {UserId} created promotion code {Code} for coupon {CouponId}",
                GetCurrentUserId(), promoCode.Code, request.CouponId);

            return CreatedAtAction(nameof(GetPromotionCode), new { code = promoCode.Code }, promoCode);
        }
        catch (Stripe.StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating promotion code");
            return BadRequest($"Stripe error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating promotion code");
            return StatusCode(500, "An error occurred while creating the promotion code");
        }
    }

    /// <summary>
    /// Get all promotion codes.
    /// </summary>
    /// <param name="couponId">Optional coupon ID to filter by</param>
    /// <param name="activeOnly">If true, only return active codes</param>
    /// <param name="limit">Maximum number of codes to return (max 100)</param>
    /// <returns>List of promotion codes</returns>
    [HttpGet("codes")]
    [ProducesResponseType(typeof(IReadOnlyList<StripePromoCodeResult>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<IReadOnlyList<StripePromoCodeResult>>> ListPromotionCodes(
        [FromQuery] string? couponId = null,
        [FromQuery] bool activeOnly = true,
        [FromQuery] int limit = 100)
    {
        try
        {
            var codes = await _promotionService.ListPromotionCodesAsync(couponId, activeOnly, limit);
            return Ok(codes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing promotion codes");
            return StatusCode(500, "An error occurred while listing promotion codes");
        }
    }

    /// <summary>
    /// Get a promotion code by code string.
    /// </summary>
    /// <param name="code">Promotion code string (e.g., "LAUNCH2024")</param>
    /// <returns>Promotion code details</returns>
    [HttpGet("codes/{code}")]
    [ProducesResponseType(typeof(StripePromoCodeResult), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<StripePromoCodeResult>> GetPromotionCode(string code)
    {
        try
        {
            var promoCode = await _promotionService.GetPromotionCodeByCodeAsync(code);
            if (promoCode == null)
            {
                return NotFound($"Promotion code not found: {code}");
            }

            return Ok(promoCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting promotion code {Code}", code);
            return StatusCode(500, "An error occurred while retrieving the promotion code");
        }
    }

    /// <summary>
    /// Deactivate a promotion code.
    /// </summary>
    /// <param name="promoCodeId">Stripe promotion code ID (not the code string)</param>
    /// <returns>Success status</returns>
    [HttpDelete("codes/{promoCodeId}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> DeactivatePromotionCode(string promoCodeId)
    {
        try
        {
            var success = await _promotionService.DeactivatePromotionCodeAsync(promoCodeId);
            if (!success)
            {
                return NotFound($"Promotion code not found: {promoCodeId}");
            }

            await _auditLogService.LogEventAsync(
                GetCurrentUserId(),
                "PROMO_CODE_DEACTIVATED",
                GetClientIPAddress(),
                "PromotionAdminController",
                true,
                $"Deactivated promotion code: {promoCodeId}");

            _logger.LogInformation("Admin {UserId} deactivated promotion code {PromoCodeId}",
                GetCurrentUserId(), promoCodeId);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating promotion code {PromoCodeId}", promoCodeId);
            return StatusCode(500, "An error occurred while deactivating the promotion code");
        }
    }

    #endregion

    #region Validation Endpoints

    /// <summary>
    /// Validate a promotion code.
    /// This can be used to check if a code is valid before checkout.
    /// </summary>
    /// <param name="code">Promotion code to validate</param>
    /// <returns>Validation result</returns>
    [HttpGet("validate/{code}")]
    [AllowAnonymous] // Allow unauthenticated validation for checkout preview
    [ProducesResponseType(typeof(PromoValidationResult), 200)]
    public async Task<ActionResult<PromoValidationResult>> ValidatePromotionCode(string code)
    {
        try
        {
            var userId = TryGetCurrentUserId() ?? Guid.Empty;
            var result = await _promotionService.ValidatePromotionCodeAsync(code, userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating promotion code {Code}", code);
            return Ok(PromoValidationResult.Failure(
                "An error occurred while validating the promotion code",
                "VALIDATION_ERROR"));
        }
    }

    #endregion

    #region Statistics Endpoints

    /// <summary>
    /// Get overall promotion statistics.
    /// </summary>
    /// <returns>Aggregate promotion statistics</returns>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(PromotionStatsResult), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<PromotionStatsResult>> GetPromotionStats()
    {
        try
        {
            var stats = await _promotionService.GetPromotionStatsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting promotion stats");
            return StatusCode(500, "An error occurred while retrieving promotion statistics");
        }
    }

    #endregion
}
