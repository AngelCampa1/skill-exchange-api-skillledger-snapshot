using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SkillLedger.Core.Interfaces;
using System.Security.Claims;

namespace SkillLedger.Api.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time messaging functionality
    /// Handles real-time message delivery, typing indicators, and read receipts
    /// </summary>
    [Authorize]
    public class MessagingHub : Hub
    {
        private readonly IMessagingService _messagingService;
        private readonly IWorkspaceService _workspaceService;
        private readonly ILogger<MessagingHub> _logger;

        public MessagingHub(
            IMessagingService messagingService,
            IWorkspaceService workspaceService,
            ILogger<MessagingHub> logger)
        {
            _messagingService = messagingService;
            _workspaceService = workspaceService;
            _logger = logger;
        }

        /// <summary>
        /// Joins a user to a workspace group for real-time messaging
        /// </summary>
        /// <param name="workspaceId">ID of the workspace to join</param>
        public async Task JoinWorkspaceAsync(string workspaceId)
        {
            if (!Guid.TryParse(workspaceId, out var workspaceGuid))
            {
                _logger.LogWarning("Invalid workspace ID format: {WorkspaceId}", workspaceId);
                return;
            }

            var userId = GetCurrentUserId();
            if (userId == null)
            {
                _logger.LogWarning("User ID not found in claims");
                return;
            }

            // Verify user has access to the workspace
            if (!await _workspaceService.HasUserAccessAsync(workspaceGuid, userId.Value))
            {
                _logger.LogWarning("User {UserId} attempted to join unauthorized workspace {WorkspaceId}",
                    userId, workspaceId);
                return;
            }

            // Join the workspace group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"workspace_{workspaceId}");

            _logger.LogInformation("User {UserId} joined workspace {WorkspaceId} via connection {ConnectionId}",
                userId, workspaceId, Context.ConnectionId);

            // Notify other users in the workspace that this user is online
            await Clients.GroupExcept($"workspace_{workspaceId}", Context.ConnectionId)
                .SendAsync("UserJoinedWorkspace", new { UserId = userId, ConnectionId = Context.ConnectionId });
        }

        /// <summary>
        /// Leaves a workspace group
        /// </summary>
        /// <param name="workspaceId">ID of the workspace to leave</param>
        public async Task LeaveWorkspaceAsync(string workspaceId)
        {
            if (!Guid.TryParse(workspaceId, out var workspaceGuid))
            {
                return;
            }

            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return;
            }

            // Leave the workspace group
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"workspace_{workspaceId}");

            // Stop any typing indicators
            await _messagingService.StopTypingIndicatorAsync(workspaceGuid, userId.Value, Context.ConnectionId);

            _logger.LogInformation("User {UserId} left workspace {WorkspaceId} via connection {ConnectionId}",
                userId, workspaceId, Context.ConnectionId);

            // Notify other users in the workspace that this user went offline
            await Clients.Group($"workspace_{workspaceId}")
                .SendAsync("UserLeftWorkspace", new { UserId = userId, ConnectionId = Context.ConnectionId });
        }

        /// <summary>
        /// Handles typing indicator updates
        /// </summary>
        /// <param name="workspaceId">ID of the workspace where user is typing</param>
        public async Task StartTypingAsync(string workspaceId)
        {
            if (!Guid.TryParse(workspaceId, out var workspaceGuid))
            {
                return;
            }

            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return;
            }

            // Verify user has access to the workspace
            if (!await _workspaceService.HasUserAccessAsync(workspaceGuid, userId.Value))
            {
                return;
            }

            // Update typing indicator in the service
            await _messagingService.UpdateTypingIndicatorAsync(workspaceGuid, userId.Value, Context.ConnectionId);

            // Notify other users in the workspace that this user is typing
            await Clients.GroupExcept($"workspace_{workspaceId}", Context.ConnectionId)
                .SendAsync("UserStartedTyping", new { UserId = userId, WorkspaceId = workspaceId });
        }

        /// <summary>
        /// Handles stopping typing indicator
        /// </summary>
        /// <param name="workspaceId">ID of the workspace where user stopped typing</param>
        public async Task StopTypingAsync(string workspaceId)
        {
            if (!Guid.TryParse(workspaceId, out var workspaceGuid))
            {
                return;
            }

            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return;
            }

            // Stop typing indicator in the service
            await _messagingService.StopTypingIndicatorAsync(workspaceGuid, userId.Value, Context.ConnectionId);

            // Notify other users in the workspace that this user stopped typing
            await Clients.GroupExcept($"workspace_{workspaceId}", Context.ConnectionId)
                .SendAsync("UserStoppedTyping", new { UserId = userId, WorkspaceId = workspaceId });
        }

        /// <summary>
        /// Marks a message as read and notifies the sender
        /// </summary>
        /// <param name="messageId">ID of the message to mark as read</param>
        /// <param name="workspaceId">ID of the workspace containing the message</param>
        public async Task MarkMessageAsReadAsync(string messageId, string workspaceId)
        {
            if (!Guid.TryParse(messageId, out var messageGuid) ||
                !Guid.TryParse(workspaceId, out var workspaceGuid))
            {
                return;
            }

            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return;
            }

            // Mark message as read in the service
            var success = await _messagingService.MarkMessageAsReadAsync(messageGuid, userId.Value);

            if (success)
            {
                // Notify all users in the workspace about the read receipt
                await Clients.Group($"workspace_{workspaceId}")
                    .SendAsync("MessageMarkedAsRead", new
                    {
                        MessageId = messageId,
                        UserId = userId,
                        ReadAt = DateTime.UtcNow
                    });
            }
        }

        /// <summary>
        /// Called when a client connects to the hub
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("User {UserId} connected to MessagingHub with connection {ConnectionId}",
                userId, Context.ConnectionId);

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Called when a client disconnects from the hub
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetCurrentUserId();

            if (userId.HasValue)
            {
                // Clean up any typing indicators for this connection
                await _messagingService.CleanupInactiveTypingIndicatorsAsync();
            }

            _logger.LogInformation("User {UserId} disconnected from MessagingHub with connection {ConnectionId}. Exception: {Exception}",
                userId, Context.ConnectionId, exception?.Message);

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Gets the current user's ID from the JWT claims
        /// </summary>
        /// <returns>User ID or null if not found</returns>
        private Guid? GetCurrentUserId()
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return null;
            }

            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }
}
