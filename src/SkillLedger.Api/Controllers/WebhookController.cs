using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using SkillLedger.Api.Attributes;
using SkillLedger.Infrastructure.Services;

namespace SkillLedger.Api.Controllers;

/// <summary>
/// Controller for handling external webhooks
/// </summary>
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("WebhookPolicy")]
public class WebhookController : BaseApiController
{
    // BUG-CRIT-004 FIX: Re-enabled StripeWebhookService with proper signature validation
    private readonly StripeWebhookService _stripeWebhookService;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(
        StripeWebhookService stripeWebhookService,
        ILogger<WebhookController> logger)
    {
        _stripeWebhookService = stripeWebhookService;
        _logger = logger;
    }

    /// <summary>
    /// Handle Stripe webhooks
    /// </summary>
    /// <returns>Success status</returns>
    [HttpPost("stripe")]
    [AllowAnonymous] // Webhooks are authenticated via signature, not JWT
    [SkillLedger.Api.Attributes.IgnoreAntiforgeryToken]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> HandleStripeWebhook()
    {
        try
        {
            _logger.LogInformation("Received Stripe webhook");

            // Read the raw request body - Stripe webhooks require the raw JSON for signature validation
            string jsonPayload;
            using (var reader = new StreamReader(Request.Body))
            {
                jsonPayload = await reader.ReadToEndAsync();
            }

            // Get the Stripe signature header
            var stripeSignature = Request.Headers["Stripe-Signature"].FirstOrDefault();

            // BUG-CRIT-004 FIX: Validate required parameters
            if (string.IsNullOrEmpty(jsonPayload))
            {
                _logger.LogWarning("Received empty Stripe webhook payload");
                return BadRequest("Webhook payload is required");
            }

            if (string.IsNullOrEmpty(stripeSignature))
            {
                _logger.LogWarning("Received Stripe webhook without signature");
                return BadRequest("Stripe signature is required");
            }

            // BUG-CRIT-004 FIX: Validate webhook signature BEFORE processing
            // This prevents attackers from sending fake webhook payloads
            Stripe.Event stripeEvent;
            try
            {
                stripeEvent = _stripeWebhookService.ConstructEvent(jsonPayload, stripeSignature);
            }
            catch (Stripe.StripeException stripeEx)
            {
                _logger.LogError(stripeEx, "Webhook signature validation failed");
                return BadRequest("Invalid signature");
            }
            catch (InvalidOperationException configEx)
            {
                _logger.LogError(configEx, "Webhook secret not configured");
                return StatusCode(503, "Webhook processing is not configured");
            }

            // BUG-CRIT-004 FIX: Only process webhooks with validated signatures
            // BUG-INT-016 FIX: Track processing outcome for proper Stripe response
            try
            {
                await _stripeWebhookService.ProcessWebhookEventAsync(stripeEvent);

                _logger.LogInformation("Successfully processed webhook event: {EventType} (ID: {EventId})",
                    stripeEvent.Type, stripeEvent.Id);

                return Ok(new { received = true, processed = true });
            }
            catch (InvalidOperationException opEx)
            {
                // BUG-INT-016 FIX: Return 422 for events that can't be processed due to invalid data
                // This tells Stripe not to retry (the data won't change)
                _logger.LogWarning(opEx, "Webhook event could not be processed: {EventType} (ID: {EventId})",
                    stripeEvent.Type, stripeEvent.Id);
                return UnprocessableEntity(new { received = true, processed = false, error = "Event data could not be processed" });
            }
            catch (KeyNotFoundException notFoundEx)
            {
                // BUG-INT-016 FIX: Return 404 for events referencing non-existent resources
                _logger.LogWarning(notFoundEx, "Webhook event references non-existent resource: {EventType} (ID: {EventId})",
                    stripeEvent.Type, stripeEvent.Id);
                return NotFound(new { received = true, processed = false, error = "Referenced resource not found" });
            }
        }
        catch (Exception ex)
        {
            // BUG-INT-016 FIX: Return 500 for unexpected errors - Stripe will retry
            _logger.LogError(ex, "Unexpected error processing Stripe webhook");
            return StatusCode(500, new { received = false, error = "Internal server error" });
        }
    }

}
