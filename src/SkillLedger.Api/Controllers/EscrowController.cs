using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SkillLedger.Api.Middleware;
using SkillLedger.Core.Attributes;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Infrastructure.Extensions;
using SkillLedger.Infrastructure.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace SkillLedger.Api.Controllers;

/// <summary>
/// API controller for project escrow operations with comprehensive security
/// Provides secure milestone-based payment releases and dispute management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableRateLimiting("EscrowPolicy")]
public class EscrowController : ControllerBase
{
    private readonly IProjectEscrowService _escrowService;
    private readonly ILogger<EscrowController> _logger;
    private readonly Core.Interfaces.IIdempotencyService _idempotencyService;
    private readonly ControllerHelperService _helperService;
    private readonly SkillLedgerDbContext _context;

    public EscrowController(
        IProjectEscrowService escrowService,
        ILogger<EscrowController> logger,
        Core.Interfaces.IIdempotencyService idempotencyService,
        ControllerHelperService helperService,
        SkillLedgerDbContext context)
    {
        _escrowService = escrowService;
        _logger = logger;
        _idempotencyService = idempotencyService;
        _helperService = helperService;
        _context = context;
    }

    #region Escrow Creation and Management

    /// <summary>
    /// Create a new escrow account for a project
    /// </summary>
    [HttpPost("create")]
    [EnableRateLimiting("EscrowPolicy")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEscrow([FromBody] CreateProjectEscrowRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                );
            _logger.LogWarning("Escrow creation validation failed: {@ValidationErrors}", errors);
            return BadRequest(ModelState);
        }

        try
        {
            var currentUserId = _helperService.GetCurrentUserId(User);
            if (currentUserId == Guid.Empty)
            {
                return Unauthorized("Invalid user token");
            }

            var project = await _context.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == request.ProjectId);

            if (project == null)
            {
                return NotFound("Project not found");
            }

            if (project.ClientId != currentUserId)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to create escrow for project {ProjectId} owned by {ClientId}",
                    currentUserId, request.ProjectId, project.ClientId);
                return Forbid();
            }

            if (project.ProviderId != request.ProviderId)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to create escrow for project {ProjectId} with provider {RequestedProviderId}, assigned provider {AssignedProviderId}",
                    currentUserId, request.ProjectId, request.ProviderId, project.ProviderId);
                return BadRequest("Escrow can only be created for the assigned project provider");
            }

            var escrow = await _escrowService.CreateEscrowAsync(
                request.ProjectId,
                request.ProviderId,
                _helperService.GetClientIpAddress(HttpContext));

            _logger.LogInformation("Escrow created successfully: {EscrowId} for project {ProjectId}",
                escrow.Id, request.ProjectId);

            var response = new
            {
                escrow.Id,
                escrow.ProjectId,
                escrow.ClientId,
                escrow.ProviderId,
                escrow.TotalAmount,
                escrow.ReleasedAmount,
                RemainingAmount = escrow.RemainingAmount,
                Status = escrow.Status.ToString(),
                escrow.RequiresMultiSignature,
                escrow.CreatedAt
            };

            return CreatedAtAction(nameof(GetEscrowByProject), new { projectId = escrow.ProjectId }, response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid escrow creation request: {Message}", ex.Message);

            // Return 404 for "Project not found" specifically
            if (ex.Message.Contains("Project not found"))
            {
                return NotFound(ex.Message);
            }

            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Escrow creation failed: {Message}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create escrow for project {ProjectId}", request.ProjectId);
            return StatusCode(500, "Internal server error occurred while creating escrow");
        }
    }

    /// <summary>
    /// Get escrow details for a specific project
    /// </summary>
    [HttpGet("project/{projectId:guid}")]
    public async Task<IActionResult> GetEscrowByProject(Guid projectId)
    {
        try
        {
            var escrow = await _escrowService.GetEscrowByProjectIdAsync(projectId);
            if (escrow == null)
            {
                return NotFound("Escrow not found for the specified project");
            }

            var currentUserId = _helperService.GetCurrentUserId(User);

            // Verify user is involved in the escrow
            if (escrow.ClientId != currentUserId && escrow.ProviderId != currentUserId)
            {
                return StatusCode(403, "Access denied to escrow information");
            }

            var response = new
            {
                escrow.Id,
                escrow.ProjectId,
                ProjectTitle = escrow.Project.Title,
                escrow.ClientId,
                ClientEmail = escrow.Client.Email,
                escrow.ProviderId,
                ProviderEmail = escrow.Provider.Email,
                escrow.TotalAmount,
                escrow.ReleasedAmount,
                RemainingAmount = escrow.RemainingAmount,
                Status = escrow.Status.ToString(),
                escrow.RequiresMultiSignature,
                escrow.CreatedAt,
                escrow.CompletedAt,
                escrow.DisputeReason,
                escrow.DisputedAt,
                ReleasedPercentage = escrow.ReleasedPercentage,
                IsFullyReleased = escrow.IsFullyReleased,
                CanBeReleased = escrow.CanBeReleased,
                MilestoneCount = escrow.Milestones.Count,
                ReleasedMilestones = escrow.Milestones.Count(m => m.IsReleased)
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get escrow for project {ProjectId}", projectId);
            return StatusCode(500, "Internal server error occurred while retrieving escrow");
        }
    }

    /// <summary>
    /// Get all active escrows for the current user
    /// </summary>
    [HttpGet("user/active")]
    public async Task<IActionResult> GetUserActiveEscrows()
    {
        try
        {
            var currentUserId = _helperService.GetCurrentUserId(User);
            var escrows = await _escrowService.GetActiveEscrowsForUserAsync(currentUserId);

            var response = escrows.Select(e => new
            {
                e.Id,
                e.ProjectId,
                ProjectTitle = e.Project.Title,
                Role = e.ClientId == currentUserId ? "Client" : "Provider",
                e.TotalAmount,
                e.ReleasedAmount,
                RemainingAmount = e.RemainingAmount,
                Status = e.Status.ToString(),
                e.CreatedAt,
                ReleasedPercentage = e.ReleasedPercentage,
                MilestoneCount = e.Milestones.Count,
                NextMilestone = e.Milestones
                    .Where(m => !m.IsReleased)
                    .OrderBy(m => m.SequenceOrder)
                    .Select(m => new { m.Description, m.Amount, m.ExpectedCompletionDate })
                    .FirstOrDefault()
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active escrows for user {UserId}", _helperService.GetCurrentUserId(User));
            return StatusCode(500, "Internal server error occurred while retrieving user escrows");
        }
    }

    /// <summary>
    /// Get escrow history and audit trail
    /// </summary>
    [HttpGet("{escrowId:guid}/history")]
    public async Task<IActionResult> GetEscrowHistory(Guid escrowId)
    {
        try
        {
            var escrow = await _escrowService.GetEscrowByIdAsync(escrowId);
            if (escrow == null)
            {
                return NotFound("Escrow not found");
            }

            var currentUserId = _helperService.GetCurrentUserId(User);

            // Verify user is involved in the escrow
            if (escrow.ClientId != currentUserId && escrow.ProviderId != currentUserId)
            {
                return StatusCode(403, "Access denied to escrow history");
            }

            var history = await _escrowService.GetEscrowHistoryAsync(escrowId);

            var response = history.Select(h => new
            {
                h.Id,
                h.Action,
                h.Details,
                h.UserId,
                CreatedAt = h.Timestamp,
                IpAddress = h.IPAddress,
                h.Success
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get escrow history for {EscrowId}", escrowId);
            return StatusCode(500, "Internal server error occurred while retrieving escrow history");
        }
    }

    #endregion

    #region Milestone Management

    /// <summary>
    /// Add a milestone to an escrow account
    /// </summary>
    [HttpPost("milestone/add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMilestone([FromBody] AddMilestoneRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                );
            _logger.LogWarning("Add milestone validation failed: {@ValidationErrors}", errors);
            return BadRequest(ModelState);
        }

        try
        {
            var escrow = await _escrowService.GetEscrowByIdAsync(request.EscrowId);
            if (escrow == null)
            {
                return NotFound("Escrow not found");
            }

            var currentUserId = _helperService.GetCurrentUserId(User);

            // Only client can add milestones
            if (escrow.ClientId != currentUserId)
            {
                return StatusCode(403, "Only the project client can add milestones");
            }

            // BUG-FIN-002 FIX: Validate that total milestones don't exceed escrow amount
            var existingMilestones = await _escrowService.GetMilestonesAsync(request.EscrowId);
            var existingTotal = existingMilestones.Sum(m => m.Amount);
            if (existingTotal + request.Amount > escrow.TotalAmount)
            {
                return BadRequest($"Total milestone amount ({existingTotal + request.Amount}) would exceed escrow amount ({escrow.TotalAmount}). Remaining available: {escrow.TotalAmount - existingTotal}");
            }

            var milestone = await _escrowService.AddMilestoneAsync(
                request.EscrowId,
                request.Description,
                request.Amount,
                request.ExpectedCompletionDate,
                request.LinkedDeliverableId,
                request.SequenceOrder);

            var response = new
            {
                milestone.Id,
                milestone.EscrowId,
                milestone.Description,
                milestone.Amount,
                milestone.ExpectedCompletionDate,
                milestone.SequenceOrder,
                milestone.IsBlocking,
                milestone.CreatedAt,
                Percentage = milestone.Percentage
            };

            return CreatedAtAction(nameof(GetMilestones), new { escrowId = request.EscrowId }, response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid milestone creation request: {Message}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add milestone to escrow {EscrowId}", request.EscrowId);
            return StatusCode(500, "Internal server error occurred while adding milestone");
        }
    }

    /// <summary>
    /// Release a specific milestone to the provider
    /// </summary>
    [HttpPut("milestone/release")]
    [EnableRateLimiting("EscrowPolicy")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReleaseMilestone([FromBody] ReleaseMilestoneRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                );
            _logger.LogWarning("Release milestone validation failed: {@ValidationErrors}", errors);
            return BadRequest(ModelState);
        }

        try
        {
            var currentUserId = _helperService.GetCurrentUserId(User);

            // CRITICAL: Idempotency check for financial operation
            var operationKey = $"escrow:release:{request.MilestoneId}:{currentUserId}";
            if (await _idempotencyService.IsDuplicateOperationAsync(operationKey))
            {
                _logger.LogWarning("Duplicate milestone release request: {MilestoneId} by user {UserId}",
                    request.MilestoneId, currentUserId);
                return Ok(new { success = true, message = "Milestone already released (duplicate request ignored)" });
            }

            var success = await _escrowService.ReleaseMilestoneAsync(
                request.MilestoneId,
                currentUserId,
                request.ReleaseNotes);

            if (!success)
                return BadRequest("Failed to release milestone");

            // Mark operation as completed
            await _idempotencyService.MarkOperationCompletedAsync(operationKey);

            _logger.LogInformation("Milestone {MilestoneId} released successfully by user {UserId}",
                request.MilestoneId, currentUserId);

            return Ok(new { success = true, message = "Milestone released successfully" });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized milestone release attempt: {Message}", ex.Message);
            return StatusCode(403, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Invalid milestone release request: {Message}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to release milestone {MilestoneId}", request.MilestoneId);
            return StatusCode(500, "Internal server error occurred while releasing milestone");
        }
    }

    /// <summary>
    /// Get all milestones for an escrow
    /// </summary>
    [HttpGet("{escrowId:guid}/milestones")]
    public async Task<IActionResult> GetMilestones(Guid escrowId)
    {
        try
        {
            var escrow = await _escrowService.GetEscrowByIdAsync(escrowId);
            if (escrow == null)
            {
                return NotFound("Escrow not found");
            }

            var currentUserId = _helperService.GetCurrentUserId(User);

            // Verify user is involved in the escrow
            if (escrow.ClientId != currentUserId && escrow.ProviderId != currentUserId)
            {
                return StatusCode(403, "Access denied to milestone information");
            }

            var milestones = await _escrowService.GetMilestonesAsync(escrowId);

            var response = milestones.Select(m => new
            {
                m.Id,
                m.Description,
                m.Amount,
                m.IsReleased,
                m.ReleasedAt,
                m.ExpectedCompletionDate,
                m.ActualCompletionDate,
                m.SequenceOrder,
                m.IsBlocking,
                m.ReleaseNotes,
                IsOverdue = m.IsOverdue,
                DaysUntilDue = m.DaysUntilDue,
                CompletionPerformance = m.GetCompletionPerformance(),
                Percentage = m.Percentage,
                CanBeReleased = m.CanBeReleased
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get milestones for escrow {EscrowId}", escrowId);
            return StatusCode(500, "Internal server error occurred while retrieving milestones");
        }
    }

    #endregion

    #region Full Escrow Operations

    /// <summary>
    /// Release the entire escrow amount to the provider
    /// </summary>
    [HttpPut("release-full")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReleaseFullEscrow([FromBody] ReleaseFullEscrowRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                );
            _logger.LogWarning("Release full escrow validation failed: {@ValidationErrors}", errors);
            return BadRequest(ModelState);
        }

        try
        {
            var currentUserId = _helperService.GetCurrentUserId(User);

            // CRITICAL: Idempotency check for full escrow release
            var operationKey = $"escrow:release-full:{request.EscrowId}:{currentUserId}";
            if (await _idempotencyService.IsDuplicateOperationAsync(operationKey))
            {
                _logger.LogWarning("Duplicate full escrow release request: {EscrowId} by user {UserId}",
                    request.EscrowId, currentUserId);
                return Ok(new { success = true, message = "Full escrow already released (duplicate request ignored)" });
            }

            var success = await _escrowService.ReleaseFullEscrowAsync(
                request.EscrowId,
                currentUserId,
                request.ReleaseNotes);

            if (!success)
                return BadRequest("Failed to release full escrow");

            // Mark operation as completed
            await _idempotencyService.MarkOperationCompletedAsync(operationKey);

            _logger.LogInformation("Full escrow {EscrowId} released successfully by user {UserId}",
                request.EscrowId, currentUserId);

            return Ok(new { success = true, message = "Full escrow released successfully" });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized full escrow release attempt: {Message}", ex.Message);
            return StatusCode(403, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Invalid full escrow release request: {Message}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to release full escrow {EscrowId}", request.EscrowId);
            return StatusCode(500, "Internal server error occurred while releasing full escrow");
        }
    }

    /// <summary>
    /// Cancel escrow and refund remaining credits to client
    /// </summary>
    [HttpPut("cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelEscrow([FromBody] CancelEscrowRequest request)
    {
        try
        {
            var currentUserId = _helperService.GetCurrentUserId(User);

            var success = await _escrowService.CancelEscrowAsync(
                request.EscrowId,
                currentUserId,
                request.CancellationReason);

            if (success)
            {
                _logger.LogInformation("Escrow {EscrowId} cancelled successfully by user {UserId}",
                    request.EscrowId, currentUserId);

                return Ok(new { success = true, message = "Escrow cancelled successfully" });
            }

            return BadRequest("Failed to cancel escrow");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized escrow cancellation attempt: {Message}", ex.Message);
            return StatusCode(403, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Invalid escrow cancellation request: {Message}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel escrow {EscrowId}", request.EscrowId);
            return StatusCode(500, "Internal server error occurred while cancelling escrow");
        }
    }

    #endregion

    #region Dispute Management

    /// <summary>
    /// Raise a dispute on an escrow account
    /// </summary>
    [HttpPost("dispute/raise")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RaiseDispute([FromBody] RaiseDisputeRequest request)
    {
        // Validate model state
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUserId = _helperService.GetCurrentUserId(User);

            // Idempotency check
            var operationKey = $"escrow:dispute:{request.EscrowId}:{currentUserId}";
            if (await _idempotencyService.IsDuplicateOperationAsync(operationKey))
            {
                _logger.LogWarning("Duplicate raise dispute request: {EscrowId} by user {UserId}",
                    request.EscrowId, currentUserId);
                return Ok(new { success = true, message = "Dispute already raised (duplicate request ignored)" });
            }

            var success = await _escrowService.RaiseDisputeAsync(
                request.EscrowId,
                currentUserId,
                request.DisputeReason);

            if (!success)
                return BadRequest("Failed to raise dispute");

            // Mark operation as completed
            await _idempotencyService.MarkOperationCompletedAsync(operationKey);

            _logger.LogInformation("Dispute raised for escrow {EscrowId} by user {UserId}",
                request.EscrowId, currentUserId);

            return Ok(new { success = true, message = "Dispute raised successfully" });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized dispute raise attempt: {Message}", ex.Message);
            return StatusCode(403, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Invalid dispute raise request: {Message}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to raise dispute for escrow {EscrowId}", request.EscrowId);
            return StatusCode(500, "Internal server error occurred while raising dispute");
        }
    }

    /// <summary>
    /// Resolve a dispute on an escrow account (Admin only)
    /// </summary>
    [HttpPut("dispute/resolve")]
    [Authorize(Roles = "Admin")]
    [SubscriptionExempt]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolveDispute([FromBody] ResolveDisputeRequest request)
    {
        try
        {
            var currentUserId = _helperService.GetCurrentUserId(User);

            // Idempotency check
            var operationKey = $"escrow:resolve:{request.EscrowId}:{currentUserId}";
            if (await _idempotencyService.IsDuplicateOperationAsync(operationKey))
            {
                _logger.LogWarning("Duplicate resolve dispute request: {EscrowId} by admin {AdminId}",
                    request.EscrowId, currentUserId);
                return Ok(new { success = true, message = "Dispute already resolved (duplicate request ignored)" });
            }

            var success = await _escrowService.ResolveDisputeAsync(
                request.EscrowId,
                currentUserId,
                request.ResolutionAction,
                request.ResolutionNotes);

            if (!success)
                return BadRequest("Failed to resolve dispute");

            // Mark operation as completed
            await _idempotencyService.MarkOperationCompletedAsync(operationKey);

            _logger.LogInformation("Dispute resolved for escrow {EscrowId} by admin {AdminId}",
                request.EscrowId, currentUserId);

            return Ok(new { success = true, message = "Dispute resolved successfully" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Invalid dispute resolution request: {Message}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve dispute for escrow {EscrowId}", request.EscrowId);
            return StatusCode(500, "Internal server error occurred while resolving dispute");
        }
    }

    /// <summary>
    /// Get all disputed escrows for admin review
    /// </summary>
    [HttpGet("disputes")]
    [Authorize(Roles = "Admin")]
    [SubscriptionExempt]
    public async Task<IActionResult> GetDisputedEscrows()
    {
        try
        {
            var disputedEscrows = await _escrowService.GetDisputedEscrowsAsync();

            var response = disputedEscrows.Select(e => new
            {
                e.Id,
                e.ProjectId,
                ProjectTitle = e.Project.Title,
                e.ClientId,
                ClientEmail = e.Client.Email,
                e.ProviderId,
                ProviderEmail = e.Provider.Email,
                e.TotalAmount,
                e.ReleasedAmount,
                e.DisputeReason,
                e.DisputedAt,
                e.CreatedAt
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get disputed escrows");
            return StatusCode(500, "Internal server error occurred while retrieving disputed escrows");
        }
    }

    #endregion

    #region Admin Operations

    /// <summary>
    /// Freeze an escrow account (Admin only)
    /// </summary>
    [HttpPut("{escrowId:guid}/freeze")]
    [Authorize(Roles = "Admin")]
    [SubscriptionExempt]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FreezeEscrow(Guid escrowId, [FromBody] FreezeEscrowRequest request)
    {
        try
        {
            var currentUserId = _helperService.GetCurrentUserId(User);

            var success = await _escrowService.FreezeEscrowAsync(escrowId, currentUserId, request.FreezeReason);

            if (success)
            {
                _logger.LogInformation("Escrow {EscrowId} frozen by admin {AdminId}", escrowId, currentUserId);
                return Ok(new { success = true, message = "Escrow frozen successfully" });
            }

            return BadRequest("Failed to freeze escrow");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to freeze escrow {EscrowId}", escrowId);
            return StatusCode(500, "Internal server error occurred while freezing escrow");
        }
    }

    /// <summary>
    /// Unfreeze an escrow account (Admin only)
    /// </summary>
    [HttpPut("{escrowId:guid}/unfreeze")]
    [Authorize(Roles = "Admin")]
    [SubscriptionExempt]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnfreezeEscrow(Guid escrowId)
    {
        try
        {
            var currentUserId = _helperService.GetCurrentUserId(User);

            var success = await _escrowService.UnfreezeEscrowAsync(escrowId, currentUserId);

            if (success)
            {
                _logger.LogInformation("Escrow {EscrowId} unfrozen by admin {AdminId}", escrowId, currentUserId);
                return Ok(new { success = true, message = "Escrow unfrozen successfully" });
            }

            return BadRequest("Failed to unfreeze escrow");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unfreeze escrow {EscrowId}", escrowId);
            return StatusCode(500, "Internal server error occurred while unfreezing escrow");
        }
    }

    /// <summary>
    /// Get system-wide escrow metrics (Admin only)
    /// </summary>
    [HttpGet("metrics")]
    [Authorize(Roles = "Admin")]
    [SubscriptionExempt]
    public async Task<IActionResult> GetSystemMetrics()
    {
        try
        {
            var metrics = await _escrowService.GetSystemEscrowMetricsAsync();
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get system escrow metrics");
            return StatusCode(500, "Internal server error occurred while retrieving system metrics");
        }
    }

    #endregion

    #region User Statistics

    /// <summary>
    /// Get escrow statistics for the current user
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetUserStatistics()
    {
        try
        {
            var currentUserId = _helperService.GetCurrentUserId(User);
            var statistics = await _escrowService.GetEscrowStatisticsAsync(currentUserId);
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get escrow statistics for user {UserId}", _helperService.GetCurrentUserId(User));
            return StatusCode(500, "Internal server error occurred while retrieving user statistics");
        }
    }

    #endregion
}

#region Request/Response Models

public class CreateProjectEscrowRequest
{
    [Required]
    public Guid ProjectId { get; set; }

    [Required]
    public Guid ProviderId { get; set; }
}

public class AddMilestoneRequest
{
    [Required]
    public Guid EscrowId { get; set; }

    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Range(1, int.MaxValue)]
    public int Amount { get; set; }

    public DateTime? ExpectedCompletionDate { get; set; }

    public Guid? LinkedDeliverableId { get; set; }

    [Range(1, int.MaxValue)]
    public int? SequenceOrder { get; set; }
}

public class ReleaseMilestoneRequest
{
    [Required]
    public Guid MilestoneId { get; set; }

    [StringLength(1000)]
    public string? ReleaseNotes { get; set; }
}

public class ReleaseFullEscrowRequest
{
    [Required]
    public Guid EscrowId { get; set; }

    [StringLength(1000)]
    public string? ReleaseNotes { get; set; }
}

public class CancelEscrowRequest
{
    [Required]
    public Guid EscrowId { get; set; }

    [StringLength(1000)]
    public string? CancellationReason { get; set; }
}

public class RaiseDisputeRequest
{
    [Required]
    public Guid EscrowId { get; set; }

    [Required]
    [StringLength(1000, MinimumLength = 10)]
    public string DisputeReason { get; set; } = string.Empty;
}

public class ResolveDisputeRequest
{
    [Required]
    public Guid EscrowId { get; set; }

    [Required]
    [StringLength(500, MinimumLength = 5)]
    public string ResolutionAction { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? ResolutionNotes { get; set; }
}

public class FreezeEscrowRequest
{
    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string FreezeReason { get; set; } = string.Empty;
}

#endregion
