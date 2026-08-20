using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.Entities
{
    /// <summary>
    /// Represents a record of user accessing a workspace document
    /// Used for audit logging and analytics
    /// </summary>
    public class DocumentAccess
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The document that was accessed
        /// </summary>
        [Required]
        public Guid DocumentId { get; set; }
        public WorkspaceDocument Document { get; set; } = null!;

        /// <summary>
        /// User who accessed the document
        /// </summary>
        [Required]
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        /// <summary>
        /// When the document was accessed
        /// </summary>
        public DateTime AccessedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Type of access (view, download, preview)
        /// </summary>
        [StringLength(50)]
        public string AccessType { get; set; } = "view";

        /// <summary>
        /// IP address of the user during access
        /// </summary>
        [StringLength(45)] // IPv6 max length
        public string? IpAddress { get; set; }

        /// <summary>
        /// User agent during access
        /// </summary>
        [StringLength(500)]
        public string? UserAgent { get; set; }

        /// <summary>
        /// Additional metadata about the access (JSON)
        /// </summary>
        public string? Metadata { get; set; }
    }
}