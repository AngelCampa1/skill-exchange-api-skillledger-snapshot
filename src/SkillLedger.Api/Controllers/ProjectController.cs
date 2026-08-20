using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Claims;

namespace SkillLedger.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Require authentication for all project endpoints
[EnableRateLimiting("ProjectCreationPolicy")]
public class ProjectController : BaseApiController
{
    private readonly IProjectService _projectService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<ProjectController> _logger;

    public ProjectController(
        IProjectService projectService,
        IAuditLogService auditLogService,
        ILogger<ProjectController> logger)
    {
        _projectService = projectService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new project
    /// </summary>
    /// <param name="createDto">Project creation details</param>
    /// <returns>Created project details</returns>
    [HttpPost]
    [EnableRateLimiting("ProjectCreationPolicy")]
    [ValidateAntiForgeryToken]  // SECURITY FIX: CSRF protection enabled in ALL environments
    [ProducesResponseType(typeof(ProjectResponseDto), (int)HttpStatusCode.Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType((int)HttpStatusCode.TooManyRequests)]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectDto createDto)
    {
        // BUG-002 FIX: Add detailed logging to diagnose 400 errors
        _logger.LogInformation("CreateProject called. Title: {Title}, SkillCount: {SkillCount}, DeliverableCount: {DeliverableCount}",
            createDto?.Title ?? "null",
            createDto?.RequiredSkills?.Count ?? 0,
            createDto?.Deliverables?.Count ?? 0);

        if (!ModelState.IsValid)
        {
            // Log validation errors for debugging
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                );
            _logger.LogWarning("Project creation validation failed: {@ValidationErrors}", errors);
            return BadRequest(ModelState);
        }

        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "User authentication required" });
        }

        var ipAddress = GetClientIpAddress();
        var result = await _projectService.CreateProjectAsync(createDto, userId.Value, ipAddress);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        if (result.Project?.Id == null)
        {
            return BadRequest(new { message = "Project creation succeeded but project data is missing" });
        }

        return CreatedAtAction(nameof(GetProject), new { id = result.Project.Id }, result);
    }

    /// <summary>
    /// Update an existing project
    /// </summary>
    /// <param name="id">Project ID</param>
    /// <param name="updateDto">Project update details</param>
    /// <returns>Updated project details</returns>
    [HttpPut("{id:guid}")]
    [EnableRateLimiting("ProjectUpdatePolicy")]
    [ValidateAntiForgeryToken]  // SECURITY FIX: CSRF protection enabled in ALL environments
    [ProducesResponseType(typeof(ProjectResponseDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.TooManyRequests)]
    public async Task<IActionResult> UpdateProject(Guid id, [FromBody] UpdateProjectDto updateDto)
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
        var result = await _projectService.UpdateProjectAsync(id, updateDto, userId.Value, ipAddress);

        if (!result.Success)
        {
            // Determine appropriate HTTP status code based on error
            if (result.Message.Contains("not found"))
                return NotFound(new { message = result.Message });
            if (result.Message.Contains("permission") || result.Message.Contains("unauthorized"))
                return StatusCode(403, new { message = result.Message });

            return BadRequest(new { message = result.Message });
        }

        return Ok(result);
    }

    /// <summary>
    /// Save project as draft (allows partial information)
    /// </summary>
    /// <param name="saveDraftDto">Draft project details</param>
    /// <returns>Saved draft details</returns>
    [HttpPost("draft")]
    [EnableRateLimiting("ProjectCreationPolicy")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(ProjectResponseDto), (int)HttpStatusCode.Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType((int)HttpStatusCode.TooManyRequests)]
    public async Task<IActionResult> SaveDraft([FromBody] SaveDraftProjectDto saveDraftDto)
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
        var result = await _projectService.SaveProjectDraftAsync(saveDraftDto, userId.Value, ipAddress);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        if (result.Project?.Id == null)
        {
            return BadRequest(new { message = "Project draft creation succeeded but project data is missing" });
        }

        return CreatedAtAction(nameof(GetProject), new { id = result.Project.Id }, result);
    }

    /// <summary>
    /// Update an existing draft project
    /// </summary>
    /// <param name="id">Project ID</param>
    /// <param name="saveDraftDto">Updated draft details</param>
    /// <returns>Updated draft details</returns>
    [HttpPut("{id:guid}/draft")]
    [EnableRateLimiting("ProjectUpdatePolicy")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(ProjectResponseDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.TooManyRequests)]
    public async Task<IActionResult> UpdateDraft(Guid id, [FromBody] SaveDraftProjectDto saveDraftDto)
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
        var result = await _projectService.UpdateProjectDraftAsync(id, saveDraftDto, userId.Value, ipAddress);

        if (!result.Success)
        {
            if (result.Message.Contains("not found"))
                return NotFound(new { message = result.Message });
            if (result.Message.Contains("permission") || result.Message.Contains("unauthorized"))
                return StatusCode(403, new { message = result.Message });

            return BadRequest(new { message = result.Message });
        }

        return Ok(result);
    }

    /// <summary>
    /// Publish a draft project (submit for moderation)
    /// </summary>
    /// <param name="id">Project ID</param>
    /// <returns>Publication result</returns>
    [HttpPost("{id:guid}/publish")]
    [EnableRateLimiting("ProjectPublishPolicy")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(ServiceResponseDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.TooManyRequests)]
    public async Task<IActionResult> PublishProject(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "User authentication required" });
        }

        var ipAddress = GetClientIpAddress();
        var result = await _projectService.PublishProjectAsync(id, userId.Value, ipAddress);

        if (!result.Success)
        {
            if (result.Message.Contains("not found"))
                return NotFound(new { message = result.Message });
            if (result.Message.Contains("permission") || result.Message.Contains("unauthorized"))
                return StatusCode(403, new { message = result.Message });

            return BadRequest(new { message = result.Message });
        }

        return Ok(result);
    }

    /// <summary>
    /// Get a project by ID
    /// SECURITY FIX: Authorization now handled at database level to prevent IDOR
    /// </summary>
    /// <param name="id">Project ID</param>
    /// <returns>Project details</returns>
    [HttpGet("{id:guid}")]
    [AllowAnonymous] // Allow anonymous access for published projects
    [ProducesResponseType(typeof(ProjectDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> GetProject(Guid id)
    {
        var userId = GetCurrentUserId();
        var ipAddress = GetClientIpAddress();

        // SECURITY FIX: Pass requesting user ID to service layer
        // Authorization is enforced at database query level
        var project = await _projectService.GetProjectByIdAsync(id, userId);

        if (project == null)
        {
            // Log unauthorized access attempts for security monitoring
            if (userId.HasValue)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // BUG-BE-003 FIX: Add error handling to prevent silent failures
                        await _auditLogService.LogEventAsync(
                            userId.Value,
                            "PROJECT_ACCESS_DENIED",
                            ipAddress,
                            Request.Headers.UserAgent.ToString(),
                            false,
                            $"{{\"ProjectId\":\"{id}\"}}",
                            "Attempted to access unauthorized or non-existent project"
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to log unauthorized access attempt for user {UserId}, project {ProjectId}", userId.Value, id);
                    }
                });
            }

            // Always return same error message to prevent enumeration
            return NotFound(new { message = "Project not found" });
        }

        // Log successful project views for analytics
        // Use sampling to reduce audit log volume (log 1 in 10 views)
        // SECURITY FIX: Use cryptographic random to prevent predictable audit bypass
        bool shouldLog = false;
        if (userId.HasValue)
        {
            Span<byte> randomBytes = stackalloc byte[1];
            System.Security.Cryptography.RandomNumberGenerator.Fill(randomBytes);
            shouldLog = (randomBytes[0] % 10) == 0;
        }

        if (shouldLog)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    // BUG-BE-003 FIX: Add error handling to prevent silent failures
                    await _auditLogService.LogEventAsync(
                        userId.Value,
                        "PROJECT_VIEW",
                        ipAddress,
                        Request.Headers.UserAgent.ToString(),
                        true,
                        $"{{\"ProjectId\":\"{id}\",\"ProjectTitle\":\"{project.Title}\"}}",
                        "Project viewed"
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to log project view audit event for user {UserId}, project {ProjectId}", userId.Value, id);
                }
            });
        }

        return Ok(project);
    }

    /// <summary>
    /// Get projects for current user
    /// </summary>
    /// <param name="includeNonPublic">Include draft and non-public projects</param>
    /// <param name="skip">Number of projects to skip (pagination)</param>
    /// <param name="take">Number of projects to take (pagination)</param>
    /// <returns>List of user's projects</returns>
    [HttpGet("my-projects")]
    [ProducesResponseType(typeof(List<ProjectDto>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> GetMyProjects(
        [FromQuery] bool includeNonPublic = true,
        [FromQuery] int skip = 0,
        [FromQuery][Range(1, 100)] int take = 20)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "User authentication required" });
        }

        var projects = await _projectService.GetProjectsByClientAsync(userId.Value, includeNonPublic, skip, take);

        return Ok(projects);
    }

    /// <summary>
    /// Search projects with filtering and pagination
    /// </summary>
    /// <param name="searchDto">Search criteria</param>
    /// <returns>List of matching projects</returns>
    [HttpGet("search")]
    [AllowAnonymous] // Allow anonymous search for published projects
    [ProducesResponseType(typeof(List<ProjectSummaryDto>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> SearchProjects([FromQuery] ProjectSearchDto searchDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Ensure anonymous users can only see published projects
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            searchDto.PublishedOnly = true;
        }

        var projects = await _projectService.SearchProjectsAsync(searchDto);
        var totalCount = await _projectService.CountProjectsAsync(searchDto);

        // Log search for analytics (optional)
        if (!string.IsNullOrWhiteSpace(searchDto.Query))
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    // BUG-BE-003 FIX: Add error handling to prevent silent failures
                    await _auditLogService.LogEventAsync(
                        userId,
                        "PROJECT_SEARCH",
                        GetClientIpAddress(),
                        Request.Headers.UserAgent.ToString(),
                        true,
                        $"{{\"Query\":\"{searchDto.Query}\",\"ResultCount\":{projects.Count}}}",
                        "Project search performed"
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to log project search audit event. Query: {Query}, ResultCount: {ResultCount}", searchDto.Query, projects.Count);
                }
            });
        }

        Response.Headers.Append("X-Total-Count", totalCount.ToString());
        Response.Headers.Append("X-Page-Size", searchDto.Take.ToString());
        Response.Headers.Append("X-Page-Number", ((searchDto.Skip / searchDto.Take) + 1).ToString());

        return Ok(projects);
    }

    /// <summary>
    /// Get marketplace projects (public-facing project listing)
    /// </summary>
    /// <param name="searchDto">Search and filter criteria</param>
    /// <returns>List of published projects available in marketplace</returns>
    [HttpGet("marketplace")]
    [AllowAnonymous] // Public marketplace access
    [ProducesResponseType(typeof(List<ProjectSummaryDto>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> GetMarketplaceProjects([FromQuery] ProjectSearchDto searchDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Marketplace always shows only published projects
        searchDto.PublishedOnly = true;

        var projects = await _projectService.SearchProjectsAsync(searchDto);
        var totalCount = await _projectService.CountProjectsAsync(searchDto);

        // Log marketplace access for analytics
        var userId = GetCurrentUserId();
        _ = Task.Run(async () =>
        {
            try
            {
                // BUG-BE-003 FIX: Add error handling to prevent silent failures
                await _auditLogService.LogEventAsync(
                    userId,
                    "MARKETPLACE_VIEW",
                    GetClientIpAddress(),
                    Request.Headers.UserAgent.ToString(),
                    true,
                    $"{{\"ResultCount\":{projects.Count}}}",
                    "Marketplace projects viewed"
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to log marketplace view audit event. ResultCount: {ResultCount}", projects.Count);
            }
        });

        Response.Headers.Append("X-Total-Count", totalCount.ToString());
        Response.Headers.Append("X-Page-Size", searchDto.Take.ToString());
        Response.Headers.Append("X-Page-Number", ((searchDto.Skip / searchDto.Take) + 1).ToString());

        return Ok(projects);
    }

    /// <summary>
    /// Delete a project (soft delete by cancelling)
    /// </summary>
    /// <param name="id">Project ID</param>
    /// <returns>Deletion result</returns>
    [HttpDelete("{id:guid}")]
    [EnableRateLimiting("ProjectDeletionPolicy")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(ServiceResponseDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.TooManyRequests)]
    public async Task<IActionResult> DeleteProject(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "User authentication required" });
        }

        var ipAddress = GetClientIpAddress();
        var result = await _projectService.DeleteProjectAsync(id, userId.Value, ipAddress);

        if (!result.Success)
        {
            if (result.Message.Contains("not found"))
                return NotFound(new { message = result.Message });
            if (result.Message.Contains("permission") || result.Message.Contains("unauthorized"))
                return StatusCode(403, new { message = result.Message });

            return BadRequest(new { message = result.Message });
        }

        return Ok(result);
    }

    /// <summary>
    /// Get project statistics for the current user
    /// </summary>
    /// <returns>Project statistics</returns>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(object), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> GetProjectStatistics()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "User authentication required" });
        }

        var statistics = await _projectService.GetProjectStatisticsAsync(userId.Value);

        return Ok(statistics);
    }

    /// <summary>
    /// Moderate a project (Admin/Moderator only)
    /// </summary>
    /// <param name="id">Project ID</param>
    /// <param name="moderationStatus">New moderation status</param>
    /// <param name="notes">Optional moderation notes</param>
    /// <returns>Moderation result</returns>
    [HttpPost("{id:guid}/moderate")]
    [Authorize(Policy = "RequireModeratorPermission")] // Require moderator permission
    [EnableRateLimiting("ModerationPolicy")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(ServiceResponseDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.TooManyRequests)]
    public async Task<IActionResult> ModerateProject(
        Guid id,
        [FromQuery][Required] string moderationStatus,
        [FromQuery] string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(moderationStatus))
        {
            return BadRequest(new { message = "Moderation status is required" });
        }

        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "User authentication required" });
        }

        var ipAddress = GetClientIpAddress();
        var result = await _projectService.ModerateProjectAsync(id, moderationStatus, userId.Value, notes, ipAddress);

        if (!result.Success)
        {
            if (result.Message.Contains("not found"))
                return NotFound(new { message = result.Message });

            return BadRequest(new { message = result.Message });
        }

        return Ok(result);
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
