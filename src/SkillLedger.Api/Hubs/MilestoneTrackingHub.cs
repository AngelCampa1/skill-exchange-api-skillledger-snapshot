using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;
using System.Security.Claims;

namespace SkillLedger.Api.Hubs;

/// <summary>
/// SignalR Hub for real-time milestone tracking and progress updates
/// Provides workspace-based group communication for milestone events
/// </summary>
[Authorize]
public class MilestoneTrackingHub : Hub
{
    private readonly SkillLedgerDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<MilestoneTrackingHub> _logger;

    public MilestoneTrackingHub(
        SkillLedgerDbContext context,
        IAuditLogService auditLogService,
        ILogger<MilestoneTrackingHub> logger)
    {
        _context = context;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// Join milestone tracking group for a specific project
    /// </summary>
    /// <param name="projectId">Project ID to track milestones for</param>
    public async Task JoinProjectMilestoneTrackingAsync(string projectId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                await Clients.Caller.SendAsync("Error", "User not authenticated");
                return;
            }

            // Validate user has access to this project
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == Guid.Parse(projectId) &&
                              p.ClientId == userId.Value);

            if (!hasAccess)
            {
                await Clients.Caller.SendAsync("Error", "Access denied to project milestones");
                await _auditLogService.LogEventAsync(
                    userId.Value,
                    "MILESTONE_HUB_ACCESS_DENIED",
                    "unknown",
                    null,
                    false,
                    $"Unauthorized access attempt to project {projectId} milestone tracking",
                    null);
                return;
            }

            // Join project milestone tracking group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"milestone_project_{projectId}");

            await Clients.Caller.SendAsync("JoinedMilestoneTracking", projectId);

            await _auditLogService.LogEventAsync(
                userId.Value,
                "MILESTONE_HUB_JOINED",
                "unknown",
                null,
                true,
                $"Joined milestone tracking for project {projectId}",
                null);

            _logger.LogInformation("User {UserId} joined milestone tracking for project {ProjectId}", userId, projectId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining milestone tracking for project {ProjectId}", projectId);
            await Clients.Caller.SendAsync("Error", "Failed to join milestone tracking");
        }
    }

    /// <summary>
    /// Leave milestone tracking group for a specific project
    /// </summary>
    /// <param name="projectId">Project ID to stop tracking</param>
    public async Task LeaveMilestoneTrackingAsync(string projectId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null) return;

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"milestone_project_{projectId}");
            await Clients.Caller.SendAsync("LeftMilestoneTracking", projectId);

            _logger.LogInformation("User {UserId} left milestone tracking for project {ProjectId}", userId, projectId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving milestone tracking for project {ProjectId}", projectId);
        }
    }

    /// <summary>
    /// Join tracking for a specific milestone
    /// </summary>
    /// <param name="milestoneId">Milestone ID to track</param>
    public async Task JoinMilestoneTrackingAsync(string milestoneId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                await Clients.Caller.SendAsync("Error", "User not authenticated");
                return;
            }

            // Validate user has access to this milestone
            var milestone = await _context.ProjectMilestones
                .Include(m => m.Project)
                .FirstOrDefaultAsync(m => m.Id == Guid.Parse(milestoneId));

            if (milestone == null || milestone.Project.ClientId != userId.Value)
            {
                await Clients.Caller.SendAsync("Error", "Access denied to milestone");
                return;
            }

            // Join milestone-specific group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"milestone_{milestoneId}");

            await Clients.Caller.SendAsync("JoinedMilestone", milestoneId);

            _logger.LogInformation("User {UserId} joined tracking for milestone {MilestoneId}", userId, milestoneId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining milestone tracking for {MilestoneId}", milestoneId);
            await Clients.Caller.SendAsync("Error", "Failed to join milestone tracking");
        }
    }

    /// <summary>
    /// Leave tracking for a specific milestone
    /// </summary>
    /// <param name="milestoneId">Milestone ID to stop tracking</param>
    public async Task LeaveMilestoneAsync(string milestoneId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null) return;

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"milestone_{milestoneId}");
            await Clients.Caller.SendAsync("LeftMilestone", milestoneId);

            _logger.LogInformation("User {UserId} left tracking for milestone {MilestoneId}", userId, milestoneId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving milestone tracking for {MilestoneId}", milestoneId);
        }
    }

    /// <summary>
    /// Update milestone progress in real-time
    /// Called by clients to broadcast progress updates
    /// </summary>
    /// <param name="milestoneId">Milestone ID</param>
    /// <param name="progressPercentage">New progress percentage</param>
    public async Task UpdateMilestoneProgressAsync(string milestoneId, decimal progressPercentage)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                await Clients.Caller.SendAsync("Error", "User not authenticated");
                return;
            }

            // Validate progress percentage
            if (progressPercentage < 0 || progressPercentage > 100)
            {
                await Clients.Caller.SendAsync("Error", "Invalid progress percentage");
                return;
            }

            // Validate user can update this milestone
            var milestone = await _context.ProjectMilestones
                .Include(m => m.Project)
                .FirstOrDefaultAsync(m => m.Id == Guid.Parse(milestoneId));

            if (milestone == null || milestone.Project.ClientId != userId.Value)
            {
                await Clients.Caller.SendAsync("Error", "Access denied to update milestone");
                return;
            }

            // Broadcast progress update to all tracking this milestone
            var progressUpdate = new
            {
                MilestoneId = milestoneId,
                ProgressPercentage = progressPercentage,
                UpdatedBy = userId,
                UpdatedAt = DateTime.UtcNow
            };

            await Clients.Group($"milestone_{milestoneId}")
                .SendAsync("ProgressUpdated", progressUpdate);

            await Clients.Group($"milestone_project_{milestone.ProjectId}")
                .SendAsync("MilestoneProgressChanged", progressUpdate);

            _logger.LogInformation("Progress updated for milestone {MilestoneId} to {Progress}% by user {UserId}",
                milestoneId, progressPercentage, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating progress for milestone {MilestoneId}", milestoneId);
            await Clients.Caller.SendAsync("Error", "Failed to update milestone progress");
        }
    }

    /// <summary>
    /// Send typing indicator for milestone comments/notes
    /// </summary>
    /// <param name="milestoneId">Milestone ID</param>
    /// <param name="isTyping">Whether user is currently typing</param>
    public async Task SendTypingIndicatorAsync(string milestoneId, bool isTyping)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null) return;

            var typingIndicator = new
            {
                MilestoneId = milestoneId,
                UserId = userId,
                IsTyping = isTyping,
                Timestamp = DateTime.UtcNow
            };

            // Send to others tracking this milestone (exclude sender)
            await Clients.GroupExcept($"milestone_{milestoneId}", Context.ConnectionId)
                .SendAsync("TypingIndicator", typingIndicator);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending typing indicator for milestone {MilestoneId}", milestoneId);
        }
    }

    public override async Task OnConnectedAsync()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId != null)
            {
                _logger.LogInformation("User {UserId} connected to milestone tracking hub", userId);

                await _auditLogService.LogEventAsync(
                    userId.Value,
                    "MILESTONE_HUB_CONNECTED",
                    "unknown",
                    null,
                    true,
                    "Connected to milestone tracking hub",
                    null);
            }

            await base.OnConnectedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error on milestone hub connection");
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId != null)
            {
                _logger.LogInformation("User {UserId} disconnected from milestone tracking hub", userId);

                if (exception != null)
                {
                    _logger.LogWarning(exception, "User {UserId} disconnected with exception", userId);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error on milestone hub disconnection");
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Get the current user's ID from the hub context
    /// </summary>
    private Guid? GetCurrentUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
        {
            return null;
        }

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    #endregion
}
