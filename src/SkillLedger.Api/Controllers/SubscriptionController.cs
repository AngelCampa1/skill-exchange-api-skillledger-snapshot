using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Services;
using System.Security.Claims;

namespace SkillLedger.Api.Controllers;

/// <summary>
/// Subscription management API controller
/// Handles subscription lifecycle, tier changes, and billing operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Route("subscription")] // Legacy route alias for frontend compatibility
[Authorize]
[EnableRateLimiting("SubscriptionPolicy")]
public class SubscriptionController : BaseApiController
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<SubscriptionController> _logger;
    private readonly Core.Interfaces.IIdempotencyService _idempotencyService;

    public SubscriptionController(
        ISubscriptionService subscriptionService,
        IPaymentService paymentService,
        ILogger<SubscriptionController> logger,
        Core.Interfaces.IIdempotencyService idempotencyService)
    {
        _subscriptionService = subscriptionService;
        _paymentService = paymentService;
        _logger = logger;
        _idempotencyService = idempotencyService;
    }

    /// <summary>
    /// Get available subscription tiers
    /// </summary>
    /// <returns>List of available subscription tiers</returns>
    [HttpGet("tiers")]
    [ProducesResponseType(typeof(List<SubscriptionTierDto>), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<SubscriptionTierDto>>> GetSubscriptionTiers()
    {
        try
        {
            var tiers = await _subscriptionService.GetSubscriptionTiersAsync();
            var tierDtos = tiers.Select(MapToSubscriptionTierDto).ToList();

            return Ok(tierDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subscription tiers");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get user's payment methods
    /// </summary>
    /// <returns>List of user's payment methods</returns>
    [HttpGet("payment-methods")]
    [ProducesResponseType(typeof(List<PaymentMethodDto>), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<PaymentMethodDto>>> GetPaymentMethods()
    {
        try
        {
            var userId = GetCurrentUserId();
            var paymentMethods = await _paymentService.GetUserPaymentMethodsAsync(userId);
            var paymentMethodDtos = paymentMethods.Select(MapToPaymentMethodDto).ToList();

            return Ok(paymentMethodDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment methods for user");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Sync payment methods from Stripe for the current user
    /// This is useful when webhooks fail to sync payment methods
    /// </summary>
    /// <returns>Synced payment methods</returns>
    [HttpPost("payment-methods/sync")]
    [ProducesResponseType(typeof(List<PaymentMethodDto>), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<PaymentMethodDto>>> SyncPaymentMethods()
    {
        try
        {
            var userId = GetCurrentUserId();

            // Get user's active subscription to find Stripe customer ID
            var subscription = await _subscriptionService.GetUserActiveSubscriptionAsync(userId);
            if (subscription == null || string.IsNullOrEmpty(subscription.ExternalSubscriptionId))
            {
                return NotFound(new { message = "No active subscription with Stripe found" });
            }

            // Get the Stripe subscription to find the payment method
            var stripeSubscriptionService = new Stripe.SubscriptionService();
            var stripeSubscription = await stripeSubscriptionService.GetAsync(subscription.ExternalSubscriptionId);

            if (string.IsNullOrEmpty(stripeSubscription.DefaultPaymentMethodId))
            {
                return Ok(new List<PaymentMethodDto>()); // No payment method on subscription
            }

            // Check if we already have this payment method
            var existingPaymentMethods = await _paymentService.GetUserPaymentMethodsAsync(userId);
            var existingTokens = existingPaymentMethods.Select(pm => pm.Token).ToHashSet();

            if (existingTokens.Contains(stripeSubscription.DefaultPaymentMethodId))
            {
                // Already have this payment method
                return Ok(existingPaymentMethods.Select(MapToPaymentMethodDto).ToList());
            }

            // Get payment method details from Stripe
            var paymentMethodService = new Stripe.PaymentMethodService();
            var stripePaymentMethod = await paymentMethodService.GetAsync(stripeSubscription.DefaultPaymentMethodId);

            // Create payment method in our database
            var paymentMethod = new PaymentMethod
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Provider = "stripe",
                Type = stripePaymentMethod.Type ?? "card",
                Token = stripePaymentMethod.Id,
                Last4Digits = stripePaymentMethod.Card?.Last4 ?? "****",
                Brand = stripePaymentMethod.Card?.Brand ?? "unknown",
                ExpiryDate = stripePaymentMethod.Card != null
                    ? $"{stripePaymentMethod.Card.ExpMonth:D2}/{stripePaymentMethod.Card.ExpYear}"
                    : null,
                CardholderName = stripePaymentMethod.BillingDetails?.Name,
                BillingCountry = stripePaymentMethod.BillingDetails?.Address?.Country,
                BillingPostalCode = stripePaymentMethod.BillingDetails?.Address?.PostalCode,
                IsDefault = !existingPaymentMethods.Any(),
                IsValid = true,
                ExpiresAt = stripePaymentMethod.Card != null
                    ? new DateTime(
                        (int)stripePaymentMethod.Card.ExpYear,
                        (int)stripePaymentMethod.Card.ExpMonth,
                        DateTime.DaysInMonth((int)stripePaymentMethod.Card.ExpYear, (int)stripePaymentMethod.Card.ExpMonth))
                    : null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Save to database
            await _paymentService.SavePaymentMethodFromWebhookAsync(paymentMethod);

            _logger.LogInformation(
                "Manually synced payment method {PaymentMethodId} for user {UserId}. Card: ****{Last4}",
                paymentMethod.Id, userId, paymentMethod.Last4Digits);

            // Return all payment methods
            var allPaymentMethods = await _paymentService.GetUserPaymentMethodsAsync(userId);
            return Ok(allPaymentMethods.Select(MapToPaymentMethodDto).ToList());
        }
        catch (Stripe.StripeException ex)
        {
            _logger.LogError(ex, "Stripe error syncing payment methods for user");
            return StatusCode(500, new { message = "Failed to sync from Stripe: " + ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing payment methods for user");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Set a payment method as the default for the current user
    /// </summary>
    /// <param name="id">Payment method ID</param>
    /// <returns>Updated payment method</returns>
    [HttpPost("payment-methods/{id}/set-default")]
    [ProducesResponseType(typeof(PaymentMethodDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<PaymentMethodDto>> SetDefaultPaymentMethod([FromRoute] Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var paymentMethod = await _paymentService.SetDefaultPaymentMethodAsync(id, userId, GetClientIPAddress());

            _logger.LogInformation(
                "Set payment method {PaymentMethodId} as default for user {UserId}",
                id, userId);

            return Ok(MapToPaymentMethodDto(paymentMethod));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Payment method not found for set default request");
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting default payment method for user");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Remove a payment method
    /// </summary>
    /// <param name="id">Payment method ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("payment-methods/{id}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult> RemovePaymentMethod([FromRoute] Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();

            // First verify the payment method exists and belongs to the user
            var paymentMethod = await _paymentService.GetPaymentMethodAsync(id, userId);
            if (paymentMethod == null)
            {
                return NotFound(new { message = "Payment method not found" });
            }

            // Cannot remove the default payment method
            if (paymentMethod.IsDefault)
            {
                return BadRequest(new { message = "Cannot remove the default payment method. Set another payment method as default first." });
            }

            var success = await _paymentService.RemovePaymentMethodAsync(id, userId, GetClientIPAddress());

            if (!success)
            {
                return StatusCode(500, new { message = "Failed to remove payment method" });
            }

            _logger.LogInformation(
                "Removed payment method {PaymentMethodId} for user {UserId}",
                id, userId);

            return Ok(new { message = "Payment method removed successfully" });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Payment method not found for delete request");
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot remove payment method - operation not allowed");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing payment method for user");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get current user's active subscription
    /// </summary>
    /// <returns>User's active subscription or null if no active subscription</returns>
    [HttpGet("current")]
    [ProducesResponseType(typeof(UserSubscriptionDto), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<UserSubscriptionDto?>> GetCurrentSubscription()
    {
        try
        {
            var userId = GetCurrentUserId();
            var subscription = await _subscriptionService.GetUserActiveSubscriptionAsync(userId);

            // Return null (200 OK) instead of 404 when no subscription exists
            // This prevents console errors and is more appropriate for "check if exists" queries
            // Use Content() with explicit JSON null to force 200 OK (ASP.NET Core converts Ok(null) to 204 NoContent)
            if (subscription == null)
            {
                return Content("null", "application/json");
            }

            // Include recent transactions
            var (transactions, _) = await _subscriptionService.GetUserSubscriptionsAsync(userId, 1, 10);
            var recentTransactions = transactions.FirstOrDefault()?.Transactions
                .Take(5)
                .Select(MapToSubscriptionTransactionDto)
                .ToList() ?? new List<SubscriptionTransactionDto>();

            var subscriptionDto = MapToUserSubscriptionDto(subscription);
            subscriptionDto.RecentTransactions = recentTransactions;

            return Ok(subscriptionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current subscription for user");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get user's subscription history
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20)</param>
    /// <returns>Paginated list of user's subscriptions</returns>
    [HttpGet("history")]
    [ProducesResponseType(typeof(SubscriptionListDto), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<SubscriptionListDto>> GetSubscriptionHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var userId = GetCurrentUserId();
            var (subscriptions, totalCount) = await _subscriptionService.GetUserSubscriptionsAsync(userId, page, pageSize);

            var subscriptionDtos = subscriptions.Select(sub =>
            {
                var dto = MapToUserSubscriptionDto(sub);
                dto.RecentTransactions = sub.Transactions
                    .Take(5)
                    .Select(MapToSubscriptionTransactionDto)
                    .ToList();
                return dto;
            }).ToList();

            var result = new SubscriptionListDto
            {
                Subscriptions = subscriptionDtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                HasNextPage = page * pageSize < totalCount,
                HasPreviousPage = page > 1
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subscription history for user");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Create a new subscription
    /// </summary>
    /// <param name="dto">Subscription creation details</param>
    /// <returns>Created subscription</returns>
    [HttpPost]
    [ProducesResponseType(typeof(UserSubscriptionDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<UserSubscriptionDto>> CreateSubscription([FromBody] CreateSubscriptionDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();

            // Check if user already has an active subscription
            var existingSubscription = await _subscriptionService.GetUserActiveSubscriptionAsync(userId);
            if (existingSubscription != null)
            {
                return Conflict(new { message = "User already has an active subscription" });
            }

            var subscription = await _subscriptionService.CreateSubscriptionAsync(
                userId,
                dto.SubscriptionTierId,
                dto.PaymentMethodId,
                dto.IsTrial,
                dto.IsAnnual,
                GetClientIPAddress());

            var subscriptionDto = MapToUserSubscriptionDto(subscription);

            return CreatedAtAction(nameof(GetCurrentSubscription), new { }, subscriptionDto);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid subscription creation request");
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Subscription creation operation failed");
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subscription");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Upgrade subscription to higher tier
    /// </summary>
    /// <param name="dto">Upgrade details</param>
    /// <returns>Updated subscription</returns>
    [HttpPost("upgrade")]
    [ProducesResponseType(typeof(UserSubscriptionDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<UserSubscriptionDto>> UpgradeSubscription([FromBody] ChangeSubscriptionTierDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();

            // Idempotency check for subscription upgrade
            var operationKey = $"subscription:upgrade:{userId}:{dto.NewTierId}";
            if (await _idempotencyService.IsDuplicateOperationAsync(operationKey))
            {
                _logger.LogWarning("Duplicate subscription upgrade request: user {UserId} to tier {TierId}",
                    userId, dto.NewTierId);

                // Return the current subscription instead of error
                var currentSub = await _subscriptionService.GetUserActiveSubscriptionAsync(userId);
                if (currentSub != null)
                    return Ok(MapToUserSubscriptionDto(currentSub));

                return Ok(new { success = true, message = "Subscription already upgraded (duplicate request ignored)" });
            }

            var subscription = await _subscriptionService.UpgradeSubscriptionAsync(
                userId,
                dto.NewTierId,
                dto.ImmediateCharge,
                GetClientIPAddress());

            // Mark operation as completed
            await _idempotencyService.MarkOperationCompletedAsync(operationKey);

            var subscriptionDto = MapToUserSubscriptionDto(subscription);
            return Ok(subscriptionDto);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid subscription upgrade request");
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Subscription upgrade operation failed");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upgrading subscription");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Downgrade subscription to lower tier
    /// </summary>
    /// <param name="dto">Downgrade details</param>
    /// <returns>Updated subscription</returns>
    [HttpPost("downgrade")]
    [ProducesResponseType(typeof(UserSubscriptionDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<UserSubscriptionDto>> DowngradeSubscription([FromBody] ChangeSubscriptionTierDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            var subscription = await _subscriptionService.DowngradeSubscriptionAsync(
                userId,
                dto.NewTierId,
                dto.EffectiveDate,
                GetClientIPAddress());

            var subscriptionDto = MapToUserSubscriptionDto(subscription);
            return Ok(subscriptionDto);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid subscription downgrade request");
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Subscription downgrade operation failed");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downgrading subscription");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Cancel subscription
    /// </summary>
    /// <param name="dto">Cancellation details</param>
    /// <returns>Cancelled subscription</returns>
    [HttpPost("cancel")]
    [ProducesResponseType(typeof(UserSubscriptionDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<UserSubscriptionDto>> CancelSubscription([FromBody] CancelSubscriptionDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();

            // Idempotency check for subscription cancellation
            var operationKey = $"subscription:cancel:{userId}";
            if (await _idempotencyService.IsDuplicateOperationAsync(operationKey))
            {
                _logger.LogWarning("Duplicate subscription cancel request: user {UserId}", userId);

                // Return the current subscription instead of error
                var currentSub = await _subscriptionService.GetUserActiveSubscriptionAsync(userId);
                if (currentSub != null)
                    return Ok(MapToUserSubscriptionDto(currentSub));

                return Ok(new { success = true, message = "Subscription already cancelled (duplicate request ignored)" });
            }

            var subscription = await _subscriptionService.CancelSubscriptionAsync(
                userId,
                dto.Reason,
                dto.Immediate,
                GetClientIPAddress());

            // Mark operation as completed
            await _idempotencyService.MarkOperationCompletedAsync(operationKey);

            var subscriptionDto = MapToUserSubscriptionDto(subscription);
            return Ok(subscriptionDto);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Subscription cancellation operation failed");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling subscription");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Pause subscription
    /// </summary>
    /// <param name="dto">Pause details</param>
    /// <returns>Paused subscription</returns>
    [HttpPost("pause")]
    [ProducesResponseType(typeof(UserSubscriptionDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<UserSubscriptionDto>> PauseSubscription([FromBody] PauseSubscriptionDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            var subscription = await _subscriptionService.PauseSubscriptionAsync(
                userId,
                dto.PauseDuration,
                GetClientIPAddress());

            var subscriptionDto = MapToUserSubscriptionDto(subscription);
            return Ok(subscriptionDto);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Subscription pause operation failed");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing subscription");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Resume paused subscription
    /// </summary>
    /// <returns>Resumed subscription</returns>
    [HttpPost("resume")]
    [ProducesResponseType(typeof(UserSubscriptionDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<UserSubscriptionDto>> ResumeSubscription()
    {
        try
        {
            var userId = GetCurrentUserId();
            var subscription = await _subscriptionService.ResumeSubscriptionAsync(userId, GetClientIPAddress());

            var subscriptionDto = MapToUserSubscriptionDto(subscription);
            return Ok(subscriptionDto);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Subscription resume operation failed");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming subscription");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Convert trial to paid subscription
    /// </summary>
    /// <param name="paymentMethodId">Payment method ID</param>
    /// <returns>Converted subscription</returns>
    [HttpPost("convert-trial")]
    [ProducesResponseType(typeof(UserSubscriptionDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<UserSubscriptionDto>> ConvertTrialToPaid([FromBody] Guid paymentMethodId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var subscription = await _subscriptionService.ConvertTrialToPaidAsync(
                userId,
                paymentMethodId,
                GetClientIPAddress());

            var subscriptionDto = MapToUserSubscriptionDto(subscription);
            return Ok(subscriptionDto);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Trial conversion operation failed");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting trial to paid subscription");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get subscription limits for current user
    /// </summary>
    /// <returns>User's subscription limits</returns>
    [HttpGet("limits")]
    [ProducesResponseType(typeof(SubscriptionLimitsResponseDto), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<SubscriptionLimitsResponseDto>> GetSubscriptionLimits()
    {
        try
        {
            var userId = GetCurrentUserId();

            // Get subscription limits and usage statistics in parallel
            var limitsTask = _subscriptionService.GetUserSubscriptionLimitsAsync(userId);
            var usageTask = _subscriptionService.GetUserUsageStatisticsAsync(userId);

            await Task.WhenAll(limitsTask, usageTask);

            var limits = await limitsTask;
            var usage = await usageTask;

            var response = new SubscriptionLimitsResponseDto
            {
                MaxActiveProjects = limits.MaxActiveProjects,
                MaxTeamMembers = limits.MaxTeamMembers,
                MaxMonthlyEarnings = limits.MaxMonthlyEarnings,
                PrioritySupport = limits.PrioritySupport,
                ApiAccess = limits.ApiAccess,
                AdvancedAnalytics = limits.AdvancedAnalytics,
                AdvancedFraudDetection = limits.AdvancedFraudDetection,
                MultiSignature = limits.MultiSignature,
                CustomIntegrations = limits.CustomIntegrations,
                Features = limits.Features,
                // Usage statistics now tracked
                CurrentProjects = usage.CurrentActiveProjects,
                CurrentTeamMembers = usage.CurrentTeamMembers,
                CurrentMonthlyEarnings = (int)usage.CurrentMonthlyEarnings
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subscription limits for user");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Check if user has access to a specific feature
    /// </summary>
    /// <param name="feature">Feature name to check</param>
    /// <returns>Feature access status</returns>
    [HttpGet("features/{feature}/access")]
    [ProducesResponseType(typeof(bool), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<bool>> HasFeatureAccess([FromRoute] string feature)
    {
        try
        {
            var userId = GetCurrentUserId();
            var hasAccess = await _subscriptionService.HasFeatureAccessAsync(userId, feature);

            return Ok(hasAccess);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking feature access for user");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    #region Private Helper Methods

    private SubscriptionTierDto MapToSubscriptionTierDto(SubscriptionTier tier)
    {
        var features = new List<string>();
        if (tier.PrioritySupport) features.Add("PrioritySupport");
        if (tier.ApiAccess) features.Add("ApiAccess");
        if (tier.AdvancedAnalytics) features.Add("AdvancedAnalytics");
        if (tier.AdvancedFraudDetection) features.Add("AdvancedFraudDetection");
        if (tier.MultiSignature) features.Add("MultiSignature");
        if (tier.CustomIntegrations) features.Add("CustomIntegrations");

        if (!string.IsNullOrEmpty(tier.Features))
        {
            try
            {
                var additionalFeatures = System.Text.Json.JsonSerializer.Deserialize<List<string>>(tier.Features);
                if (additionalFeatures != null)
                    features.AddRange(additionalFeatures);
            }
            catch (System.Text.Json.JsonException)
            {
                _logger.LogWarning("Failed to parse features JSON for tier {TierId}", tier.Id);
            }
        }

        return new SubscriptionTierDto
        {
            Id = tier.Id,
            Name = tier.Name,
            Description = tier.Description,
            Price = tier.Price,
            AnnualPrice = tier.AnnualPrice,
            CreditBonus = tier.CreditBonus,
            MaxActiveProjects = tier.MaxActiveProjects,
            MaxTeamMembers = tier.MaxTeamMembers,
            PrioritySupport = tier.PrioritySupport,
            ApiAccess = tier.ApiAccess,
            AdvancedAnalytics = tier.AdvancedAnalytics,
            AdvancedFraudDetection = tier.AdvancedFraudDetection,
            MultiSignature = tier.MultiSignature,
            CustomIntegrations = tier.CustomIntegrations,
            MaxMonthlyEarnings = tier.MaxMonthlyEarnings,
            Features = features,
            SortOrder = tier.SortOrder
        };
    }

    private UserSubscriptionDto MapToUserSubscriptionDto(UserSubscription subscription)
    {
        return new UserSubscriptionDto
        {
            Id = subscription.Id,
            Status = subscription.Status,
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            NextBillingDate = subscription.NextBillingDate,
            TrialEndDate = subscription.TrialEndDate,
            AutoRenew = subscription.AutoRenew,
            IsAnnual = subscription.IsAnnual,
            BillingCycleCount = subscription.BillingCycleCount,
            RetryCount = subscription.RetryCount,
            NextRetryAt = subscription.NextRetryAt,
            CreatedAt = subscription.CreatedAt,
            UpdatedAt = subscription.UpdatedAt,
            CancelledAt = subscription.CancelledAt,
            CancellationReason = subscription.CancellationReason,
            Tier = MapToSubscriptionTierDto(subscription.SubscriptionTier),
            PaymentMethod = subscription.PaymentMethod != null ? MapToPaymentMethodDto(subscription.PaymentMethod) : null,
            RecentTransactions = new List<SubscriptionTransactionDto>()
        };
    }

    private PaymentMethodDto MapToPaymentMethodDto(PaymentMethod paymentMethod)
    {
        return new PaymentMethodDto
        {
            Id = paymentMethod.Id,
            Provider = paymentMethod.Provider,
            Type = paymentMethod.Type,
            Last4Digits = paymentMethod.Last4Digits,
            Brand = paymentMethod.Brand,
            ExpiryDate = paymentMethod.ExpiryDate,
            CardholderName = paymentMethod.CardholderName,
            BillingCountry = paymentMethod.BillingCountry,
            BillingPostalCode = paymentMethod.BillingPostalCode,
            IsDefault = paymentMethod.IsDefault,
            IsValid = paymentMethod.IsValid,
            ExpiresAt = paymentMethod.ExpiresAt,
            CreatedAt = paymentMethod.CreatedAt,
            UpdatedAt = paymentMethod.UpdatedAt,
            LastUsedAt = paymentMethod.LastUsedAt
        };
    }

    private SubscriptionTransactionDto MapToSubscriptionTransactionDto(SubscriptionTransaction transaction)
    {
        return new SubscriptionTransactionDto
        {
            Id = transaction.Id,
            Type = transaction.Type,
            Amount = transaction.Amount,
            Currency = transaction.Currency,
            ExternalTransactionId = transaction.ExternalTransactionId,
            Status = transaction.Status,
            Description = transaction.Description,
            FailureReason = transaction.FailureReason,
            RetryCount = transaction.RetryCount,
            NextRetryAt = transaction.NextRetryAt,
            CreatedAt = transaction.CreatedAt,
            ProcessedAt = transaction.ProcessedAt,
            CompletedAt = transaction.CompletedAt,
            FailedAt = transaction.FailedAt,
            RefundedAt = transaction.RefundedAt,
            RefundAmount = transaction.RefundAmount
        };
    }

    #endregion
}