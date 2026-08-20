using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Interfaces;
using System.Security.Claims;

namespace SkillLedger.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableRateLimiting("TransferPolicy")]
public class CreditTransferController : ControllerBase
{
    private readonly ICreditTransferService _creditTransferService;
    private readonly ILogger<CreditTransferController> _logger;

    public CreditTransferController(
        ICreditTransferService creditTransferService,
        ILogger<CreditTransferController> logger)
    {
        _creditTransferService = creditTransferService;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(TransferCreditsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<TransferCreditsResponseDto>> TransferCredits(
        [FromBody] TransferCreditsRequestDto request)
    {
        try
        {
            var fromUserId = GetCurrentUserId();
            var ipAddress = GetClientIPAddress();
            var userAgent = Request.Headers.UserAgent.ToString();

            // BUG-040 FIX: Pass idempotency key to service
            var response = await _creditTransferService.TransferCreditsAsync(
                fromUserId,
                request.ToUserId,
                request.Amount,
                request.Message,
                ipAddress,
                userAgent,
                request.IdempotencyKey);

            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Credit transfer failed for user {UserId}", User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown");
            return StatusCode(500, "An error occurred while processing the transfer");
        }
    }

    [HttpPost("batch")]
    [ProducesResponseType(typeof(BatchTransferResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BatchTransferResponseDto>> BatchTransfer(
        [FromBody] BatchTransferRequestDto request)
    {
        try
        {
            var fromUserId = GetCurrentUserId();
            var ipAddress = GetClientIPAddress();
            var userAgent = Request.Headers.UserAgent.ToString();

            var response = await _creditTransferService.BatchTransferAsync(
                fromUserId,
                request.Transfers,
                ipAddress,
                userAgent);

            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch transfer failed for user {UserId}", User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown");
            return StatusCode(500, "An error occurred while processing the batch transfer");
        }
    }

    [HttpGet("{transferId}")]
    [ProducesResponseType(typeof(CreditTransferDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreditTransferDetailDto>> GetTransferDetails(Guid transferId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var transfer = await _creditTransferService.GetTransferDetailsAsync(transferId, userId);

            if (transfer == null)
                return NotFound();

            return Ok(transfer);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    [HttpGet("history")]
    [ProducesResponseType(typeof(TransferHistoryResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TransferHistoryResponseDto>> GetTransferHistory(
        [FromQuery] TransferHistoryRequestDto request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var history = await _creditTransferService.GetTransferHistoryAsync(userId, request);
            return Ok(history);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    [HttpPost("{transferId}/reverse")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ReverseTransfer(Guid transferId,
        [FromBody] ReverseTransferRequestDto request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var success = await _creditTransferService.ReverseTransferAsync(transferId, userId, request.Reason);

            if (!success)
                return BadRequest("Transfer cannot be reversed");

            return Ok();
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    [HttpGet("{transferId}/can-reverse")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<bool>> CanReverseTransfer(Guid transferId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var canReverse = await _creditTransferService.CanReverseTransferAsync(transferId, userId);
            return Ok(canReverse);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    [HttpGet("limits")]
    [ProducesResponseType(typeof(TransferLimitsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TransferLimitsDto>> GetTransferLimits()
    {
        try
        {
            var userId = GetCurrentUserId();
            var limits = await _creditTransferService.GetTransferLimitsAsync(userId);
            return Ok(limits);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    [HttpGet("{transferId}/receipt")]
    [ProducesResponseType(typeof(TransferReceiptDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransferReceiptDto>> GenerateReceipt(Guid transferId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var receipt = await _creditTransferService.GenerateReceiptAsync(transferId, userId);

            if (receipt == null)
                return NotFound();

            return Ok(receipt);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    [HttpPost("verify-receipt")]
    [ProducesResponseType(typeof(VerifyReceiptResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<VerifyReceiptResponseDto>> VerifyReceipt(
        [FromBody] VerifyReceiptRequestDto request)
    {
        var response = await _creditTransferService.VerifyReceiptAsync(
            request.TransferId,
            request.Signature);

        return Ok(response);
    }

    [HttpGet("statistics")]
    [ProducesResponseType(typeof(TransferStatistics), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TransferStatistics>> GetTransferStatistics(
        [FromQuery] int? hours = 24)
    {
        try
        {
            var userId = GetCurrentUserId();
            var timeframe = TimeSpan.FromHours(hours ?? 24);
            var stats = await _creditTransferService.GetTransferStatisticsAsync(userId, timeframe);
            return Ok(stats);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    [HttpGet("fraud-analysis")]
    [ProducesResponseType(typeof(FraudAssessmentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<FraudAssessmentResult>> AnalyzeFraudRisk(
        [FromQuery] int amount)
    {
        try
        {
            var userId = GetCurrentUserId();
            var ipAddress = GetClientIPAddress();
            var analysis = await _creditTransferService.AnalyzeTransferRiskAsync(userId, amount, ipAddress);
            return Ok(analysis);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Invalid or missing user identity");
        return userId;
    }

    private string GetClientIPAddress()
    {
        return SkillLedger.Infrastructure.Services.TrustedClientIpResolver.GetClientIpAddress(HttpContext);
    }
}
