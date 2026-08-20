using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.Entities
{
    /// <summary>
    /// Represents a user currently typing in a workspace (temporary entity)
    /// </summary>
    public class TypingIndicator
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The workspace where the user is typing
        /// </summary>
        [Required]
        public Guid WorkspaceId { get; set; }
        public ProjectWorkspace Workspace { get; set; } = null!;

        /// <summary>
        /// The user who is typing
        /// </summary>
        [Required]
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        /// <summary>
        /// When the typing indicator was last updated
        /// </summary>
        public DateTime LastTypingAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Connection ID for SignalR (to track which connection is typing)
        /// </summary>
        [StringLength(100)]
        public string? ConnectionId { get; set; }

        /// <summary>
        /// Checks if the typing indicator is still active (within 5 seconds)
        /// </summary>
        /// <returns>True if the user is still considered typing</returns>
        public bool IsActive()
        {
            return LastTypingAt.AddSeconds(5) > DateTime.UtcNow;
        }

        /// <summary>
        /// Updates the typing indicator to show continued typing
        /// </summary>
        public void UpdateTyping()
        {
            LastTypingAt = DateTime.UtcNow;
        }
    }
}