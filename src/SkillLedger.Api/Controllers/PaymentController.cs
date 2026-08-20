using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Interfaces;
using SkillLedger.Core.Enums;

namespace SkillLedger.Api.Controllers;

/// <summary>
/// Payment management API controller
/// Handles payment methods, transactions, and billing operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableRateLimiting("PaymentPolicy")]
public class PaymentController : BaseApiController
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        IPaymentService paymentService,
        ILogger<PaymentController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>
    /// Get user's payment methods
    /// </summary>
    /// <returns>List of user's payment methods</returns>
    [HttpGet("methods")]
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
    /// Get specific payment method
    /// </summary>
    /// <param name="paymentMethodId">Payment method ID</param>
    /// <returns>Payment method details</returns>
    [HttpGet("methods/{paymentMethodId:guid}")]
    [ProducesResponseType(typeof(PaymentMethodDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<PaymentMethodDto>> GetPaymentMethod([FromRoute] Guid paymentMethodId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var paymentMethod = await _paymentService.GetPaymentMethodAsync(paymentMethodId, userId);

            if (paymentMethod == null)
            {
                return NotFound(new { message = "Payment method not found" });
            }

            var paymentMethodDto = MapToPaymentMethodDto(paymentMethod);
            return Ok(paymentMethodDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment method {PaymentMethodId}", paymentMethodId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Create a new payment method
    /// </summary>
    /// <param name="dto">Payment method creation details</param>
    /// <returns>Created payment method</returns>
    [HttpPost("methods")]
    [ProducesResponseType(typeof(PaymentMethodDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<PaymentMethodDto>> CreatePaymentMethod([FromBody] CreatePaymentMethodDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            var paymentMethod = await _paymentService.CreatePaymentMethodAsync(
                userId,
                dto.Provider,
                dto.PaymentMethodToken,
                dto.IsDefault,
                GetClientIPAddress());

            var paymentMethodDto = MapToPaymentMethodDto(paymentMethod);
            return CreatedAtAction(nameof(GetPaymentMethod), new { paymentMethodId = paymentMethod.Id }, paymentMethodDto);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid payment method creation request");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment method");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Set payment method as default
    /// </summary>
    /// <param name="paymentMethodId">Payment method ID</param>
    /// <returns>Updated payment method</returns>
    [HttpPost("methods/{paymentMethodId:guid}/set-default")]
    [ProducesResponseType(typeof(PaymentMethodDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<PaymentMethodDto>> SetDefaultPaymentMethod([FromRoute] Guid paymentMethodId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var paymentMethod = await _paymentService.SetDefaultPaymentMethodAsync(
                paymentMethodId,
                userId,
                GetClientIPAddress());

            var paymentMethodDto = MapToPaymentMethodDto(paymentMethod);
            return Ok(paymentMethodDto);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid set default payment method request");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting default payment method {PaymentMethodId}", paymentMethodId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Remove a payment method
    /// </summary>
    /// <param name="paymentMethodId">Payment method ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("methods/{paymentMethodId:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult> RemovePaymentMethod([FromRoute] Guid paymentMethodId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var success = await _paymentService.RemovePaymentMethodAsync(
                paymentMethodId,
                userId,
                GetClientIPAddress());

            if (!success)
            {
                return NotFound(new { message = "Payment method not found" });
            }

            return Ok(new { message = "Payment method removed successfully" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot remove payment method {PaymentMethodId}", paymentMethodId);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing payment method {PaymentMethodId}", paymentMethodId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Validate a payment method
    /// </summary>
    /// <param name="paymentMethodId">Payment method ID</param>
    /// <returns>Validation result</returns>
    [HttpPost("methods/{paymentMethodId:guid}/validate")]
    [ProducesResponseType(typeof(PaymentValidationResult), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<PaymentValidationResult>> ValidatePaymentMethod([FromRoute] Guid paymentMethodId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _paymentService.ValidatePaymentMethodAsync(paymentMethodId, userId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating payment method {PaymentMethodId}", paymentMethodId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Process a one-time payment
    /// </summary>
    /// <param name="dto">Payment processing details</param>
    /// <returns>Payment result</returns>
    [HttpPost("process")]
    [ProducesResponseType(typeof(PaymentResultDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<PaymentResultDto>> ProcessOneTimePayment([FromBody] ProcessPaymentDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            var result = await _paymentService.ProcessOneTimePaymentAsync(
                userId,
                dto.PaymentMethodId,
                dto.Amount,
                dto.Currency,
                dto.Description,
                GetClientIPAddress());

            var resultDto = MapToPaymentResultDto(result);
            return Ok(resultDto);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid payment processing request");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing one-time payment");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Refund a payment transaction
    /// </summary>
    /// <param name="transactionId">Transaction ID to refund</param>
    /// <param name="amount">Refund amount (optional for full refund)</param>
    /// <param name="reason">Refund reason</param>
    /// <returns>Refund result</returns>
    [HttpPost("transactions/{transactionId:guid}/refund")]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(RefundResultDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<RefundResultDto>> RefundPayment(
        [FromRoute] Guid transactionId,
        [FromQuery] decimal? amount = null,
        [FromQuery] string? reason = null)
    {
        try
        {
            var result = await _paymentService.RefundPaymentAsync(
                transactionId,
                requestingUserId: null,
                amount: amount,
                reason: reason,
                createdFromIP: GetClientIPAddress());

            var resultDto = MapToRefundResultDto(result);
            return Ok(resultDto);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid refund request for transaction {TransactionId}", transactionId);
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized refund request for transaction {TransactionId}", transactionId);
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refunding payment transaction {TransactionId}", transactionId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get payment method details from provider
    /// </summary>
    /// <param name="provider">Payment provider</param>
    /// <param name="paymentMethodToken">Payment method token</param>
    /// <returns>Payment method details</returns>
    [HttpGet("methods/details")]
    [ProducesResponseType(typeof(PaymentMethodDetails), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<PaymentMethodDetails>> GetPaymentMethodDetails(
        [FromQuery] string provider,
        [FromQuery] string paymentMethodToken)
    {
        try
        {
            if (string.IsNullOrEmpty(provider) || string.IsNullOrEmpty(paymentMethodToken))
            {
                return BadRequest(new { message = "Provider and payment method token are required" });
            }

            var details = await _paymentService.GetPaymentMethodDetailsAsync(paymentMethodToken, provider);
            return Ok(details);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment method details");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    #region Private Helper Methods

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

    private PaymentResultDto MapToPaymentResultDto(PaymentResult result)
    {
        return new PaymentResultDto
        {
            Success = result.Success,
            TransactionId = result.Transaction?.Id.ToString(),
            ExternalTransactionId = result.ExternalTransactionId,
            ErrorMessage = result.ErrorMessage,
            Status = result.Status,
            RequiresAction = result.RequiresAction,
            ClientSecret = result.ClientSecret,
            NextActionUrl = result.NextActionUrl
        };
    }

    private RefundResultDto MapToRefundResultDto(RefundResult result)
    {
        return new RefundResultDto
        {
            Success = result.Success,
            RefundTransactionId = result.RefundTransaction?.Id.ToString(),
            ExternalRefundId = result.ExternalRefundId,
            ErrorMessage = result.ErrorMessage,
            Status = result.Status,
            RefundedAmount = result.RefundedAmount
        };
    }

    #endregion
}
