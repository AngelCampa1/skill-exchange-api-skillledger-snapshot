using SkillLedger.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.DTOs
{
    /// <summary>
    /// Request model for sending a new message
    /// </summary>
    public class SendMessageRequest
    {
        [Required]
        public Guid WorkspaceId { get; set; }

        [StringLength(4000)]
        public string? MessageText { get; set; }

        [Required]
        public MessageType MessageType { get; set; } = MessageType.Text;

        public Guid? ReplyToMessageId { get; set; }

        // File attachment properties (when MessageType is File, Image, or Voice)
        public string? AttachmentUrl { get; set; }
        public string? AttachmentFileName { get; set; }
        public long? AttachmentSize { get; set; }
        public string? AttachmentMimeType { get; set; }

        /// <summary>
        /// BUG-038 FIX: Optional idempotency key to prevent duplicate messages
        /// Clients should generate a unique key (e.g., GUID) for each send attempt
        /// </summary>
        [StringLength(128)]
        public string? IdempotencyKey { get; set; }

        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }

    /// <summary>
    /// Response model for a sent message
    /// </summary>
    public class MessageDto
    {
        public Guid Id { get; set; }
        public Guid WorkspaceId { get; set; }
        public Guid SenderId { get; set; }
        public string SenderName { get; set; } = null!;
        public string SenderAvatar { get; set; } = null!;
        public string? MessageText { get; set; }
        public MessageType MessageType { get; set; }
        public MessageStatus Status { get; set; }
        public bool IsEdited { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? EditedAt { get; set; }
        public DateTime? ReadAt { get; set; }

        // Reply information
        public Guid? ReplyToMessageId { get; set; }
        public MessageDto? ReplyToMessage { get; set; }

        // Attachment information
        public string? AttachmentUrl { get; set; }
        public string? AttachmentFileName { get; set; }
        public long? AttachmentSize { get; set; }
        public string? AttachmentMimeType { get; set; }

        // Reactions
        public List<MessageReactionDto> Reactions { get; set; } = new();

        // Permission flags
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }

    /// <summary>
    /// DTO for message reactions
    /// </summary>
    public class MessageReactionDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = null!;
        public string Emoji { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Request model for editing an existing message
    /// </summary>
    public class EditMessageRequest
    {
        [Required]
        [StringLength(4000)]
        public string MessageText { get; set; } = null!;

        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }

    /// <summary>
    /// Request model for adding a reaction to a message
    /// </summary>
    public class AddReactionRequest
    {
        [Required]
        [StringLength(10)]
        public string Emoji { get; set; } = null!;

        public string? IpAddress { get; set; }
    }

    /// <summary>
    /// Request model for getting message history
    /// </summary>
    public class MessageHistoryRequest
    {
        [Required]
        public Guid WorkspaceId { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;

        public DateTime? BeforeDate { get; set; }
        public DateTime? AfterDate { get; set; }

        public string? SearchQuery { get; set; }
        public MessageType? MessageType { get; set; }
        public Guid? SenderId { get; set; }
    }

    /// <summary>
    /// Response model for message history
    /// </summary>
    public class MessageHistoryResponse
    {
        public List<MessageDto> Messages { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    /// <summary>
    /// DTO for typing indicator
    /// </summary>
    public class TypingIndicatorDto
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = null!;
        public DateTime LastTypingAt { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Request model for search messages
    /// </summary>
    public class SearchMessagesRequest
    {
        [Required]
        public Guid WorkspaceId { get; set; }

        [Required]
        [StringLength(100)]
        public string Query { get; set; } = null!;

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        public MessageType? MessageType { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    /// <summary>
    /// Response model for message search results
    /// </summary>
    public class SearchMessagesResponse
    {
        public List<MessageDto> Messages { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string Query { get; set; } = null!;
        public TimeSpan SearchDuration { get; set; }
    }

    /// <summary>
    /// Response model for message statistics
    /// </summary>
    public class MessageStatsDto
    {
        public Guid WorkspaceId { get; set; }
        public int TotalMessages { get; set; }
        public int UnreadMessages { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public Dictionary<MessageType, int> MessagesByType { get; set; } = new();
        public Dictionary<string, int> TopReactions { get; set; } = new();
    }
}