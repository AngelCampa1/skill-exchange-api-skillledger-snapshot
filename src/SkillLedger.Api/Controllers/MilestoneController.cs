using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using SkillLedger.Api.Hubs;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Extensions;
using SkillLedger.Infrastructure.Services;
using System.Security.Claims;

namespace SkillLedger.Api.Controllers;

/// <summary>
/// Controller for milestone tracking and deliverable management
/// Provides comprehensive milestone lifecycle management with real-time updates
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MilestoneController : ControllerBase
{
    private readonly IMilestoneTrackingService _milestoneService;
    private readonly IHubContext<MessagingHub> _messagingHubContext;
    private readonly ILogger<MilestoneController> _logger;
    private readonly IAuditLogService _auditLogService;
    private readonly Core.Interfaces.IIdempotencyService _idempotencyService;
    private readonly ControllerHelperService _helperService;

    public MilestoneController(
        IMilestoneTrackingService milestoneService,
        IHubContext<MessagingHub> messagingHubContext,
        ILogger<MilestoneController> logger,
        IAuditLogService auditLogService,
        Core.Interfaces.IIdempotencyService idempotencyService,
        ControllerHelperService helperService)
    {
        _milestoneService = milestoneService;
        _messagingHubContext = messagingHubContext;
        _logger = logger;
        _auditLogService = auditLogService;
        _idempotencyService = idempotencyService;
        _helperService = helperService;
    }

    /// <summary>
    /// Get milestone by ID
    /// </summary>
    [HttpGet("{milestoneId:guid}")]
    public async Task<ActionResult<MilestoneResponseDto>> GetMilestone(Guid milestoneId)
    {
        try
        {
            var userId = _helperService.GetCurrentUserId(User);
            var milestone = await _milestoneService.GetMilestoneByIdAsync(milestoneId);

            if (milestone == null)
                return NotFound($"Milestone {milestoneId} not found");

            // Validate user access through service layer
            var hasAccess = await _milestoneService.ValidateUserPermissionsAsync(milestoneId, userId, "READ");
            if (!hasAccess)
                return Forbid();

            return Ok(milestone);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving milestone {MilestoneId}", milestoneId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get milestones with filtering and pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedMilestonesDto>> GetMilestones([FromQuery] MilestoneFilterDto filter)
    {
        try
        {
            var userId = _helperService.GetCurrentUserId(User);
            var result = await _milestoneService.GetMilestonesAsync(filter, userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving milestones with filter");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Create a new milestone
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("GeneralApiPolicy")]
    public async Task<ActionResult<MilestoneResponseDto>> CreateMilestone([FromBody] CreateMilestoneRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = _helperService.GetCurrentUserId(User);
            var ipAddress = _helperService.GetClientIpAddress(HttpContext);

            var result = await _milestoneService.CreateMilestoneAsync(request, userId, ipAddress);

            // Audit logging
            _auditLogService.LogAuditEventAsync(
                _logger,
                userId,
                AuditActions.MILESTONE_CREATED,
                ipAddress,
                Request.Headers.UserAgent.ToString(),
                true,
                $"{{\"MilestoneId\":\"{result.Id}\",\"ProjectId\":\"{request.ProjectId}\"}}"
            );

            // Notify workspace participants via SignalR
            try
            {
                await _messagingHubContext.Clients.Group($"workspace_{request.ProjectId}")
                    .SendAsync("MilestoneCreated", result);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to broadcast MilestoneCreated for {MilestoneId}", result.Id);
                // Continue - SignalR failure should not block HTTP response
            }

            return CreatedAtAction(nameof(GetMilestone), new { milestoneId = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating milestone");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update an existing milestone
    /// </summary>
    [HttpPut("{milestoneId:guid}")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("GeneralApiPolicy")]
    public async Task<ActionResult<MilestoneResponseDto>> UpdateMilestone(Guid milestoneId, [FromBody] UpdateMilestoneRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = _helperService.GetCurrentUserId(User);
            var ipAddress = _helperService.GetClientIpAddress(HttpContext);
            var result = await _milestoneService.UpdateMilestoneAsync(milestoneId, request, userId);

            if (result == null)
                return NotFound($"Milestone {milestoneId} not found");

            // Audit logging
            _auditLogService.LogAuditEventAsync(
                _logger,
                userId,
                AuditActions.MILESTONE_UPDATED,
                ipAddress,
                Request.Headers.UserAgent.ToString(),
                true,
                $"{{\"MilestoneId\":\"{milestoneId}\"}}"
            );

            // Notify workspace participants via SignalR
            try
            {
                await _messagingHubContext.Clients.Group($"milestone_{milestoneId}")
                    .SendAsync("MilestoneUpdated", result);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to broadcast MilestoneUpdated for {MilestoneId}", milestoneId);
                // Continue - SignalR failure should not block HTTP response
            }

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating milestone {MilestoneId}", milestoneId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete a milestone
    /// </summary>
    [HttpDelete("{milestoneId:guid}")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("GeneralApiPolicy")]
    public async Task<ActionResult> DeleteMilestone(Guid milestoneId)
    {
        try
        {
            var userId = _helperService.GetCurrentUserId(User);
            var ipAddress = _helperService.GetClientIpAddress(HttpContext);
            var success = await _milestoneService.DeleteMilestoneAsync(milestoneId, userId);

            if (!success)
                return NotFound($"Milestone {milestoneId} not found or cannot be deleted");

            // Audit logging
            _auditLogService.LogAuditEventAsync(
                _logger,
                userId,
                AuditActions.MILESTONE_DELETED,
                ipAddress,
                Request.Headers.UserAgent.ToString(),
                true,
                $"{{\"MilestoneId\":\"{milestoneId}\"}}"
            );

            // Notify workspace participants via SignalR
            try
            {
                await _messagingHubContext.Clients.Group($"milestone_{milestoneId}")
                    .SendAsync("MilestoneDeleted", milestoneId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to broadcast MilestoneDeleted for {MilestoneId}", milestoneId);
                // Continue - SignalR failure should not block HTTP response
            }

            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting milestone {MilestoneId}", milestoneId);
            return StatusCode(500, "Internal server error");
        }
    }

    #region Milestone Status Management

    /// <summary>
    /// Start work on a milestone
    /// </summary>
    [HttpPost("{milestoneId:guid}/start")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("MilestoneStateChangePolicy")]
    public async Task<ActionResult> StartMilestone(Guid milestoneId)
    {
        try
        {
            var userId = _helperService.GetCurrentUserId(User);
            var ipAddress = _helperService.GetClientIpAddress(HttpContext);

            // Idempotency check
            var operationKey = $"milestone:start:{milestoneId}:{userId}";
            if (await _idempotencyService.IsDuplicateOperationAsync(operationKey))
            {
                _logger.LogWarning("Duplicate start milestone request: {MilestoneId} by user {UserId}", milestoneId, userId);
                return Ok(new { message = "Milestone already started (duplicate request ignored)" });
            }

            var success = await _milestoneService.StartMilestoneAsync(milestoneId, userId);

            if (!success)
                return BadRequest("Failed to start milestone. Check milestone status and permissions.");

            // Mark operation as completed
            await _idempotencyService.MarkOperationCompletedAsync(operationKey);

            // Audit logging
            _auditLogService.LogAuditEventAsync(
                _logger,
                userId,
                AuditActions.MILESTONE_STARTED,
                ipAddress,
                Request.Headers.UserAgent.ToString(),
                true,
                $"{{\"MilestoneId\":\"{milestoneId}\"}}"
            );

            // Notify workspace participants
            try
            {
                await _messagingHubContext.Clients.Group($"milestone_{milestoneId}")
                    .SendAsync("MilestoneStarted", milestoneId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to broadcast MilestoneStarted for {MilestoneId}", milestoneId);
                // Continue - SignalR failure should not block HTTP response
            }

            return Ok(new { message = "Milestone started successfully" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting milestone {MilestoneId}", milestoneId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Submit milestone for review
    /// </summary>
    [HttpPost("{milestoneId:guid}/submit")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("MilestoneStateChangePolicy")]
    public async Task<ActionResult> SubmitMilestoneForReview(Guid milestoneId)
    {
        try
        {
            var userId = _helperService.GetCurrentUserId(User);
            var ipAddress = _helperService.GetClientIpAddress(HttpContext);

            // Idempotency check
            var operationKey = $"milestone:submit:{milestoneId}:{userId}";
            if (await _idempotencyService.IsDuplicateOperationAsync(operationKey))
            {
                _logger.LogWarning("Duplicate submit milestone request: {MilestoneId} by user {UserId}", milestoneId, userId);
                return Ok(new { message = "Milestone already submitted for review (duplicate request ignored)" });
            }

            var success = await _milestoneService.SubmitMilestoneForReviewAsync(milestoneId, userId);

            if (!success)
                return BadRequest("Failed to submit milestone for review. Check milestone status and permissions.");

            // Mark operation as completed
            await _idempotencyService.MarkOperationCompletedAsync(operationKey);

            // Audit logging
            _auditLogService.LogAuditEventAsync(
                _logger,
                userId,
                AuditActions.MILESTONE_SUBMITTED,
                ipAddress,
                Request.Headers.UserAgent.ToString(),
                true,
                $"{{\"MilestoneId\":\"{milestoneId}\"}}"
            );

            // Notify workspace participants
            try
            {
                await _messagingHubContext.Clients.Group($"milestone_{milestoneId}")
                    .SendAsync("MilestoneSubmittedForReview", milestoneId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to broadcast MilestoneSubmittedForReview for {MilestoneId}", milestoneId);
                // Continue - SignalR failure should not block HTTP response
            }

            return Ok(new { message = "Milestone submitted for review successfully" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting milestone {MilestoneId} for review", milestoneId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Approve a milestone
    /// </summary>
    [HttpPost("{milestoneId:guid}/approve")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("MilestoneStateChangePolicy")]
    public async Task<ActionResult> ApproveMilestone(Guid milestoneId, [FromBody] ApproveMilestoneRequestDto request)
    {
        try
        {
            var userId = _helperService.GetCurrentUserId(User);
            var ipAddress = _helperService.GetClientIpAddress(HttpContext);

            // Idempotency check
            var operationKey = $"milestone:approve:{milestoneId}:{userId}";
            if (await _idempotencyService.IsDuplicateOperationAsync(operationKey))
            {
                _logger.LogWarning("Duplicate approve milestone request: {MilestoneId} by user {UserId}", milestoneId, userId);
                return Ok(new { message = "Milestone already approved (duplicate request ignored)" });
            }

            var success = await _milestoneService.ApproveMilestoneAsync(milestoneId, userId, request.ReviewNotes);

            if (!success)
                return BadRequest("Failed to approve milestone. Check milestone status and permissions.");

            // Mark operation as completed
            await _idempotencyService.MarkOperationCompletedAsync(operationKey);

            // Audit logging
            _auditLogService.LogAuditEventAsync(
                _logger,
                userId,
                AuditActions.MILESTONE_APPROVED,
                ipAddress,
                Request.Headers.UserAgent.ToString(),
                true,
                $"{{\"MilestoneId\":\"{milestoneId}\"}}"
            );

            // Notify workspace participants
            try
            {
                await _messagingHubContext.Clients.Group($"milestone_{milestoneId}")
                    .SendAsync("MilestoneApproved", new { MilestoneId = milestoneId, ReviewNotes = request.ReviewNotes });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to broadcast MilestoneApproved for {MilestoneId}", milestoneId);
                // Continue - SignalR failure should not block HTTP response
            }

            return Ok(new { message = "Milestone approved successfully" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving milestone {MilestoneId}", milestoneId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Request revisions for a milestone
    /// </summary>
    [HttpPost("{milestoneId:guid}/request-revisions")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("MilestoneStateChangePolicy")]
    public async Task<ActionResult> RequestMilestoneRevisions(Guid milestoneId, [FromBody] RequestRevisionsDto request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.ReviewNotes))
                return BadRequest("Review notes are required for revision requests");

            var userId = _helperService.GetCurrentUserId(User);
            var ipAddress = _helperService.GetClientIpAddress(HttpContext);

            // Idempotency check
            var operationKey = $"milestone:revision:{milestoneId}:{userId}";
            if (await _idempotencyService.IsDuplicateOperationAsync(operationKey))
            {
                _logger.LogWarning("Duplicate revision request: {MilestoneId} by user {UserId}", milestoneId, userId);
                return Ok(new { message = "Milestone revisions already requested (duplicate request ignored)" });
            }

            var success = await _milestoneService.RequestMilestoneRevisionAsync(milestoneId, userId, request.ReviewNotes);

            if (!success)
                return BadRequest("Failed to request milestone revisions. Check milestone status and permissions.");

            // Mark operation as completed
            await _idempotencyService.MarkOperationCompletedAsync(operationKey);

            // Audit logging
            _auditLogService.LogAuditEventAsync(
                _logger,
                userId,
                AuditActions.MILESTONE_REVISION_REQUESTED,
                ipAddress,
                Request.Headers.UserAgent.ToString(),
                true,
                $"{{\"MilestoneId\":\"{milestoneId}\"}}"
            );

            // Notify workspace participants
            try
            {
                await _messagingHubContext.Clients.Group($"milestone_{milestoneId}")
                    .SendAsync("MilestoneRevisionsRequested", new { MilestoneId = milestoneId, ReviewNotes = request.ReviewNotes });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to broadcast MilestoneRevisionsRequested for {MilestoneId}", milestoneId);
                // Continue - SignalR failure should not block HTTP response
            }

            return Ok(new { message = "Milestone revisions requested successfully" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting revisions for milestone {MilestoneId}", milestoneId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Cancel a milestone
    /// </summary>
    [HttpPost("{milestoneId:guid}/cancel")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("MilestoneStateChangePolicy")]
    public async Task<ActionResult> CancelMilestone(Guid milestoneId, [FromBody] CancelMilestoneRequestDto request)
    {
        try
        {
            var userId = _helperService.GetCurrentUserId(User);
            var ipAddress = _helperService.GetClientIpAddress(HttpContext);

            // Idempotency check
            var operationKey = $"milestone:cancel:{milestoneId}:{userId}";
            if (await _idempotencyService.IsDuplicateOperationAsync(operationKey))
            {
                _logger.LogWarning("Duplicate cancel milestone request: {MilestoneId} by user {UserId}", milestoneId, userId);
                return Ok(new { message = "Milestone already cancelled (duplicate request ignored)" });
            }

            var success = await _milestoneService.CancelMilestoneAsync(milestoneId, userId, request.Reason);

            if (!success)
                return BadRequest("Failed to cancel milestone. Check milestone status and permissions.");

            // Mark operation as completed
            await _idempotencyService.MarkOperationCompletedAsync(operationKey);

            // Audit logging
            _auditLogService.LogAuditEventAsync(
                _logger,
                userId,
                AuditActions.MILESTONE_CANCELLED,
                ipAddress,
                Request.Headers.UserAgent.ToString(),
                true,
                $"{{\"MilestoneId\":\"{milestoneId}\"}}"
            );

            // Notify workspace participants
            try
            {
                await _messagingHubContext.Clients.Group($"milestone_{milestoneId}")
                    .SendAsync("MilestoneCancelled", new { MilestoneId = milestoneId, Reason = request.Reason });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to broadcast MilestoneCancelled for {MilestoneId}", milestoneId);
                // Continue - SignalR failure should not block HTTP response
            }

            return Ok(new { message = "Milestone cancelled successfully" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling milestone {MilestoneId}", milestoneId);
            return StatusCode(500, "Internal server error");
        }
    }

    #endregion

    #region Deliverable Submissions

    /// <summary>
    /// Create a submission for a milestone
    /// </summary>
    [HttpPost("{milestoneId:guid}/submissions")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("GeneralApiPolicy")]
    public async Task<ActionResult<SubmissionResponseDto>> CreateSubmission(Guid milestoneId, [FromBody] CreateSubmissionRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            request.MilestoneId = milestoneId; // Ensure consistency

            var userId = _helperService.GetCurrentUserId(User);
            var ipAddress = _helperService.GetClientIpAddress(HttpContext);

            var result = await _milestoneService.CreateSubmissionAsync(request, userId, ipAddress);

            // Audit logging
            _auditLogService.LogAuditEventAsync(
                _logger,
                userId,
                AuditActions.SUBMISSION_CREATED,
                ipAddress,
                Request.Headers.UserAgent.ToString(),
                true,
                $"{{\"SubmissionId\":\"{result.Id}\",\"MilestoneId\":\"{milestoneId}\"}}"
            );

            // Notify workspace participants
            try
            {
                await _messagingHubContext.Clients.Group($"milestone_{milestoneId}")
                    .SendAsync("SubmissionCreated", result);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to broadcast SubmissionCreated for milestone {MilestoneId}", milestoneId);
                // Continue - SignalR failure should not block HTTP response
            }

            return CreatedAtAction(nameof(GetSubmission), new { submissionId = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating submission for milestone {MilestoneId}", milestoneId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get submission by ID
    /// </summary>
    [HttpGet("submissions/{submissionId:guid}")]
    public async Task<ActionResult<SubmissionResponseDto>> GetSubmission(Guid submissionId)
    {
        try
        {
            var userId = _helperService.GetCurrentUserId(User);
            var submission = await _milestoneService.GetSubmissionByIdAsync(submissionId, userId);

            if (submission == null)
                return NotFound($"Submission {submissionId} not found");

            return Ok(submission);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving submission {SubmissionId}", submissionId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get all submissions for a milestone
    /// </summary>
    [HttpGet("{milestoneId:guid}/submissions")]
    public async Task<ActionResult<List<SubmissionResponseDto>>> GetMilestoneSubmissions(Guid milestoneId)
    {
        try
        {
            var userId = _helperService.GetCurrentUserId(User);
            var submissions = await _milestoneService.GetMilestoneSubmissionsAsync(milestoneId, userId);
            return Ok(submissions);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving submissions for milestone {MilestoneId}", milestoneId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Review a milestone submission
    /// </summary>
    [HttpPost("submissions/{submissionId:guid}/review")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("GeneralApiPolicy")]
    public async Task<ActionResult> ReviewSubmission(Guid submissionId, [FromBody] ReviewSubmissionRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = _helperService.GetCurrentUserId(User);
            var ipAddress = _helperService.GetClientIpAddress(HttpContext);

            // Idempotency check
            var operationKey = $"submission:review:{submissionId}:{userId}";
            if (await _idempotencyService.IsDuplicateOperationAsync(operationKey))
            {
                _logger.LogWarning("Duplicate review submission request: {SubmissionId} by user {UserId}", submissionId, userId);
                return Ok(new { message = "Submission already reviewed (duplicate request ignored)" });
            }

            var success = await _milestoneService.ReviewSubmissionAsync(submissionId, request, userId);

            if (!success)
                return BadRequest("Failed to review submission. Check submission status and permissions.");

            // Mark operation as completed
            await _idempotencyService.MarkOperationCompletedAsync(operationKey);

            // Audit logging
            _auditLogService.LogAuditEventAsync(
                _logger,
                userId,
                AuditActions.SUBMISSION_REVIEWED,
                ipAddress,
                Request.Headers.UserAgent.ToString(),
                true,
                $"{{\"SubmissionId\":\"{submissionId}\"}}"
            );

            // Notify workspace participants
            try
            {
                await _messagingHubContext.Clients.Group($"submission_{submissionId}")
                    .SendAsync("SubmissionReviewed", new { SubmissionId = submissionId, Review = request });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to broadcast SubmissionReviewed for {SubmissionId}", submissionId);
                // Continue - SignalR failure should not block HTTP response
            }

            return Ok(new { message = "Submission reviewed successfully" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reviewing submission {SubmissionId}", submissionId);
            return StatusCode(500, "Internal server error");
        }
    }

    #endregion

    #region Progress and Analytics

    /// <summary>
    /// Get project progress summary
    /// </summary>
    [HttpGet("projects/{projectId:guid}/progress")]
    public async Task<ActionResult<ProjectProgressDto>> GetProjectProgress(Guid projectId)
    {
        try
        {
            var userId = _helperService.GetCurrentUserId(User);
            var progress = await _milestoneService.GetProjectProgressAsync(projectId, userId);
            return Ok(progress);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving progress for project {ProjectId}", projectId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get overdue milestones for current user
    /// </summary>
    [HttpGet("overdue")]
    public async Task<ActionResult<List<MilestoneResponseDto>>> GetOverdueMilestones()
    {
        try
        {
            var userId = _helperService.GetCurrentUserId(User);
            var overdueMilestones = await _milestoneService.GetOverdueMilestonesAsync(userId);
            return Ok(overdueMilestones);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving overdue milestones");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get upcoming milestones for current user
    /// </summary>
    [HttpGet("upcoming")]
    public async Task<ActionResult<List<MilestoneResponseDto>>> GetUpcomingMilestones([FromQuery] int daysAhead = 7)
    {
        try
        {
            var userId = _helperService.GetCurrentUserId(User);
            var upcomingMilestones = await _milestoneService.GetUpcomingMilestonesAsync(userId, daysAhead);
            return Ok(upcomingMilestones);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving upcoming milestones");
            return StatusCode(500, "Internal server error");
        }
    }

    #endregion

    #region Escrow Integration

    /// <summary>
    /// Link milestone to escrow milestone for payment triggers
    /// </summary>
    [HttpPost("{milestoneId:guid}/link-escrow/{escrowMilestoneId:guid}")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("GeneralApiPolicy")]
    public async Task<ActionResult> LinkToEscrowMilestone(Guid milestoneId, Guid escrowMilestoneId)
    {
        try
        {
            var userId = _helperService.GetCurrentUserId(User);
            var ipAddress = _helperService.GetClientIpAddress(HttpContext);
            var success = await _milestoneService.LinkToEscrowMilestoneAsync(milestoneId, escrowMilestoneId, userId);

            if (!success)
                return BadRequest("Failed to link milestone to escrow. Check permissions and milestone status.");

            // Audit logging
            _auditLogService.LogAuditEventAsync(
                _logger,
                userId,
                AuditActions.ESCROW_LINKED,
                ipAddress,
                Request.Headers.UserAgent.ToString(),
                true,
                $"{{\"MilestoneId\":\"{milestoneId}\",\"EscrowMilestoneId\":\"{escrowMilestoneId}\"}}"
            );

            return Ok(new { message = "Milestone linked to escrow successfully" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking milestone {MilestoneId} to escrow {EscrowMilestoneId}", milestoneId, escrowMilestoneId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Trigger payment release for approved milestone
    /// CRITICAL: Protected with idempotency to prevent double payment release
    /// </summary>
    [HttpPost("{milestoneId:guid}/trigger-payment")]
    [EnableRateLimiting("MilestonePaymentPolicy")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> TriggerPaymentRelease(
        Guid milestoneId,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey = null)
    {
        try
        {
            var userId = _helperService.GetCurrentUserId(User);
            var ipAddress = _helperService.GetClientIpAddress(HttpContext);

            // CRITICAL: Idempotency check to prevent double payment release
            var operationKey = $"milestone:payment:{milestoneId}:{userId}";

            if (await _idempotencyService.IsDuplicateOperationAsync(operationKey))
            {
                _logger.LogWarning(
                    "Duplicate payment release detected for milestone {MilestoneId} by user {UserId} from IP {IpAddress}",
                    milestoneId, userId, ipAddress);

                return Ok(new { message = "Payment already released (duplicate request ignored)" });
            }

            var success = await _milestoneService.TriggerPaymentReleaseAsync(milestoneId, userId);

            if (!success)
            {
                // Don't mark as completed if operation failed
                return BadRequest("Failed to trigger payment release. Check milestone approval status and permissions.");
            }

            // Mark operation as completed to prevent duplicates
            await _idempotencyService.MarkOperationCompletedAsync(operationKey);

            // Audit logging (fire-and-forget pattern)
            _auditLogService.LogAuditEventAsync(
                _logger,
                userId,
                AuditActions.PAYMENT_RELEASE_TRIGGERED,
                ipAddress,
                Request.Headers.UserAgent.ToString(),
                true,
                $"{{\"MilestoneId\":\"{milestoneId}\"}}"
            );

            // Notify workspace participants (with error handling)
            try
            {
                await _messagingHubContext.Clients.Group($"milestone_{milestoneId}")
                    .SendAsync("PaymentReleased", milestoneId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to broadcast PaymentReleased for milestone {MilestoneId}",
                    milestoneId);
                // Continue - SignalR failure should not block HTTP response
            }

            return Ok(new { message = "Payment release triggered successfully" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering payment release for milestone {MilestoneId}", milestoneId);
            return StatusCode(500, "Internal server error");
        }
    }

    #endregion

    #region Private Helper Methods

    #endregion
}
