using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Infrastructure.Services;
using SkillLedger.Core.DTOs;
using SkillLedger.Api.Middleware;

namespace SkillLedger.Api.Controllers;

/// <summary>
/// Controller for handling Stripe checkout operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Route("checkout")] // Legacy route alias for frontend compatibility
[Authorize]
[EnableRateLimiting("CheckoutPolicy")]
public class CheckoutController : BaseApiController
{
    private const int TrialDurationDays = 30;

    private readonly StripeCheckoutService _checkoutService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly SkillLedgerDbContext _context;
    private readonly ILogger<CheckoutController> _logger;

    public CheckoutController(
        StripeCheckoutService checkoutService,
        ISubscriptionService subscriptionService,
        SkillLedgerDbContext context,
        ILogger<CheckoutController> logger)
    {
        _checkoutService = checkoutService;
        _subscriptionService = subscriptionService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Create a checkout session for a new subscription
    /// </summary>
    /// <param name="request">Checkout request details</param>
    /// <returns>Checkout session details</returns>
    [HttpPost("create-subscription")]
    [SubscriptionExempt]
    [ProducesResponseType(typeof(CheckoutSessionResult), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<CheckoutSessionResult>> CreateSubscriptionCheckout([FromBody] CreateSubscriptionCheckoutRequest request)
    {
        try
        {
            // Get current user ID
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized("User not authenticated");
            }

            // Validate request
            if (request.TierId == Guid.Empty)
            {
                return BadRequest("Tier ID is required");
            }

            if (string.IsNullOrEmpty(request.SuccessUrl))
            {
                return BadRequest("Success URL is required");
            }

            if (string.IsNullOrEmpty(request.CancelUrl))
            {
                return BadRequest("Cancel URL is required");
            }

            // Validate that user doesn't already have an active subscription with this tier
            var existingSubscription = await _subscriptionService.GetUserActiveSubscriptionAsync(userId);
            if (existingSubscription != null && existingSubscription.SubscriptionTierId == request.TierId)
            {
                return BadRequest("You already have an active subscription to this tier");
            }

            // Determine trial eligibility: only first-time subscribers get a trial
            int? trialPeriodDays = null;
            if (request.IncludeTrial)
            {
                var hasPriorSubscription = await _context.UserSubscriptions
                    .AnyAsync(us => us.UserId == userId);
                if (!hasPriorSubscription)
                {
                    trialPeriodDays = TrialDurationDays;
                }
            }

            // SECURITY FIX: Validate and sanitize redirect URLs to prevent open redirect attacks
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var successUrl = ValidateRedirectUrl(request.SuccessUrl, $"{baseUrl}/subscription/success?session_id={{CHECKOUT_SESSION_ID}}");
            var cancelUrl = ValidateRedirectUrl(request.CancelUrl, $"{baseUrl}/subscription/choose-plan");

            // Create checkout session
            var result = await _checkoutService.CreateSubscriptionCheckoutAsync(
                userId,
                request.TierId,
                request.BillingCycle,
                successUrl,
                cancelUrl,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                trialPeriodDays);

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subscription checkout session for user");
            return StatusCode(500, "An error occurred while creating the checkout session");
        }
    }

    /// <summary>
    /// Create a checkout session for adding a payment method
    /// </summary>
    /// <param name="request">Payment method setup request</param>
    /// <returns>Checkout session details</returns>
    [HttpPost("setup-payment-method")]
    [ProducesResponseType(typeof(CheckoutSessionResult), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<CheckoutSessionResult>> CreatePaymentMethodSetupSession([FromBody] PaymentMethodSetupRequest request)
    {
        try
        {
            // Get current user ID
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized("User not authenticated");
            }

            // Validate request
            if (string.IsNullOrEmpty(request.SuccessUrl))
            {
                return BadRequest("Success URL is required");
            }

            if (string.IsNullOrEmpty(request.CancelUrl))
            {
                return BadRequest("Cancel URL is required");
            }

            // SECURITY FIX: Validate and sanitize redirect URLs to prevent open redirect attacks
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var successUrl = ValidateRedirectUrl(request.SuccessUrl, $"{baseUrl}/account/payment-methods?setup_success=true");
            var cancelUrl = ValidateRedirectUrl(request.CancelUrl, $"{baseUrl}/account/payment-methods");

            // Create checkout session
            var result = await _checkoutService.CreatePaymentMethodSetupSessionAsync(
                userId,
                successUrl,
                cancelUrl,
                HttpContext.Connection.RemoteIpAddress?.ToString());

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment method setup session for user");
            return StatusCode(500, "An error occurred while creating the payment method setup session");
        }
    }

    /// <summary>
    /// Get details of a checkout session
    /// </summary>
    /// <param name="sessionId">Stripe session ID</param>
    /// <returns>Session details</returns>
    [HttpGet("session/{sessionId}")]
    [SubscriptionExempt]
    [ProducesResponseType(typeof(CheckoutSessionDetails), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<CheckoutSessionDetails>> GetCheckoutSession(string sessionId)
    {
        try
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                return BadRequest("Session ID is required");
            }

            var sessionDetails = await _checkoutService.GetCheckoutSessionAsync(sessionId);
            if (sessionDetails == null)
            {
                return NotFound("Session not found");
            }

            return Ok(sessionDetails);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving checkout session {SessionId}", sessionId);
            return StatusCode(500, "An error occurred while retrieving the session");
        }
    }

    /// <summary>
    /// Get available subscription tiers for checkout
    /// </summary>
    /// <returns>List of available subscription tiers</returns>
    [HttpGet("subscription-tiers")]
    [SubscriptionExempt]
    [ProducesResponseType(typeof(List<SubscriptionTierDto>), 200)]
    public async Task<ActionResult<List<SubscriptionTierDto>>> GetSubscriptionTiers()
    {
        try
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized("User not authenticated");
            }

            var tiers = await _subscriptionService.GetSubscriptionTiersAsync();
            var tierDtos = tiers.Select(t => new SubscriptionTierDto
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                Price = t.Price,
                AnnualPrice = t.AnnualPrice,
                CreditBonus = t.CreditBonus,
                MaxActiveProjects = t.MaxActiveProjects,
                MaxTeamMembers = t.MaxTeamMembers,
                PrioritySupport = t.PrioritySupport,
                ApiAccess = t.ApiAccess,
                AdvancedAnalytics = t.AdvancedAnalytics,
                AdvancedFraudDetection = t.AdvancedFraudDetection,
                MultiSignature = t.MultiSignature,
                CustomIntegrations = t.CustomIntegrations,
                Features = new List<string>()
            }).ToList();
            return Ok(tierDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subscription tiers");
            return StatusCode(500, "An error occurred while retrieving subscription tiers");
        }
    }

    /// <summary>
    /// Check if the current user is eligible for a free trial
    /// </summary>
    /// <returns>Trial eligibility status</returns>
    [HttpGet("trial-eligibility")]
    [SubscriptionExempt]
    [ProducesResponseType(typeof(TrialEligibilityResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<TrialEligibilityResponse>> GetTrialEligibility()
    {
        try
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized("User not authenticated");
            }

            var hasPriorSubscription = await _context.UserSubscriptions
                .AnyAsync(us => us.UserId == userId);

            return Ok(new TrialEligibilityResponse
            {
                Eligible = !hasPriorSubscription,
                TrialDays = TrialDurationDays
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking trial eligibility for user");
            return StatusCode(500, "An error occurred while checking trial eligibility");
        }
    }

    /// <summary>
    /// Known-safe hosts for redirect URLs. Dynamic parent-domain matching is intentionally
    /// avoided to prevent open-redirect via attacker-controlled subdomains.
    /// </summary>
    private static readonly HashSet<string> AllowedRedirectHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "skillledger.app",
        "api.skillledger.app",
        "app.skillledger.app",
        "localhost",
        "localhost:3030",
        "localhost:8030",
        "127.0.0.1",
        "127.0.0.1:3030",
        "127.0.0.1:8030"
    };

    /// <summary>
    /// SECURITY: Validates redirect URLs to prevent open redirect attacks.
    /// Accepts relative paths (always safe) or absolute URLs whose host is in the
    /// explicit allowlist. Dynamic parent-domain matching is not used because it
    /// would allow any attacker-controlled subdomain of skillledger.app.
    /// </summary>
    /// <param name="userProvidedUrl">URL provided by user</param>
    /// <param name="fallbackUrl">Safe fallback URL if validation fails</param>
    /// <returns>Validated URL or fallback</returns>
    private string ValidateRedirectUrl(string? userProvidedUrl, string fallbackUrl)
    {
        // If no URL provided, use fallback
        if (string.IsNullOrWhiteSpace(userProvidedUrl))
        {
            return fallbackUrl;
        }

        var currentScheme = Request.Scheme.ToLowerInvariant();
        var currentHost = Request.Host.Value?.ToLowerInvariant() ?? string.Empty;

        try
        {
            // Relative paths are always safe — they stay on the same origin
            if (!userProvidedUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !userProvidedUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                if (Uri.TryCreate(userProvidedUrl, UriKind.Relative, out _))
                {
                    var safeUrl = $"{currentScheme}://{currentHost}{(userProvidedUrl.StartsWith("/") ? "" : "/")}{userProvidedUrl}";
                    _logger.LogInformation("Converted relative URL to absolute: {Url}", safeUrl);
                    return safeUrl;
                }
                _logger.LogWarning("SECURITY: Invalid relative URL format: {Url}", userProvidedUrl);
                return fallbackUrl;
            }

            // Absolute URL — validate against explicit allowlist
            if (Uri.TryCreate(userProvidedUrl, UriKind.Absolute, out var absoluteUri))
            {
                var urlHost = absoluteUri.Host.ToLowerInvariant();

                // Include port for non-standard ports (e.g. localhost:3030)
                if (absoluteUri.Port is not (80 or 443))
                    urlHost = $"{urlHost}:{absoluteUri.Port}";

                if (AllowedRedirectHosts.Contains(urlHost) || AllowedRedirectHosts.Contains(absoluteUri.Host.ToLowerInvariant()))
                {
                    _logger.LogInformation("Validated allowlisted redirect URL: {Url}", userProvidedUrl);
                    return userProvidedUrl;
                }

                _logger.LogWarning(
                    "SECURITY: Blocked non-allowlisted redirect. URL host: {UrlHost}, Allowed: {Allowed}",
                    urlHost, string.Join(", ", AllowedRedirectHosts));
                return fallbackUrl;
            }

            _logger.LogWarning("SECURITY: Invalid redirect URL format: {Url}", userProvidedUrl);
            return fallbackUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SECURITY: Error validating redirect URL: {Url}", userProvidedUrl);
            return fallbackUrl;
        }
    }
}

/// <summary>
/// Request to create a subscription checkout session
/// </summary>
public class CreateSubscriptionCheckoutRequest
{
    /// <summary>
    /// Subscription tier ID
    /// </summary>
    public Guid TierId { get; set; }

    /// <summary>
    /// Billing cycle (Monthly or Annual)
    /// </summary>
    public BillingCycle BillingCycle { get; set; } = BillingCycle.Monthly;

    /// <summary>
    /// URL to redirect to on successful payment
    /// </summary>
    public string SuccessUrl { get; set; } = string.Empty;

    /// <summary>
    /// URL to redirect to on cancelled payment
    /// </summary>
    public string CancelUrl { get; set; } = string.Empty;

    /// <summary>
    /// Optional promo code
    /// </summary>
    public string? PromoCode { get; set; }

    /// <summary>
    /// Whether to include a free trial period if the user is eligible (default true)
    /// </summary>
    public bool IncludeTrial { get; set; } = true;
}

/// <summary>
/// Request to create a payment method setup session
/// </summary>
public class PaymentMethodSetupRequest
{
    /// <summary>
    /// URL to redirect to on successful setup
    /// </summary>
    public string SuccessUrl { get; set; } = string.Empty;

    /// <summary>
    /// URL to redirect to on cancelled setup
    /// </summary>
    public string CancelUrl { get; set; } = string.Empty;

    /// <summary>
    /// Whether this should be set as the default payment method
    /// </summary>
    public bool SetAsDefault { get; set; } = true;
}

/// <summary>
/// Response for trial eligibility check
/// </summary>
public class TrialEligibilityResponse
{
    /// <summary>
    /// Whether the user is eligible for a free trial
    /// </summary>
    public bool Eligible { get; set; }

    /// <summary>
    /// Number of trial days if eligible
    /// </summary>
    public int TrialDays { get; set; }
}