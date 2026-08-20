using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.RateLimiting;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Interfaces;
using SkillLedger.Api.Hubs;
using SkillLedger.Infrastructure.Extensions;
using SkillLedger.Infrastructure.Services;
using System.Security.Claims;

namespace SkillLedger.Api.Controllers
{
    /// <summary>
    /// API Controller for real-time messaging functionality
    /// Provides REST endpoints for message management with SignalR integration
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MessagingController : BaseApiController
    {
        private readonly IMessagingService _messagingService;
        private readonly IHubContext<MessagingHub> _hubContext;
        private readonly ILogger<MessagingController> _logger;
        private readonly IAuditLogService _auditLogService;
        private readonly ControllerHelperService _helperService;

        public MessagingController(
            IMessagingService messagingService,
            IHubContext<MessagingHub> hubContext,
            ILogger<MessagingController> logger,
            IAuditLogService auditLogService,
            ControllerHelperService helperService)
        {
            _messagingService = messagingService;
            _hubContext = hubContext;
            _logger = logger;
            _auditLogService = auditLogService;
            _helperService = helperService;
        }

        /// <summary>
        /// Sends a new message in a workspace
        /// </summary>
        /// <param name="request">Message details</param>
        /// <returns>The sent message DTO</returns>
        [HttpPost("send")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("MessagingPolicy")]
        public async Task<ActionResult<MessageDto>> SendMessageAsync([FromBody] SendMessageRequest request)
        {
            try
            {
                var userId = _helperService.GetCurrentUserId(User);
                var ipAddress = _helperService.GetClientIpAddress(HttpContext);
                request.IpAddress = ipAddress;
                request.UserAgent = Request.Headers.UserAgent.ToString();

                var messageDto = await _messagingService.SendMessageAsync(request, userId);

                // Audit logging
                _auditLogService.LogAuditEventAsync(
                    _logger,
                    userId,
                    AuditActions.MESSAGE_SENT,
                    ipAddress,
                    request.UserAgent,
                    true,
                    $"{{\"MessageId\":\"{messageDto.Id}\",\"WorkspaceId\":\"{request.WorkspaceId}\"}}"
                );

                // Broadcast the new message to all users in the workspace via SignalR
                try
                {
                    await _hubContext.Clients.Group($"workspace_{request.WorkspaceId}")
                        .SendAsync("NewMessage", messageDto);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to broadcast NewMessage for workspace {WorkspaceId}", request.WorkspaceId);
                    // Continue - SignalR failure should not block HTTP response
                }

                _logger.LogInformation("Message {MessageId} sent by user {UserId} in workspace {WorkspaceId}",
                    messageDto.Id, userId, request.WorkspaceId);

                return Ok(messageDto);
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, "You don't have permission to send messages in this workspace");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Edits an existing message
        /// </summary>
        /// <param name="messageId">ID of the message to edit</param>
        /// <param name="request">Edit request details</param>
        /// <returns>The updated message DTO</returns>
        [HttpPut("{messageId:guid}/edit")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("MessagingPolicy")]
        public async Task<ActionResult<MessageDto>> EditMessageAsync(
            [FromRoute] Guid messageId,
            [FromBody] EditMessageRequest request)
        {
            try
            {
                var userId = _helperService.GetCurrentUserId(User);
                var ipAddress = _helperService.GetClientIpAddress(HttpContext);
                request.IpAddress = ipAddress;
                request.UserAgent = Request.Headers.UserAgent.ToString();

                var messageDto = await _messagingService.EditMessageAsync(messageId, request, userId);

                // Audit logging
                _auditLogService.LogAuditEventAsync(
                    _logger,
                    userId,
                    AuditActions.MESSAGE_EDITED,
                    ipAddress,
                    request.UserAgent,
                    true,
                    $"{{\"MessageId\":\"{messageId}\",\"WorkspaceId\":\"{messageDto.WorkspaceId}\"}}"
                );

                // Broadcast the message update to all users in the workspace via SignalR
                try
                {
                    await _hubContext.Clients.Group($"workspace_{messageDto.WorkspaceId}")
                        .SendAsync("MessageUpdated", messageDto);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to broadcast MessageUpdated for workspace {WorkspaceId}", messageDto.WorkspaceId);
                    // Continue - SignalR failure should not block HTTP response
                }

                _logger.LogInformation("Message {MessageId} edited by user {UserId}", messageId, userId);

                return Ok(messageDto);
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, "You don't have permission to edit this message");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Deletes a message
        /// </summary>
        /// <param name="messageId">ID of the message to delete</param>
        /// <returns>Success status</returns>
        [HttpDelete("{messageId:guid}")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("MessagingPolicy")]
        public async Task<ActionResult> DeleteMessageAsync([FromRoute] Guid messageId)
        {
            try
            {
                var userId = _helperService.GetCurrentUserId(User);
                var ipAddress = _helperService.GetClientIpAddress(HttpContext);

                // Get message details before deletion for broadcasting
                var message = await _messagingService.GetMessageAsync(messageId, userId);
                if (message == null)
                {
                    return NotFound("Message not found");
                }

                var success = await _messagingService.DeleteMessageAsync(messageId, userId);
                if (!success)
                {
                    return StatusCode(403, "You don't have permission to delete this message");
                }

                // Audit logging
                _auditLogService.LogAuditEventAsync(
                    _logger,
                    userId,
                    AuditActions.MESSAGE_DELETED,
                    ipAddress,
                    Request.Headers.UserAgent.ToString(),
                    true,
                    $"{{\"MessageId\":\"{messageId}\",\"WorkspaceId\":\"{message.WorkspaceId}\"}}"
                );

                // Broadcast the message deletion to all users in the workspace via SignalR
                try
                {
                    await _hubContext.Clients.Group($"workspace_{message.WorkspaceId}")
                        .SendAsync("MessageDeleted", new { MessageId = messageId });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to broadcast MessageDeleted for workspace {WorkspaceId}", message.WorkspaceId);
                    // Continue - SignalR failure should not block HTTP response
                }

                _logger.LogInformation("Message {MessageId} deleted by user {UserId}", messageId, userId);

                return Ok(new { Success = true, Message = "Message deleted successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Marks a message as read
        /// </summary>
        /// <param name="messageId">ID of the message to mark as read</param>
        /// <returns>Success status</returns>
        [HttpPost("{messageId:guid}/read")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("MessagingPolicy")]
        public async Task<ActionResult> MarkMessageAsReadAsync([FromRoute] Guid messageId)
        {
            try
            {
                var userId = _helperService.GetCurrentUserId(User);
                var ipAddress = _helperService.GetClientIpAddress(HttpContext);
                var success = await _messagingService.MarkMessageAsReadAsync(messageId, userId);

                if (success)
                {
                    // Get the message to find the workspace ID for broadcasting
                    var message = await _messagingService.GetMessageAsync(messageId, userId);
                    if (message != null)
                    {
                        // Audit logging
                        _auditLogService.LogAuditEventAsync(
                            _logger,
                            userId,
                            AuditActions.MESSAGE_READ,
                            ipAddress,
                            Request.Headers.UserAgent.ToString(),
                            true,
                            $"{{\"MessageId\":\"{messageId}\",\"WorkspaceId\":\"{message.WorkspaceId}\"}}"
                        );

                        // Broadcast the read receipt to all users in the workspace via SignalR
                        try
                        {
                            await _hubContext.Clients.Group($"workspace_{message.WorkspaceId}")
                                .SendAsync("MessageMarkedAsRead", new
                                {
                                    MessageId = messageId,
                                    UserId = userId,
                                    ReadAt = DateTime.UtcNow
                                });
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to broadcast MessageMarkedAsRead for workspace {WorkspaceId}", message.WorkspaceId);
                            // Continue - SignalR failure should not block HTTP response
                        }
                    }

                    return Ok(new { Success = true });
                }

                return NotFound("Message not found or already read");
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, "You don't have permission to access this message");
            }
        }

        /// <summary>
        /// Marks all messages in a workspace as read
        /// </summary>
        /// <param name="workspaceId">ID of the workspace</param>
        /// <returns>Number of messages marked as read</returns>
        [HttpPost("workspace/{workspaceId:guid}/read-all")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("MessagingPolicy")]
        public async Task<ActionResult<int>> MarkAllMessagesAsReadAsync([FromRoute] Guid workspaceId)
        {
            try
            {
                var userId = _helperService.GetCurrentUserId(User);
                var ipAddress = _helperService.GetClientIpAddress(HttpContext);
                var count = await _messagingService.MarkAllMessagesAsReadAsync(workspaceId, userId);

                // Audit logging
                _auditLogService.LogAuditEventAsync(
                    _logger,
                    userId,
                    AuditActions.MESSAGES_READ_ALL,
                    ipAddress,
                    Request.Headers.UserAgent.ToString(),
                    true,
                    $"{{\"WorkspaceId\":\"{workspaceId}\",\"Count\":{count}}}"
                );

                // Broadcast the bulk read receipt to all users in the workspace via SignalR
                try
                {
                    await _hubContext.Clients.Group($"workspace_{workspaceId}")
                        .SendAsync("AllMessagesMarkedAsRead", new
                        {
                            WorkspaceId = workspaceId,
                            UserId = userId,
                            ReadAt = DateTime.UtcNow,
                            Count = count
                        });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to broadcast AllMessagesMarkedAsRead for workspace {WorkspaceId}", workspaceId);
                    // Continue - SignalR failure should not block HTTP response
                }

                return Ok(count);
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, "You don't have permission to access this workspace");
            }
        }

        /// <summary>
        /// Gets message history for a workspace with pagination
        /// </summary>
        /// <param name="workspaceId">ID of the workspace</param>
        /// <param name="pageNumber">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 50)</param>
        /// <param name="beforeDate">Get messages before this date</param>
        /// <param name="afterDate">Get messages after this date</param>
        /// <param name="searchQuery">Search query for message content</param>
        /// <param name="messageType">Filter by message type</param>
        /// <param name="senderId">Filter by sender ID</param>
        /// <returns>Paginated message history</returns>
        [HttpGet("workspace/{workspaceId:guid}/history")]
        public async Task<ActionResult<MessageHistoryResponse>> GetMessageHistoryAsync(
            [FromRoute] Guid workspaceId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] DateTime? beforeDate = null,
            [FromQuery] DateTime? afterDate = null,
            [FromQuery] string? searchQuery = null,
            [FromQuery] Core.Enums.MessageType? messageType = null,
            [FromQuery] Guid? senderId = null)
        {
            try
            {
                var request = new MessageHistoryRequest
                {
                    WorkspaceId = workspaceId,
                    PageNumber = Math.Max(1, pageNumber),
                    PageSize = Math.Min(100, Math.Max(1, pageSize)), // Limit page size to 100
                    BeforeDate = beforeDate,
                    AfterDate = afterDate,
                    SearchQuery = searchQuery,
                    MessageType = messageType,
                    SenderId = senderId
                };

                var userId = _helperService.GetCurrentUserId(User);
                var response = await _messagingService.GetMessageHistoryAsync(request, userId);

                // Add pagination headers
                Response.Headers.Append("X-Total-Count", response.TotalCount.ToString());
                Response.Headers.Append("X-Page-Size", request.PageSize.ToString());
                Response.Headers.Append("X-Page-Number", request.PageNumber.ToString());
                Response.Headers.Append("X-Total-Pages",
                    Math.Ceiling((double)response.TotalCount / request.PageSize).ToString());

                return Ok(response);
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, "You don't have permission to access this workspace");
            }
        }

        /// <summary>
        /// Searches messages in a workspace
        /// </summary>
        /// <param name="workspaceId">ID of the workspace</param>
        /// <param name="query">Search query</param>
        /// <param name="pageNumber">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 20)</param>
        /// <param name="messageType">Filter by message type</param>
        /// <param name="fromDate">Search messages from this date</param>
        /// <param name="toDate">Search messages to this date</param>
        /// <returns>Search results</returns>
        [HttpGet("workspace/{workspaceId:guid}/search")]
        [EnableRateLimiting("MessageSearchPolicy")]  // BUG-BE-005 FIX: Add rate limiting to prevent scraping and DoS
        public async Task<ActionResult<SearchMessagesResponse>> SearchMessagesAsync(
            [FromRoute] Guid workspaceId,
            [FromQuery] string query,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] Core.Enums.MessageType? messageType = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("Search query is required");
            }

            try
            {
                var request = new SearchMessagesRequest
                {
                    WorkspaceId = workspaceId,
                    Query = query.Trim(),
                    PageNumber = Math.Max(1, pageNumber),
                    PageSize = Math.Min(50, Math.Max(1, pageSize)), // Limit page size to 50 for search
                    MessageType = messageType,
                    FromDate = fromDate,
                    ToDate = toDate
                };

                var userId = _helperService.GetCurrentUserId(User);
                var response = await _messagingService.SearchMessagesAsync(request, userId);

                // Add pagination headers
                Response.Headers.Append("X-Total-Count", response.TotalCount.ToString());
                Response.Headers.Append("X-Page-Size", request.PageSize.ToString());
                Response.Headers.Append("X-Page-Number", request.PageNumber.ToString());
                Response.Headers.Append("X-Total-Pages",
                    Math.Ceiling((double)response.TotalCount / request.PageSize).ToString());

                return Ok(response);
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, "You don't have permission to search in this workspace");
            }
        }

        /// <summary>
        /// Gets a specific message by ID
        /// </summary>
        /// <param name="messageId">ID of the message</param>
        /// <returns>Message DTO</returns>
        [HttpGet("{messageId:guid}")]
        public async Task<ActionResult<MessageDto>> GetMessageAsync([FromRoute] Guid messageId)
        {
            try
            {
                var userId = _helperService.GetCurrentUserId(User);
                var message = await _messagingService.GetMessageAsync(messageId, userId);

                if (message == null)
                {
                    return NotFound("Message not found");
                }

                return Ok(message);
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, "You don't have permission to access this message");
            }
        }

        /// <summary>
        /// Adds a reaction to a message
        /// </summary>
        /// <param name="messageId">ID of the message to react to</param>
        /// <param name="request">Reaction request details</param>
        /// <returns>Success status</returns>
        [HttpPost("{messageId:guid}/reactions")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("MessagingPolicy")]
        public async Task<ActionResult> AddReactionAsync(
            [FromRoute] Guid messageId,
            [FromBody] AddReactionRequest request)
        {
            try
            {
                var userId = _helperService.GetCurrentUserId(User);
                var ipAddress = _helperService.GetClientIpAddress(HttpContext);
                request.IpAddress = ipAddress;

                var success = await _messagingService.AddReactionAsync(messageId, request, userId);

                if (success)
                {
                    // Get the message to find the workspace ID for broadcasting
                    var message = await _messagingService.GetMessageAsync(messageId, userId);
                    if (message != null)
                    {
                        // Audit logging
                        _auditLogService.LogAuditEventAsync(
                            _logger,
                            userId,
                            AuditActions.REACTION_ADDED,
                            ipAddress,
                            Request.Headers.UserAgent.ToString(),
                            true,
                            $"{{\"MessageId\":\"{messageId}\",\"Emoji\":\"{request.Emoji}\",\"WorkspaceId\":\"{message.WorkspaceId}\"}}"
                        );

                        // Broadcast the reaction to all users in the workspace via SignalR
                        try
                        {
                            await _hubContext.Clients.Group($"workspace_{message.WorkspaceId}")
                                .SendAsync("ReactionAdded", new
                                {
                                    MessageId = messageId,
                                    Reaction = new
                                    {
                                        UserId = userId,
                                        Emoji = request.Emoji,
                                        CreatedAt = DateTime.UtcNow
                                    }
                                });
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to broadcast ReactionAdded for workspace {WorkspaceId}", message.WorkspaceId);
                            // Continue - SignalR failure should not block HTTP response
                        }
                    }

                    return Ok(new { Success = true });
                }

                return BadRequest("Failed to add reaction");
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, "You don't have permission to react to this message");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Removes a reaction from a message
        /// </summary>
        /// <param name="messageId">ID of the message</param>
        /// <param name="emoji">Emoji to remove</param>
        /// <returns>Success status</returns>
        [HttpDelete("{messageId:guid}/reactions/{emoji}")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("MessagingPolicy")]
        public async Task<ActionResult> RemoveReactionAsync(
            [FromRoute] Guid messageId,
            [FromRoute] string emoji)
        {
            try
            {
                var userId = _helperService.GetCurrentUserId(User);
                var ipAddress = _helperService.GetClientIpAddress(HttpContext);
                var success = await _messagingService.RemoveReactionAsync(messageId, emoji, userId);

                if (success)
                {
                    // Get the message to find the workspace ID for broadcasting
                    var message = await _messagingService.GetMessageAsync(messageId, userId);
                    if (message != null)
                    {
                        // Audit logging
                        _auditLogService.LogAuditEventAsync(
                            _logger,
                            userId,
                            AuditActions.REACTION_REMOVED,
                            ipAddress,
                            Request.Headers.UserAgent.ToString(),
                            true,
                            $"{{\"MessageId\":\"{messageId}\",\"Emoji\":\"{emoji}\",\"WorkspaceId\":\"{message.WorkspaceId}\"}}"
                        );

                        // Broadcast the reaction removal to all users in the workspace via SignalR
                        try
                        {
                            await _hubContext.Clients.Group($"workspace_{message.WorkspaceId}")
                                .SendAsync("ReactionRemoved", new
                                {
                                    MessageId = messageId,
                                    UserId = userId,
                                    Emoji = emoji
                                });
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to broadcast ReactionRemoved for workspace {WorkspaceId}", message.WorkspaceId);
                            // Continue - SignalR failure should not block HTTP response
                        }
                    }

                    return Ok(new { Success = true });
                }

                return NotFound("Reaction not found");
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, "You don't have permission to access this message");
            }
        }

        /// <summary>
        /// Gets typing indicators for a workspace
        /// </summary>
        /// <param name="workspaceId">ID of the workspace</param>
        /// <returns>List of active typing indicators</returns>
        [HttpGet("workspace/{workspaceId:guid}/typing")]
        public async Task<ActionResult<List<TypingIndicatorDto>>> GetTypingIndicatorsAsync([FromRoute] Guid workspaceId)
        {
            try
            {
                var userId = _helperService.GetCurrentUserId(User);
                var indicators = await _messagingService.GetTypingIndicatorsAsync(workspaceId, userId);

                return Ok(indicators);
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, "You don't have permission to access this workspace");
            }
        }

        /// <summary>
        /// Gets message statistics for a workspace
        /// </summary>
        /// <param name="workspaceId">ID of the workspace</param>
        /// <returns>Message statistics</returns>
        [HttpGet("workspace/{workspaceId:guid}/stats")]
        public async Task<ActionResult<MessageStatsDto>> GetMessageStatsAsync([FromRoute] Guid workspaceId)
        {
            try
            {
                var userId = _helperService.GetCurrentUserId(User);
                var stats = await _messagingService.GetMessageStatsAsync(workspaceId, userId);

                return Ok(stats);
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, "You don't have permission to access this workspace");
            }
        }

        /// <summary>
        /// Gets unread message count for the current user across all workspaces
        /// </summary>
        /// <returns>Total unread message count</returns>
        [HttpGet("unread-count")]
        public async Task<ActionResult<int>> GetUnreadMessageCountAsync()
        {
            var userId = _helperService.GetCurrentUserId(User);
            var count = await _messagingService.GetUnreadMessageCountAsync(userId);

            return Ok(count);
        }

        /// <summary>
        /// Gets unread message count for a specific workspace
        /// </summary>
        /// <param name="workspaceId">ID of the workspace</param>
        /// <returns>Unread message count for the workspace</returns>
        [HttpGet("workspace/{workspaceId:guid}/unread-count")]
        public async Task<ActionResult<int>> GetWorkspaceUnreadCountAsync([FromRoute] Guid workspaceId)
        {
            try
            {
                var userId = _helperService.GetCurrentUserId(User);
                var count = await _messagingService.GetWorkspaceUnreadCountAsync(workspaceId, userId);

                return Ok(count);
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, "You don't have permission to access this workspace");
            }
        }

    }
}
