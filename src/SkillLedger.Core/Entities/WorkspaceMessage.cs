using SkillLedger.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillLedger.Core.Entities
{
    /// <summary>
    /// Represents a message sent within a project workspace
    /// </summary>
    public class WorkspaceMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The workspace this message belongs to
        /// </summary>
        [Required]
        public Guid WorkspaceId { get; set; }
        public ProjectWorkspace Workspace { get; set; } = null!;

        /// <summary>
        /// The user who sent the message
        /// </summary>
        [Required]
        public Guid SenderId { get; set; }
        public User Sender { get; set; } = null!;

        /// <summary>
        /// The text content of the message (encrypted)
        /// </summary>
        [MaxLength(4000)]
        public string? MessageText { get; set; }

        /// <summary>
        /// Type of message (text, file, system, etc.)
        /// </summary>
        [Required]
        public MessageType MessageType { get; set; } = MessageType.Text;

        /// <summary>
        /// Current status of the message (sent, delivered, read)
        /// </summary>
        [Required]
        public MessageStatus Status { get; set; } = MessageStatus.Sent;

        /// <summary>
        /// URL or path to file attachment (if applicable)
        /// </summary>
        [StringLength(500)]
        public string? AttachmentUrl { get; set; }

        /// <summary>
        /// Original filename for file attachments
        /// </summary>
        [StringLength(255)]
        public string? AttachmentFileName { get; set; }

        /// <summary>
        /// Size of attachment in bytes
        /// </summary>
        public long? AttachmentSize { get; set; }

        /// <summary>
        /// MIME type of attachment
        /// </summary>
        [StringLength(100)]
        public string? AttachmentMimeType { get; set; }

        /// <summary>
        /// Whether this message has been edited after sending
        /// </summary>
        public bool IsEdited { get; set; } = false;

        /// <summary>
        /// Message this is a reply to (for threading)
        /// </summary>
        public Guid? ReplyToMessageId { get; set; }
        public WorkspaceMessage? ReplyToMessage { get; set; }

        /// <summary>
        /// Messages that reply to this one
        /// </summary>
        public ICollection<WorkspaceMessage> Replies { get; set; } = new List<WorkspaceMessage>();

        /// <summary>
        /// When the message was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the message was last edited (if applicable)
        /// </summary>
        public DateTime? EditedAt { get; set; }

        /// <summary>
        /// When the message was read by the recipient
        /// </summary>
        public DateTime? ReadAt { get; set; }

        /// <summary>
        /// BUG-038 FIX: Idempotency key to prevent duplicate message processing
        /// Clients should generate a unique key (e.g., GUID) for each send attempt
        /// </summary>
        [StringLength(128)]
        public string? IdempotencyKey { get; set; }

        /// <summary>
        /// IP address of the sender (for security auditing)
        /// </summary>
        [StringLength(45)] // IPv6 max length
        public string? SenderIpAddress { get; set; }

        /// <summary>
        /// User agent of the sender
        /// </summary>
        [StringLength(500)]
        public string? SenderUserAgent { get; set; }

        /// <summary>
        /// Collection of message reactions
        /// </summary>
        public ICollection<MessageReaction> Reactions { get; set; } = new List<MessageReaction>();

        /// <summary>
        /// Marks the message as edited
        /// </summary>
        public void MarkAsEdited()
        {
            IsEdited = true;
            EditedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Marks the message as read
        /// </summary>
        public void MarkAsRead()
        {
            if (Status != MessageStatus.Read)
            {
                Status = MessageStatus.Read;
                ReadAt = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Marks the message as delivered
        /// </summary>
        public void MarkAsDelivered()
        {
            if (Status == MessageStatus.Sent)
            {
                Status = MessageStatus.Delivered;
            }
        }

        /// <summary>
        /// Checks if the message can be edited by the given user
        /// </summary>
        /// <param name="userId">The user ID to check</param>
        /// <returns>True if the user can edit this message</returns>
        public bool CanBeEditedBy(Guid userId)
        {
            // Only the sender can edit their own messages
            // And only text messages can be edited
            // And only within 24 hours of sending
            return SenderId == userId
                && MessageType == MessageType.Text
                && CreatedAt.AddHours(24) > DateTime.UtcNow;
        }

        /// <summary>
        /// Checks if the message can be deleted by the given user
        /// </summary>
        /// <param name="userId">The user ID to check</param>
        /// <returns>True if the user can delete this message</returns>
        public bool CanBeDeletedBy(Guid userId)
        {
            // Only the sender can delete their own messages
            // And only within 24 hours of sending
            return SenderId == userId && CreatedAt.AddHours(24) > DateTime.UtcNow;
        }
    }
}