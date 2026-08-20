using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Services;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Claims;

namespace SkillLedger.Api.Controllers;

/// <summary>
/// Controller for project application functionality
/// </summary>
[ApiController]
[Route("api/project-applications")]
[Authorize] // Require authentication for all application endpoints
public class ProjectApplicationController : BaseApiController
{
    private readonly IProjectApplicationService _applicationService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<ProjectApplicationController> _logger;
    private readonly Core.Interfaces.IIdempotencyService _idempotencyService;

    public ProjectApplicationController(
        IProjectApplicationService applicationService,
        IAuditLogService auditLogService,
        ILogger<ProjectApplicationController> logger,
        Core.Interfaces.IIdempotencyService idempotencyService)
    {
        _applicationService = applicationService;
        _auditLogService = auditLogService;
        _logger = logger;
        _idempotencyService = idempotencyService;
    }

    /// <summary>
    /// Submit a new project application
    /// </summary>
    /// <param name="createDto">Application details</param>
    /// <returns>Application submission result</returns>
    [HttpPost]
    [EnableRateLimiting("ProjectApplicationPolicy")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(ServiceResponseDto), (int)HttpStatusCode.Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType((int)HttpStatusCode.TooManyRequests)]
    public async Task<IActionResult> SubmitApplication([FromBody] CreateProjectApplicationDto createDto)
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
        var result = await _applicationService.SubmitApplicationAsync(createDto, userId.Value, ipAddress);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        // Return 201 Created with application ID
        return Created($"/api/project-applications/{result.Data}", new
        {
            message = result.Message,
            applicationId = result.Data
        });
    }

    /// <summary>
    /// Get a specific project application by ID
    /// </summary>
    /// <param name="id">Application ID</param>
    /// <returns>Application details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProjectApplicationDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    public async Task<IActionResult> GetApplication(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "User authentication required" });
        }

        var application = await _applicationService.GetApplicationByIdAsync(id, userId.Value);

        if (application == null)
        {
            return NotFound(new { message = "Application not found or access denied" });
        }

        return Ok(application);
    }

    /// <summary>
    /// Get applications for a specific project (client view)
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="searchDto">Search and filtering criteria</param>
    /// <returns>List of applications for the project</returns>
    [HttpGet("project/{projectId:guid}")]
    [ProducesResponseType(typeof(ApplicationSearchResultDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    public async Task<IActionResult> GetProjectApplications(
        Guid projectId,
        [FromQuery] ApplicationSearchDto searchDto)
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

        var result = await _applicationService.GetProjectApplicationsAsync(projectId, userId.Value, searchDto);

        // Add pagination headers
        Response.Headers.Append("X-Total-Count", result.TotalCount.ToString());
        Response.Headers.Append("X-Page-Size", result.PageSize.ToString());
        Response.Headers.Append("X-Page-Number", result.CurrentPage.ToString());
        Response.Headers.Append("X-Total-Pages", result.TotalPages.ToString());

        return Ok(result);
    }

    /// <summary>
    /// Get applications submitted by the current provider
    /// </summary>
    /// <param name="searchDto">Search and filtering criteria</param>
    /// <returns>List of provider's applications</returns>
    [HttpGet("my-applications")]
    [ProducesResponseType(typeof(ApplicationSearchResultDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> GetMyApplications([FromQuery] ApplicationSearchDto searchDto)
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

        var result = await _applicationService.GetProviderApplicationsAsync(userId.Value, searchDto);

        // Add pagination headers
        Response.Headers.Append("X-Total-Count", result.TotalCount.ToString());
        Response.Headers.Append("X-Page-Size", result.PageSize.ToString());
        Response.Headers.Append("X-Page-Number", result.CurrentPage.ToString());
        Response.Headers.Append("X-Total-Pages", result.TotalPages.ToString());

        return Ok(result);
    }

    /// <summary>
    /// Update application status (by client - accept, reject, etc.)
    /// </summary>
    /// <param name="id">Application ID</param>
    /// <param name="updateDto">Status update details</param>
    /// <returns>Status update result</returns>
    [HttpPut("{id:guid}/status")]
    [EnableRateLimiting("ProjectApplicationStatusUpdatePolicy")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(ServiceResponseDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    public async Task<IActionResult> UpdateApplicationStatus(
        Guid id,
        [FromBody] UpdateApplicationStatusDto updateDto)
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

        // Idempotency check for application status update
        var operationKey = $"application:status:{id}:{userId.Value}:{updateDto.Status}";
        if (await _idempotencyService.IsDuplicateOperationAsync(operationKey))
        {
            _logger.LogWarning("Duplicate application status update request: {ApplicationId} to {Status} by user {UserId}",
                id, updateDto.Status, userId.Value);
            return Ok(new { success = true, message = "Application status already updated (duplicate request ignored)" });
        }

        var ipAddress = GetClientIpAddress();
        var result = await _applicationService.UpdateApplicationStatusAsync(id, updateDto, userId.Value, ipAddress);

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
    /// Withdraw an application (by provider)
    /// </summary>
    /// <param name="id">Application ID</param>
    /// <param name="reason">Optional withdrawal reason</param>
    /// <returns>Withdrawal result</returns>
    [HttpPost("{id:guid}/withdraw")]
    [EnableRateLimiting("ProjectApplicationWithdrawPolicy")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(ServiceResponseDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    public async Task<IActionResult> WithdrawApplication(
        Guid id,
        [FromQuery] string? reason = null)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "User authentication required" });
        }

        // Idempotency check for application withdrawal
        var operationKey = $"application:withdraw:{id}:{userId.Value}";
        if (await _idempotencyService.IsDuplicateOperationAsync(operationKey))
        {
            _logger.LogWarning("Duplicate application withdraw request: {ApplicationId} by user {UserId}",
                id, userId.Value);
            return Ok(new { success = true, message = "Application already withdrawn (duplicate request ignored)" });
        }

        var ipAddress = GetClientIpAddress();
        var result = await _applicationService.WithdrawApplicationAsync(id, userId.Value, reason, ipAddress);

        if (!result.Success)
        {
            if (result.Message.Contains("not found") || result.Message.Contains("permission"))
                return NotFound(new { message = result.Message });

            return BadRequest(new { message = result.Message });
        }

        // Mark operation as completed
        await _idempotencyService.MarkOperationCompletedAsync(operationKey);

        return Ok(result);
    }

    /// <summary>
    /// Check if current provider can apply to a project
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <returns>Whether provider can apply</returns>
    [HttpGet("can-apply/{projectId:guid}")]
    [ProducesResponseType(typeof(object), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> CanApplyToProject(Guid projectId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "User authentication required" });
        }

        var canApply = await _applicationService.CanProviderApplyToProjectAsync(projectId, userId.Value);

        return Ok(new { canApply, projectId });
    }

    /// <summary>
    /// Get application statistics for current user
    /// </summary>
    /// <param name="asClient">Get statistics as client (for received applications) vs provider (for submitted applications)</param>
    /// <returns>Application statistics</returns>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(ApplicationStatisticsDto), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetApplicationStatistics([FromQuery] bool asClient = false)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "User authentication required" });
        }

        ApplicationStatisticsDto statistics;

        if (asClient)
        {
            statistics = await _applicationService.GetClientApplicationStatisticsAsync(userId.Value);
        }
        else
        {
            statistics = await _applicationService.GetProviderApplicationStatisticsAsync(userId.Value);
        }

        return Ok(statistics);
    }

    /// <summary>
    /// Get recommended projects for the current provider
    /// </summary>
    /// <param name="take">Number of recommendations to return</param>
    /// <returns>List of recommended projects</returns>
    [HttpGet("recommended-projects")]
    [ProducesResponseType(typeof(List<ProjectSummaryDto>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetRecommendedProjects([FromQuery][Range(1, 50)] int take = 10)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "User authentication required" });
        }

        var recommendations = await _applicationService.GetRecommendedProjectsForProviderAsync(userId.Value, take);

        return Ok(recommendations);
    }

    /// <summary>
    /// Calculate skill match score for a project and current provider
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <returns>Skill match score</returns>
    [HttpGet("skill-match/{projectId:guid}")]
    [ProducesResponseType(typeof(object), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetSkillMatchScore(Guid projectId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "User authentication required" });
        }

        var skillMatchScore = await _applicationService.CalculateSkillMatchScoreAsync(projectId, userId.Value);

        return Ok(new
        {
            projectId,
            skillMatchScore,
            matchPercentage = Math.Round(skillMatchScore * 100, 1)
        });
    }

    /// <summary>
    /// Administrative endpoint to expire old applications (Admin/System only)
    /// </summary>
    /// <param name="expiredAfterDays">Applications older than this many days will be expired</param>
    /// <returns>Number of applications expired</returns>
    [HttpPost("admin/expire-old")]
    [Authorize(Policy = "RequireAdminPermission")]
    [EnableRateLimiting("AdminOperationPolicy")]
    [ProducesResponseType(typeof(object), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    public async Task<IActionResult> ExpireOldApplications([FromQuery][Range(1, 365)] int expiredAfterDays = 30)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "User authentication required" });
        }

        var expiredCount = await _applicationService.ExpireOldApplicationsAsync(expiredAfterDays);

        // Log the administrative action
        await _auditLogService.LogEventAsync(
            userId.Value,
            "APPLICATIONS_EXPIRED_BULK",
            GetClientIpAddress(),
            Request.Headers.UserAgent.ToString(),
            true,
            $"{{\"ExpiredCount\":{expiredCount},\"ExpiredAfterDays\":{expiredAfterDays}}}",
            "Bulk expired old applications"
        );

        _logger.LogInformation("Admin {UserId} expired {Count} applications older than {Days} days",
            userId.Value, expiredCount, expiredAfterDays);

        return Ok(new
        {
            message = $"Successfully expired {expiredCount} applications older than {expiredAfterDays} days.",
            expiredCount,
            expiredAfterDays
        });
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
