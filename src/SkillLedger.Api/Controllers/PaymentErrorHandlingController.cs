using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using Stripe;
using SkillLedger.Infrastructure.Services;

namespace SkillLedger.Api.Controllers;

/// <summary>
/// Controller for handling payment errors and recovery workflows
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableRateLimiting("PaymentPolicy")]
public class PaymentErrorHandlingController : BaseApiController
{
    private readonly PaymentErrorHandlingService _errorHandlingService;
    private readonly ILogger<PaymentErrorHandlingController> _logger;

    public PaymentErrorHandlingController(
        PaymentErrorHandlingService errorHandlingService,
        ILogger<PaymentErrorHandlingController> logger)
    {
        _errorHandlingService = errorHandlingService;
        _logger = logger;
    }

    /// <summary>
    /// Handles payment failure and provides recovery options
    /// </summary>
    /// <param name="request">Payment failure handling request</param>
    /// <returns>Error handling result</returns>
    [HttpPost("handle-payment-failure")]
    [ProducesResponseType(typeof(PaymentErrorHandlingResult), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<PaymentErrorHandlingResult>> HandlePaymentFailure(
        [FromBody] HandlePaymentFailureRequest request)
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
            if (string.IsNullOrEmpty(request.PaymentIntentId))
            {
                return BadRequest("Payment Intent ID is required");
            }

            // Handle payment failure
            var result = await _errorHandlingService.HandlePaymentFailureAsync(
                request.PaymentIntentId,
                userId,
                request.ErrorDetails);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling payment failure for user");
            return StatusCode(500, "An error occurred while handling the payment failure");
        }
    }

    /// <summary>
    /// Gets available recovery options for a failed payment
    /// </summary>
    /// <param name="paymentIntentId">Failed payment intent ID</param>
    /// <returns>Available recovery options</returns>
    [HttpGet("recovery-options/{paymentIntentId}")]
    [ProducesResponseType(typeof(PaymentRecoveryOptions), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<PaymentRecoveryOptions>> GetRecoveryOptions(string paymentIntentId)
    {
        try
        {
            // Get current user ID
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized("User not authenticated");
            }

            // Validate payment intent ID
            if (string.IsNullOrEmpty(paymentIntentId))
            {
                return BadRequest("Payment Intent ID is required");
            }

            // Get recovery options
            var options = await _errorHandlingService.GetRecoveryOptionsAsync(paymentIntentId, userId);

            return Ok(options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recovery options for payment intent: {PaymentIntentId}", paymentIntentId);
            return StatusCode(500, "An error occurred while retrieving recovery options");
        }
    }

    /// <summary>
    /// Attempts to retry a failed payment
    /// </summary>
    /// <param name="request">Payment retry request</param>
    /// <returns>Retry result</returns>
    [HttpPost("retry-payment")]
    [ProducesResponseType(typeof(PaymentRetryResult), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<PaymentRetryResult>> RetryPayment(
        [FromBody] RetryPaymentRequest request)
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
            if (string.IsNullOrEmpty(request.PaymentIntentId))
            {
                return BadRequest("Payment Intent ID is required");
            }

            // Retry payment
            var result = await _errorHandlingService.RetryPaymentAsync(
                request.PaymentIntentId,
                userId,
                request.Reason);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying payment for user");
            return StatusCode(500, "An error occurred while retrying the payment");
        }
    }

    /// <summary>
    /// Processes invoice payment failure (used by webhooks)
    /// </summary>
    /// <param name="request">Invoice dunning request</param>
    /// <returns>Dunning result</returns>
    [HttpPost("process-invoice-failure")]
    [ProducesResponseType(typeof(InvoiceDunningResult), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<InvoiceDunningResult>> ProcessInvoiceFailure(
        [FromBody] ProcessInvoiceFailureRequest request)
    {
        try
        {
            // Validate request
            if (string.IsNullOrEmpty(request.InvoiceId))
            {
                return BadRequest("Invoice ID is required");
            }

            if (string.IsNullOrEmpty(request.SubscriptionId))
            {
                return BadRequest("Subscription ID is required");
            }

            // Process invoice payment failure
            var result = await _errorHandlingService.ProcessInvoicePaymentFailureAsync(
                request.InvoiceId,
                request.SubscriptionId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing invoice payment failure for invoice: {InvoiceId}", request.InvoiceId);
            return StatusCode(500, "An error occurred while processing the invoice failure");
        }
    }

    // VULN-007 FIX: Disable test endpoint in production builds
    // Test endpoints expose server information and should never be available in production
#if DEBUG
    /// <summary>
    /// Test endpoint for payment error handling
    /// WARNING: Development/Testing only - disabled in production builds
    /// </summary>
    /// <returns>Test status</returns>
    [HttpGet("test")]
    [ProducesResponseType(200)]
    [AllowAnonymous] // Allow unauthenticated access for E2E tests
    public IActionResult TestErrorHandling()
    {
        return Ok(new
        {
            message = "Payment error handling endpoint is active (DEBUG BUILD ONLY)",
            timestamp = DateTime.UtcNow,
            environment = "Development"
        });
    }
#endif
}

/// <summary>
/// Request to handle payment failure
/// </summary>
public class HandlePaymentFailureRequest
{
    /// <summary>
    /// Payment Intent ID that failed
    /// </summary>
    public string PaymentIntentId { get; set; } = string.Empty;

    /// <summary>
    /// Error details from Stripe (optional)
    /// </summary>
    public StripeError? ErrorDetails { get; set; }
}

/// <summary>
/// Request to retry a payment
/// </summary>
public class RetryPaymentRequest
{
    /// <summary>
    /// Payment Intent ID to retry
    /// </summary>
    public string PaymentIntentId { get; set; } = string.Empty;

    /// <summary>
    /// Reason for retry
    /// </summary>
    public PaymentRetryReason Reason { get; set; } = PaymentRetryReason.UserInitiated;
}

/// <summary>
/// Request to process invoice payment failure
/// </summary>
public class ProcessInvoiceFailureRequest
{
    /// <summary>
    /// Invoice ID that failed
    /// </summary>
    public string InvoiceId { get; set; } = string.Empty;

    /// <summary>
    /// Related subscription ID
    /// </summary>
    public string SubscriptionId { get; set; } = string.Empty;
}
