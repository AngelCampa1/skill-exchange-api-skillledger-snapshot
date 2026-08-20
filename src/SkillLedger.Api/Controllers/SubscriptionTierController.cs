using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SkillLedger.Infrastructure.Services;

namespace SkillLedger.Api.Controllers;

/// <summary>
/// API controller for subscription tier information
/// </summary>
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("DefaultPolicy")]
public class SubscriptionTierController : BaseApiController
{
    private readonly SubscriptionDataSeeder _subscriptionSeeder;
    private readonly ILogger<SubscriptionTierController> _logger;

    public SubscriptionTierController(
        SubscriptionDataSeeder subscriptionSeeder,
        ILogger<SubscriptionTierController> logger)
    {
        _subscriptionSeeder = subscriptionSeeder;
        _logger = logger;
    }

    /// <summary>
    /// Get all available subscription tiers for display
    /// </summary>
    /// <returns>List of subscription tiers with pricing and features</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<SubscriptionTierDisplayDto>), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<SubscriptionTierDisplayDto>>> GetSubscriptionTiers()
    {
        try
        {
            var tiers = await _subscriptionSeeder.GetSubscriptionTiersForDisplayAsync();
            return Ok(tiers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subscription tiers");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get a specific subscription tier by ID
    /// </summary>
    /// <param name="id">Subscription tier ID</param>
    /// <returns>Subscription tier details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SubscriptionTierDisplayDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<SubscriptionTierDisplayDto>> GetSubscriptionTier(Guid id)
    {
        try
        {
            var tiers = await _subscriptionSeeder.GetSubscriptionTiersForDisplayAsync();
            var tier = tiers.FirstOrDefault(t => t.Id == id);

            if (tier == null)
            {
                return NotFound(new { message = "Subscription tier not found" });
            }

            return Ok(tier);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subscription tier {TierId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Re-seed subscription tiers (admin only)
    /// </summary>
    /// <returns>Success status</returns>
    [HttpPost("seed")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(500)]
    public async Task<ActionResult> SeedSubscriptionTiers()
    {
        try
        {
            await _subscriptionSeeder.SeedSubscriptionTiersAsync();
            return Ok(new { message = "Subscription tiers seeded successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding subscription tiers");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Validate subscription tiers configuration (admin only)
    /// </summary>
    /// <returns>Validation result</returns>
    [HttpPost("validate")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(500)]
    public async Task<ActionResult> ValidateSubscriptionTiers()
    {
        try
        {
            var isValid = await _subscriptionSeeder.ValidateSubscriptionTiersAsync();

            return Ok(new
            {
                isValid,
                message = isValid
                    ? "Subscription tiers are valid"
                    : "Subscription tiers validation failed",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating subscription tiers");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}