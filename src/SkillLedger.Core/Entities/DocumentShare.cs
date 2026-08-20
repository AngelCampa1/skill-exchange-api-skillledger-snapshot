using SkillLedger.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.Entities
{
    /// <summary>
    /// Represents a document sharing permission for a user
    /// Extends access control beyond workspace membership
    /// </summary>
    public class DocumentShare
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The document being shared
        /// </summary>
        [Required]
        public Guid DocumentId { get; set; }
        public WorkspaceDocument Document { get; set; } = null!;

        /// <summary>
        /// User who is being granted access
        /// </summary>
        [Required]
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        /// <summary>
        /// User who shared the document
        /// </summary>
        [Required]
        public Guid SharedBy { get; set; }
        public User Sharer { get; set; } = null!;

        /// <summary>
        /// Level of access granted (view, edit, admin)
        /// </summary>
        public SharePermission Permission { get; set; } = SharePermission.View;

        /// <summary>
        /// When the share was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Optional expiration date for the share
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Whether the share is currently active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// When the share was revoked (if applicable)
        /// </summary>
        public DateTime? RevokedAt { get; set; }

        /// <summary>
        /// User who revoked the share
        /// </summary>
        public Guid? RevokedBy { get; set; }
        public User? Revoker { get; set; }

        /// <summary>
        /// Optional message or note about the share
        /// </summary>
        [StringLength(1000)]
        public string? ShareMessage { get; set; }

        /// <summary>
        /// Access token for secure sharing (optional)
        /// </summary>
        [StringLength(256)]
        public string? AccessToken { get; set; }

        /// <summary>
        /// Checks if the share is currently valid and active
        /// </summary>
        /// <returns>True if the share is active and not expired</returns>
        public bool IsActiveAndValid()
        {
            if (!IsActive || RevokedAt.HasValue)
                return false;

            if (ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow)
                return false;

            return true;
        }

        /// <summary>
        /// Revokes the document share
        /// </summary>
        /// <param name="revokedBy">User revoking the share</param>
        public void Revoke(Guid revokedBy)
        {
            IsActive = false;
            RevokedAt = DateTime.UtcNow;
            RevokedBy = revokedBy;
        }

        /// <summary>
        /// Extends the expiration date of the share
        /// </summary>
        /// <param name="newExpirationDate">New expiration date</param>
        public void ExtendExpiration(DateTime newExpirationDate)
        {
            if (newExpirationDate > DateTime.UtcNow)
            {
                ExpiresAt = newExpirationDate;
            }
        }

        /// <summary>
        /// Checks if the user has the specified permission level
        /// </summary>
        /// <param name="requiredPermission">Required permission level</param>
        /// <returns>True if user has sufficient permission</returns>
        public bool HasPermission(SharePermission requiredPermission)
        {
            if (!IsActiveAndValid())
                return false;

            return Permission >= requiredPermission;
        }
    }
}