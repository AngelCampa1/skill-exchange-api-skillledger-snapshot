using SkillLedger.Core.Models;

namespace SkillLedger.Core.Interfaces
{
    /// <summary>
    /// Service for managing document sharing via external links
    /// </summary>
    public interface IDocumentSharingService
    {
        /// <summary>
        /// Creates a shareable link for a document
        /// </summary>
        Task<ShareLinkResult> CreateShareLinkAsync(Guid documentId, ShareLinkRequest request);

        /// <summary>
        /// Gets a share link by its token
        /// </summary>
        Task<ShareLinkInfo?> GetShareLinkAsync(string shareToken);

        /// <summary>
        /// Revokes a share link
        /// </summary>
        Task<bool> RevokeShareLinkAsync(string shareToken, Guid userId);

        /// <summary>
        /// Lists all share links for a document
        /// </summary>
        Task<IEnumerable<ShareLinkInfo>> GetDocumentShareLinksAsync(Guid documentId);

        /// <summary>
        /// Validates if a share link is still active and accessible
        /// </summary>
        Task<ShareLinkValidationResult> ValidateShareLinkAsync(string shareToken, string? ipAddress = null);

        /// <summary>
        /// Logs access to a shared document
        /// </summary>
        Task LogShareLinkAccessAsync(string shareToken, string ipAddress, string? userAgent = null);

        /// <summary>
        /// Updates share link settings
        /// BUG FIX DS-003: Added requestingUserId parameter for authorization
        /// </summary>
        Task<bool> UpdateShareLinkAsync(string shareToken, ShareLinkUpdateRequest request, Guid? requestingUserId = null);

        /// <summary>
        /// Increments the download count for a share link
        /// BUG FIX DS-006: Added method to track downloads
        /// </summary>
        Task<bool> IncrementDownloadCountAsync(string shareToken);

        /// <summary>
        /// Validates a password for a password-protected share link
        /// BUG FIX DS-009: Added method for password validation
        /// </summary>
        Task<ShareLinkPasswordValidationResult> ValidateShareLinkPasswordAsync(string shareToken, string password);
    }

    /// <summary>
    /// Result of validating a share link password
    /// BUG FIX DS-009: Added for password validation
    /// </summary>
    public class ShareLinkPasswordValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public ShareLinkInfo? ShareInfo { get; set; }
    }

    /// <summary>
    /// Request to create a share link
    /// </summary>
    public class ShareLinkRequest
    {
        public SharePermissionLevel Permission { get; set; } = SharePermissionLevel.View;
        public DateTime? ExpiresAt { get; set; }
        public bool RequirePassword { get; set; } = false;
        public string? Password { get; set; }
        public int? MaxDownloads { get; set; }
        public bool AllowPublicAccess { get; set; } = true;
        public string? Description { get; set; }
    }

    /// <summary>
    /// Result of creating a share link
    /// </summary>
    public class ShareLinkResult
    {
        public string ShareToken { get; set; } = string.Empty;
        public string ShareUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public SharePermissionLevel Permission { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Information about a share link
    /// </summary>
    public class ShareLinkInfo
    {
        public string ShareToken { get; set; } = string.Empty;
        public Guid DocumentId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public Guid CreatedBy { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public SharePermissionLevel Permission { get; set; }
        public bool RequirePassword { get; set; }
        /// <summary>
        /// BUG FIX DS-009: Password hash for password-protected links (never expose externally)
        /// </summary>
        public string? PasswordHash { get; set; }
        public int? MaxDownloads { get; set; }
        public int CurrentDownloads { get; set; }
        public DateTime? LastAccessedAt { get; set; }
        public int TotalAccesses { get; set; }
        public bool IsActive { get; set; }
        public string? Description { get; set; }
    }

    /// <summary>
    /// Result of validating a share link
    /// </summary>
    public class ShareLinkValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public ShareLinkInfo? ShareInfo { get; set; }
        public bool RequiresPassword { get; set; }
        public bool HasExpired { get; set; }
        public bool MaxDownloadsReached { get; set; }
    }

    /// <summary>
    /// Request to update share link settings
    /// </summary>
    public class ShareLinkUpdateRequest
    {
        public DateTime? ExpiresAt { get; set; }
        public SharePermissionLevel? Permission { get; set; }
        public int? MaxDownloads { get; set; }
        public string? Description { get; set; }
        public bool? IsActive { get; set; }
    }

    /// <summary>
    /// Permission levels for shared documents
    /// </summary>
    public enum SharePermissionLevel
    {
        View = 1,
        Download = 2,
        Comment = 3
    }
}