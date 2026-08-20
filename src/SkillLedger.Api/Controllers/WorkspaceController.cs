using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Interfaces;
using System.Security.Claims;

namespace SkillLedger.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [EnableRateLimiting("WorkspacePolicy")]
    public class WorkspaceController : ControllerBase
    {
        private readonly IWorkspaceService _workspaceService;
        private readonly ILogger<WorkspaceController> _logger;

        public WorkspaceController(
            IWorkspaceService workspaceService,
            ILogger<WorkspaceController> logger)
        {
            _workspaceService = workspaceService;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new project workspace
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ProjectWorkspace), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateWorkspace([FromBody] CreateWorkspaceRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var workspace = await _workspaceService.CreateWorkspaceAsync(request.ProjectId, request.ProviderId, userId);

                _logger.LogInformation("Workspace {WorkspaceId} created for project {ProjectId}",
                    workspace.Id, request.ProjectId);

                return CreatedAtAction(nameof(GetWorkspace), new { id = workspace.Id }, workspace);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid project ID in workspace creation: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Workspace creation conflict: {Message}", ex.Message);
                return Conflict(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Unauthorized workspace creation attempt: {Message}", ex.Message);
                return Forbid();
            }
        }

        /// <summary>
        /// Gets workspace dashboard data
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(WorkspaceDashboardDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetWorkspace(Guid id)
        {
            var userId = GetCurrentUserId();

            try
            {
                var dashboard = await _workspaceService.GetWorkspaceDashboardAsync(id, userId);
                return Ok(dashboard);
            }
            catch (UnauthorizedAccessException)
            {
                return NotFound(); // Don't reveal workspace existence to unauthorized users
            }
        }

        /// <summary>
        /// Gets current user's workspaces
        /// </summary>
        [HttpGet("my-workspaces")]
        [ProducesResponseType(typeof(IEnumerable<WorkspaceListDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserWorkspaces()
        {
            var userId = GetCurrentUserId();
            var workspaces = await _workspaceService.GetUserWorkspacesAsync(userId);
            return Ok(workspaces);
        }

        /// <summary>
        /// Gets workspace by project ID
        /// </summary>
        [HttpGet("project/{projectId:guid}")]
        [ProducesResponseType(typeof(WorkspaceDashboardDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetWorkspaceByProject(Guid projectId)
        {
            var userId = GetCurrentUserId();
            var workspace = await _workspaceService.GetWorkspaceByProjectAsync(projectId, userId);

            if (workspace == null)
            {
                return NotFound();
            }

            var dashboard = await _workspaceService.GetWorkspaceDashboardAsync(workspace.Id, userId);
            return Ok(dashboard);
        }

        /// <summary>
        /// Archives a workspace
        /// </summary>
        [HttpPost("{id:guid}/archive")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ArchiveWorkspace(Guid id)
        {
            var userId = GetCurrentUserId();
            var result = await _workspaceService.ArchiveWorkspaceAsync(id, userId);

            if (!result)
            {
                return NotFound();
            }

            return Ok(new { message = "Workspace archived successfully" });
        }

        /// <summary>
        /// Updates workspace timeline data
        /// </summary>
        [HttpPut("{id:guid}/timeline")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateTimeline(Guid id, [FromBody] UpdateTimelineRequest request)
        {
            var userId = GetCurrentUserId();
            var result = await _workspaceService.UpdateTimelineAsync(id, userId, request.TimelineData);

            if (!result)
            {
                return NotFound();
            }

            return Ok(new { message = "Timeline updated successfully" });
        }

        /// <summary>
        /// Updates workspace milestone data
        /// </summary>
        [HttpPut("{id:guid}/milestones")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateMilestones(Guid id, [FromBody] UpdateTimelineRequest request)
        {
            var userId = GetCurrentUserId();
            var result = await _workspaceService.UpdateMilestonesAsync(id, userId, request.TimelineData);

            if (!result)
            {
                return NotFound();
            }

            return Ok(new { message = "Milestones updated successfully" });
        }

        /// <summary>
        /// Checks if user has access to workspace (for integration purposes)
        /// </summary>
        [HttpGet("{id:guid}/access")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> CheckAccess(Guid id)
        {
            var userId = GetCurrentUserId();
            var hasAccess = await _workspaceService.HasUserAccessAsync(id, userId);
            return Ok(new { hasAccess });
        }

        /// <summary>
        /// Updates integration status (internal use)
        /// </summary>
        [HttpPut("{id:guid}/integration-status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Policy = "RequireAdminRole")] // Only admins can update integration status
        public async Task<IActionResult> UpdateIntegrationStatus(Guid id, [FromBody] UpdateIntegrationStatusRequest request)
        {
            var result = await _workspaceService.UpdateIntegrationStatusAsync(id, request.Status);

            if (!result)
            {
                return NotFound();
            }

            return Ok(new { message = "Integration status updated successfully" });
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid user ID in token");
            }
            return userId;
        }
    }

    public class UpdateIntegrationStatusRequest
    {
        public string Status { get; set; } = null!;
    }
}
