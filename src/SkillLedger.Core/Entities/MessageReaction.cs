using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.Entities
{
    /// <summary>
    /// Represents a reaction (emoji) to a workspace message
    /// </summary>
    public class MessageReaction
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The message this reaction belongs to
        /// </summary>
        [Required]
        public Guid MessageId { get; set; }
        public WorkspaceMessage Message { get; set; } = null!;

        /// <summary>
        /// The user who added the reaction
        /// </summary>
        [Required]
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        /// <summary>
        /// The emoji reaction (e.g., "👍", "❤️", "😄")
        /// </summary>
        [Required]
        [StringLength(10)]
        public string Emoji { get; set; } = null!;

        /// <summary>
        /// When the reaction was added
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// IP address of the user who added the reaction (for auditing)
        /// </summary>
        [StringLength(45)] // IPv6 max length
        public string? IpAddress { get; set; }
    }
}