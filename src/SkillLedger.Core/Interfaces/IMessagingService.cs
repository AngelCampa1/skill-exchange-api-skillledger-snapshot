using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;

namespace SkillLedger.Core.Interfaces
{
    /// <summary>
    /// Service for managing workspace messaging functionality
    /// </summary>
    public interface IMessagingService
    {
        // Message operations
        /// <summary>
        /// Sends a new message in a workspace
        /// </summary>
        /// <param name="request">Message details</param>
        /// <param name="senderId">ID of the user sending the message</param>
        /// <returns>The sent message DTO</returns>
        Task<MessageDto> SendMessageAsync(SendMessageRequest request, Guid senderId);

        /// <summary>
        /// Edits an existing message (only by the sender, within time limit)
        /// </summary>
        /// <param name="messageId">ID of the message to edit</param>
        /// <param name="request">Edit request details</param>
        /// <param name="userId">ID of the user attempting to edit</param>
        /// <returns>The updated message DTO</returns>
        Task<MessageDto> EditMessageAsync(Guid messageId, EditMessageRequest request, Guid userId);

        /// <summary>
        /// Deletes a message (only by the sender, within time limit)
        /// </summary>
        /// <param name="messageId">ID of the message to delete</param>
        /// <param name="userId">ID of the user attempting to delete</param>
        /// <returns>True if deletion was successful</returns>
        Task<bool> DeleteMessageAsync(Guid messageId, Guid userId);

        /// <summary>
        /// Marks a message as read by the current user
        /// </summary>
        /// <param name="messageId">ID of the message to mark as read</param>
        /// <param name="userId">ID of the user marking as read</param>
        /// <returns>True if marking was successful</returns>
        Task<bool> MarkMessageAsReadAsync(Guid messageId, Guid userId);

        /// <summary>
        /// Marks all messages in a workspace as read by the current user
        /// </summary>
        /// <param name="workspaceId">ID of the workspace</param>
        /// <param name="userId">ID of the user marking messages as read</param>
        /// <returns>Number of messages marked as read</returns>
        Task<int> MarkAllMessagesAsReadAsync(Guid workspaceId, Guid userId);

        // Message history and search
        /// <summary>
        /// Gets message history for a workspace with pagination
        /// </summary>
        /// <param name="request">History request parameters</param>
        /// <param name="userId">ID of the user requesting history</param>
        /// <returns>Paginated message history</returns>
        Task<MessageHistoryResponse> GetMessageHistoryAsync(MessageHistoryRequest request, Guid userId);

        /// <summary>
        /// Searches messages in a workspace
        /// </summary>
        /// <param name="request">Search request parameters</param>
        /// <param name="userId">ID of the user performing search</param>
        /// <returns>Search results with messages</returns>
        Task<SearchMessagesResponse> SearchMessagesAsync(SearchMessagesRequest request, Guid userId);

        /// <summary>
        /// Gets a specific message by ID
        /// </summary>
        /// <param name="messageId">ID of the message</param>
        /// <param name="userId">ID of the user requesting the message</param>
        /// <returns>Message DTO or null if not found/unauthorized</returns>
        Task<MessageDto?> GetMessageAsync(Guid messageId, Guid userId);

        // Reactions
        /// <summary>
        /// Adds a reaction to a message
        /// </summary>
        /// <param name="messageId">ID of the message to react to</param>
        /// <param name="request">Reaction request details</param>
        /// <param name="userId">ID of the user adding the reaction</param>
        /// <returns>True if reaction was added successfully</returns>
        Task<bool> AddReactionAsync(Guid messageId, AddReactionRequest request, Guid userId);

        /// <summary>
        /// Removes a reaction from a message
        /// </summary>
        /// <param name="messageId">ID of the message</param>
        /// <param name="emoji">Emoji to remove</param>
        /// <param name="userId">ID of the user removing the reaction</param>
        /// <returns>True if reaction was removed successfully</returns>
        Task<bool> RemoveReactionAsync(Guid messageId, string emoji, Guid userId);

        // Typing indicators
        /// <summary>
        /// Updates typing indicator for a user in a workspace
        /// </summary>
        /// <param name="workspaceId">ID of the workspace</param>
        /// <param name="userId">ID of the typing user</param>
        /// <param name="connectionId">SignalR connection ID</param>
        /// <returns>True if typing indicator was updated</returns>
        Task<bool> UpdateTypingIndicatorAsync(Guid workspaceId, Guid userId, string? connectionId = null);

        /// <summary>
        /// Stops typing indicator for a user in a workspace
        /// </summary>
        /// <param name="workspaceId">ID of the workspace</param>
        /// <param name="userId">ID of the user</param>
        /// <param name="connectionId">SignalR connection ID</param>
        /// <returns>True if typing indicator was removed</returns>
        Task<bool> StopTypingIndicatorAsync(Guid workspaceId, Guid userId, string? connectionId = null);

        /// <summary>
        /// Gets current typing indicators for a workspace
        /// </summary>
        /// <param name="workspaceId">ID of the workspace</param>
        /// <param name="excludeUserId">User ID to exclude (typically the requesting user)</param>
        /// <returns>List of active typing indicators</returns>
        Task<List<TypingIndicatorDto>> GetTypingIndicatorsAsync(Guid workspaceId, Guid? excludeUserId = null);

        /// <summary>
        /// Cleans up inactive typing indicators (older than 5 seconds)
        /// </summary>
        /// <returns>Number of indicators cleaned up</returns>
        Task<int> CleanupInactiveTypingIndicatorsAsync();

        // Statistics and analytics
        /// <summary>
        /// Gets message statistics for a workspace
        /// </summary>
        /// <param name="workspaceId">ID of the workspace</param>
        /// <param name="userId">ID of the user requesting statistics</param>
        /// <returns>Message statistics</returns>
        Task<MessageStatsDto> GetMessageStatsAsync(Guid workspaceId, Guid userId);

        // Utility methods
        /// <summary>
        /// Checks if a user has access to a workspace for messaging
        /// </summary>
        /// <param name="workspaceId">ID of the workspace</param>
        /// <param name="userId">ID of the user</param>
        /// <returns>True if user has messaging access</returns>
        Task<bool> HasMessagingAccessAsync(Guid workspaceId, Guid userId);

        /// <summary>
        /// Gets unread message count for a user across all workspaces
        /// </summary>
        /// <param name="userId">ID of the user</param>
        /// <returns>Total unread message count</returns>
        Task<int> GetUnreadMessageCountAsync(Guid userId);

        /// <summary>
        /// Gets unread message count for a specific workspace
        /// </summary>
        /// <param name="workspaceId">ID of the workspace</param>
        /// <param name="userId">ID of the user</param>
        /// <returns>Unread message count for the workspace</returns>
        Task<int> GetWorkspaceUnreadCountAsync(Guid workspaceId, Guid userId);
    }
}