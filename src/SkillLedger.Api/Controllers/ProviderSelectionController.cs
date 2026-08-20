using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SkillLedger.Api.Filters;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Services;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Claims;

namespace SkillLedger.Api.Controllers;

/// <summary>
/// Controller for provider selection and matching functionality
/// </summary>
[ApiController]
[Route("api/provider-selection")]
[Authorize] // Require authentication for all selection endpoints
public class ProviderSelectionController : BaseApiController
{
    private readonly IProviderSelectionService _selectionService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<ProviderSelectionController> _logger;
    private readonly Core.Interfaces.IIdempotencyService _idempotencyService;

    public ProviderSelectionController(
        IProviderSelectionService selectionService,
        IAuditLogService auditLogService,
        ILogger<ProviderSelectionController> logger,
        Core.Interfaces.IIdempotencyService idempotencyService)
    {
        _selectionService = selectionService;
        _auditLogService = auditLogService;
        _logger = logger;
        _idempotencyService = idempotencyService;
    }

    /// <summary>
    /// Create a new provider selection for a project
    /// </summary>
    /// <param name="createDto">Selection creation details</param>
    /// <returns>Selection creation result</returns>
    [HttpPost]
    [EnableRateLimiting("ProviderSelectionPolicy")]
    [ServiceFilter(typeof(ConditionalAntiforgeryFilter))]
    [ProducesResponseType(typeof(ServiceResponseDto), (int)HttpStatusCode.Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType((int)HttpStatusCode.TooManyRequests)]
    public async Task<IActionResult> CreateProviderSelection([FromBody] CreateProviderSelectionDto createDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "User authentication required" });
        }

        var ipAddress = GetClientIpAddress();
        var result = await _selectionService.CreateProviderSelectionAsync(createDto, userId.Value, ipAddress);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Message, details = result.ErrorDetails });
        }

        // Return 201 Created with selection ID
        return Created($"/api/provider-selection/{result.Data}", new
        {
            message = result.Message,
            selectionId = result.Data
        });
    }

    /// <summary>
    /// Get provider selection by ID
    /// </summary>
    /// <param name="id">Selection ID</param>
    /// <returns>Selection details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProviderSelectionDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    public async Task<IActionResult> GetProviderSelection(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "User authentication required" });
        }

        var selection = await _selectionService.GetProviderSelectionByIdAsync(id, userId.Value);

        if (selection == null)
        {
            return NotFound(new { message = "Provider selection not found or access denied" });
        }

        return Ok(selection);
    }

    /// <summary>
    /// Get provider selection for a specific project
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <returns>Selection details or null if no selection made</returns>
    [HttpGet("project/{projectId:guid}")]
    [ProducesResponseType(typeof(ProviderSelectionDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    public async Task<IActionResult> GetProjectSelection(Guid projectId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "User authentication required" });
        }

        var selection = await _selectionService.GetProjectSelectionAsync(projectId, userId.Value);

        if (selection == null)
        {
            return NotFound(new { message = "No provider selection found for this project or access denied" });
        }

        return Ok(selection);
    }

    /// <summary>
    /// Get selection dashboard with ranked applications for a project
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <returns>Selection dashboard with ranked applications</returns>
    [HttpGet("dashboard/{projectId:guid}")]
    [ProducesResponseType(typeof(SelectionDashboardDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    public async Task<IActionResult> GetSelectionDashboard(Guid projectId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "User authentication required" });
        }

        try
        {
            var dashboard = await _selectionService.GetSelectionDashboardAsync(projectId, userId.Value);
            return Ok(dashboard);
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, new { message = "You don't have permission to view this project's selection dashboard." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving selection dashboard for project {ProjectId}", projectId);
            return BadRequest(new { message = "An error occurred while retrieving the selection dashboard." });
        }
    }

    /// <summary>
    /// Rank and compare applications for a project
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <returns>List of ranked applications with comparison scores</returns>
    [HttpGet("rank/{projectId:guid}")]
    [ProducesResponseType(typeof(List<ApplicationComparisonDto>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    public async Task<IActionResult> RankApplications(Guid projectId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "User authentication required" });
        }

        try
        {
            var rankedApplications = await _selectionService.RankApplicationsAsync(projectId, userId.Value);
            return Ok(rankedApplications);
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, new { message = "You don't have permission to rank applications for this project." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ranking applications for project {ProjectId}", projectId);
            return BadRequest(new { message = "An error occurred while ranking applications." });
        }
    }

    /// <summary>
    /// Get detailed comparison for a specific application
    /// </summary>
    /// <param name="applicationId">Application ID</param>
    /// <param name="projectId">Project ID</param>
    /// <returns>Application comparison details with ranking score</returns>
    [HttpGet("compare/{applicationId:guid}/project/{projectId:guid}")]
    [ProducesResponseType(typeof(ApplicationComparisonDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> GetApplicationComparison(Guid applicationId, Guid projectId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "User authentication required" });
        }

        try
        {
            var comparison = await _selectionService.CalculateApplicationRankingAsync(
                applicationId,
                projectId,
                userId.Value,
                User.IsInRole("Admin"));
            return Ok(comparison);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, new { message = "You don't have permission to compare applications for this project." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating application comparison for application {ApplicationId}", applicationId);
            return BadRequest(new { message = "An error occurred while calculating application comparison." });
        }
    }

    /// <summary>
    /// Get provider work history and reputation summary
    /// </summary>
    /// <param name="providerId">Provider user ID</param>
    /// <returns>Provider history summary</returns>
    [HttpGet("provider-history/{providerId:guid}")]
    [ProducesResponseType(typeof(ProviderHistorySummaryDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> GetProviderHistory(Guid providerId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "User authentication required" });
        }

        try
        {
            var history = await _selectionService.GetProviderHistorySummaryAsync(providerId);
            return Ok(history);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving provider history for provider {ProviderId}", providerId);
            return BadRequest(new { message = "An error occurred while retrieving provider history." });
        }
    }

    /// <summary>
    /// Update provider selection status
    /// </summary>
    /// <param name="id">Selection ID</param>
    /// <param name="status">New status</param>
    /// <returns>Update result</returns>
    [HttpPut("{id:guid}/status")]
    [EnableRateLimiting("ProviderSelectionUpdatePolicy")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(ServiceResponseDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    public async Task<IActionResult> UpdateSelectionStatus(
        Guid id,
        [FromBody][Required] ProviderSelectionStatus status)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "User authentication required" });
        }

        // Idempotency check for selection status update
        var operationKey = $"selection:status:{id}:{userId.Value}:{status}";
        if (await _idempotencyService.IsDuplicateOperationAsync(operationKey))
        {
            _logger.LogWarning("Duplicate selection status update request: {SelectionId} to {Status} by user {UserId}",
                id, status, userId.Value);
            return Ok(new { success = true, message = "Selection status already updated (duplicate request ignored)" });
        }

        var ipAddress = GetClientIpAddress();
        var result = await _selectionService.UpdateSelectionStatusAsync(id, status, userId.Value, ipAddress);

        if (!result.Success)
        {
            if (result.Message.Contains("not found"))
                return NotFound(new { message = result.Message });
            if (result.Message.Contains("permission") || result.Message.Contains("don't have"))
                return StatusCode(403, new { message = result.Message });

            return BadRequest(new { message = result.Message });
        }

        // Mark operation as completed
        await _idempotencyService.MarkOperationCompletedAsync(operationKey);

        return Ok(result);
    }

    /// <summary>
    /// Update escrow funding status
    /// </summary>
    /// <param name="id">Selection ID</param>
    /// <param name="isFunded">Whether escrow is funded</param>
    /// <returns>Update result</returns>
    [HttpPut("{id:guid}/escrow")]
    [EnableRateLimiting("ProviderSelectionUpdatePolicy")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(ServiceResponseDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    public async Task<IActionResult> UpdateEscrowStatus(
        Guid id,
        [FromBody][Required] bool isFunded)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "User authentication required" });
        }

        // Idempotency check for escrow funding status update
        var operationKey = $"selection:escrow:{id}:{userId.Value}:{isFunded}";
        if (await _idempotencyService.IsDuplicateOperationAsync(operationKey))
        {
            _logger.LogWarning("Duplicate escrow status update request: {SelectionId} to {IsFunded} by user {UserId}",
                id, isFunded, userId.Value);
            return Ok(new { success = true, message = "Escrow status already updated (duplicate request ignored)" });
        }

        var ipAddress = GetClientIpAddress();
        var result = await _selectionService.UpdateEscrowStatusAsync(id, isFunded, userId.Value, ipAddress);

        if (!result.Success)
        {
            if (result.Message.Contains("not found"))
                return NotFound(new { message = result.Message });
            if (result.Message.Contains("permission") || result.Message.Contains("don't have"))
                return StatusCode(403, new { message = result.Message });

            return BadRequest(new { message = result.Message });
        }

        // Mark operation as completed
        await _idempotencyService.MarkOperationCompletedAsync(operationKey);

        return Ok(result);
    }

    /// <summary>
    /// Update contract signing status
    /// </summary>
    /// <param name="id">Selection ID</param>
    /// <param name="isSigned">Whether contract is signed</param>
    /// <returns>Update result</returns>
    [HttpPut("{id:guid}/contract")]
    [EnableRateLimiting("ProviderSelectionUpdatePolicy")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(ServiceResponseDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    public async Task<IActionResult> UpdateContractStatus(
        Guid id,
        [FromBody][Required] bool isSigned)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "User authentication required" });
        }

        // Idempotency check for contract signing status update
        var operationKey = $"selection:contract:{id}:{userId.Value}:{isSigned}";
        if (await _idempotencyService.IsDuplicateOperationAsync(operationKey))
        {
            _logger.LogWarning("Duplicate contract status update request: {SelectionId} to {IsSigned} by user {UserId}",
                id, isSigned, userId.Value);
            return Ok(new { success = true, message = "Contract status already updated (duplicate request ignored)" });
        }

        var ipAddress = GetClientIpAddress();
        var result = await _selectionService.UpdateContractStatusAsync(id, isSigned, userId.Value, ipAddress);

        if (!result.Success)
        {
            if (result.Message.Contains("not found"))
                return NotFound(new { message = result.Message });
            if (result.Message.Contains("permission") || result.Message.Contains("don't have"))
                return StatusCode(403, new { message = result.Message });

            return BadRequest(new { message = result.Message });
        }

        // Mark operation as completed
        await _idempotencyService.MarkOperationCompletedAsync(operationKey);

        return Ok(result);
    }

    /// <summary>
    /// Cancel a provider selection before work begins
    /// </summary>
    /// <param name="id">Selection ID</param>
    /// <param name="reason">Cancellation reason</param>
    /// <returns>Cancellation result</returns>
    [HttpPost("{id:guid}/cancel")]
    [EnableRateLimiting("ProviderSelectionUpdatePolicy")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(ServiceResponseDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    public async Task<IActionResult> CancelSelection(
        Guid id,
        [FromBody][Required][MinLength(10)] string reason)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "User authentication required" });
        }

        var ipAddress = GetClientIpAddress();
        var result = await _selectionService.CancelSelectionAsync(id, reason, userId.Value, ipAddress);

        if (!result.Success)
        {
            if (result.Message.Contains("not found"))
                return NotFound(new { message = result.Message });
            if (result.Message.Contains("permission") || result.Message.Contains("don't have"))
                return StatusCode(403, new { message = result.Message });

            return BadRequest(new { message = result.Message });
        }

        return Ok(result);
    }

    /// <summary>
    /// Search and filter provider selections
    /// </summary>
    /// <param name="searchDto">Search and filtering criteria</param>
    /// <returns>List of matching selections</returns>
    [HttpGet("search")]
    [ProducesResponseType(typeof(List<ProviderSelectionDto>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> SearchSelections([FromQuery] ProviderSelectionSearchDto searchDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "User authentication required" });
        }

        var selections = await _selectionService.SearchSelectionsAsync(searchDto, userId.Value);

        return Ok(selections);
    }

    /// <summary>
    /// Get selection statistics for current user (as client or provider)
    /// </summary>
    /// <param name="asProvider">Get statistics as provider vs client</param>
    /// <returns>Selection statistics</returns>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(Dictionary<string, object>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetSelectionStatistics([FromQuery] bool asProvider = false)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "User authentication required" });
        }

        Dictionary<string, object> statistics;

        if (asProvider)
        {
            statistics = await _selectionService.GetProviderSelectionStatisticsAsync(userId.Value);
        }
        else
        {
            statistics = await _selectionService.GetClientSelectionStatisticsAsync(userId.Value);
        }

        return Ok(statistics);
    }

    /// <summary>
    /// Get recommended providers for a project
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="take">Number of recommendations to return</param>
    /// <returns>List of recommended applications</returns>
    [HttpGet("recommendations/{projectId:guid}")]
    [ProducesResponseType(typeof(List<ApplicationComparisonDto>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> GetRecommendedProviders(
        Guid projectId,
        [FromQuery][Range(1, 20)] int take = 5)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "User authentication required" });
        }

        List<ApplicationComparisonDto> recommendations;
        try
        {
            recommendations = await _selectionService.GetRecommendedProvidersAsync(
                projectId,
                userId.Value,
                take,
                User.IsInRole("Admin"));
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, new { message = "You don't have permission to view recommendations for this project." });
        }

        if (!recommendations.Any())
        {
            return NotFound(new { message = "No recommended providers found for this project" });
        }

        return Ok(recommendations);
    }

    /// <summary>
    /// Initiate escrow setup for a selection
    /// </summary>
    /// <param name="id">Selection ID</param>
    /// <returns>Escrow initiation result</returns>
    [HttpPost("{id:guid}/initiate-escrow")]
    [EnableRateLimiting("ProviderSelectionUpdatePolicy")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(ServiceResponseDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    public async Task<IActionResult> InitiateEscrow(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "User authentication required" });
        }

        var result = await _selectionService.InitiateEscrowAsync(id, userId.Value);

        if (!result.Success)
        {
            if (result.Message.Contains("not found"))
                return NotFound(new { message = result.Message });
            if (result.Message.Contains("permission") || result.Message.Contains("don't have"))
                return StatusCode(403, new { message = result.Message });

            return BadRequest(new { message = result.Message });
        }

        return Ok(result);
    }

    /// <summary>
    /// Check if a project is ready for provider selection
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <returns>Whether project is ready for selection</returns>
    [HttpGet("ready/{projectId:guid}")]
    [ProducesResponseType(typeof(object), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> IsProjectReadyForSelection(Guid projectId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "User authentication required" });
        }

        var isReady = await _selectionService.IsProjectReadyForSelectionAsync(
            projectId,
            userId.Value,
            User.IsInRole("Admin"));

        return Ok(new { projectId, isReady });
    }

    private new Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private string GetClientIpAddress()
    {
        return SkillLedger.Infrastructure.Services.TrustedClientIpResolver.GetClientIpAddress(HttpContext);
    }
}
