using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.Entities
{
    /// <summary>
    /// Represents a document or file uploaded to a project workspace
    /// </summary>
    public class WorkspaceDocument
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The workspace this document belongs to
        /// </summary>
        [Required]
        public Guid WorkspaceId { get; set; }
        public ProjectWorkspace Workspace { get; set; } = null!;

        /// <summary>
        /// Original file name as uploaded by the user
        /// </summary>
        [Required]
        [StringLength(500)]
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Storage path for the file (blob name or local path)
        /// </summary>
        [Required]
        [StringLength(1000)]
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// File size in bytes
        /// </summary>
        [Required]
        public long FileSize { get; set; }

        /// <summary>
        /// MIME type of the file
        /// </summary>
        [Required]
        [StringLength(100)]
        public string MimeType { get; set; } = string.Empty;

        /// <summary>
        /// User who uploaded the file
        /// </summary>
        [Required]
        public Guid UploadedBy { get; set; }
        public User Uploader { get; set; } = null!;

        /// <summary>
        /// Folder this document belongs to (optional)
        /// </summary>
        public Guid? FolderId { get; set; }
        public DocumentFolder? Folder { get; set; }

        /// <summary>
        /// Version number for version control
        /// </summary>
        public int VersionNumber { get; set; } = 1;

        /// <summary>
        /// Whether the file is marked as deleted
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// When the document was uploaded
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the document was last accessed
        /// </summary>
        public DateTime? LastAccessedAt { get; set; }

        /// <summary>
        /// When the document was deleted (soft delete)
        /// </summary>
        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// User who deleted the document
        /// </summary>
        public Guid? DeletedBy { get; set; }
        public User? Deleter { get; set; }

        /// <summary>
        /// Security scan results (JSON)
        /// </summary>
        public string? SecurityScanResult { get; set; }

        /// <summary>
        /// Whether the file passed security scanning
        /// </summary>
        public bool SecurityScanPassed { get; set; } = false;

        /// <summary>
        /// File download/access history
        /// </summary>
        public ICollection<DocumentAccess> AccessHistory { get; set; } = new List<DocumentAccess>();

        /// <summary>
        /// File sharing permissions
        /// </summary>
        public ICollection<DocumentShare> Shares { get; set; } = new List<DocumentShare>();

        /// <summary>
        /// Previous versions of this document (for version control)
        /// </summary>
        public ICollection<WorkspaceDocument> PreviousVersions { get; set; } = new List<WorkspaceDocument>();

        /// <summary>
        /// Parent document if this is a version
        /// </summary>
        public Guid? ParentDocumentId { get; set; }
        public WorkspaceDocument? ParentDocument { get; set; }

        /// <summary>
        /// Marks the document as accessed by a user
        /// </summary>
        /// <param name="userId">User accessing the document</param>
        public void RecordAccess(Guid userId)
        {
            LastAccessedAt = DateTime.UtcNow;
            AccessHistory.Add(new DocumentAccess
            {
                DocumentId = Id,
                UserId = userId,
                AccessedAt = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Soft deletes the document
        /// </summary>
        /// <param name="userId">User performing the deletion</param>
        public void Delete(Guid userId)
        {
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
            DeletedBy = userId;
        }

        /// <summary>
        /// Restores a soft-deleted document
        /// </summary>
        public void Restore()
        {
            IsDeleted = false;
            DeletedAt = null;
            DeletedBy = null;
        }

        /// <summary>
        /// Checks if a user can access this document
        /// </summary>
        /// <param name="userId">User ID to check</param>
        /// <returns>True if user has access</returns>
        public bool CanBeAccessedBy(Guid userId)
        {
            if (IsDeleted) return false;

            // Check if user has workspace access through the workspace entity
            if (Workspace?.IsAccessibleBy(userId) == true)
                return true;

            // Check if user has explicit document share permission
            return Shares.Any(s => s.UserId == userId && s.IsActiveAndValid());
        }

        /// <summary>
        /// Checks if a user can edit this document
        /// </summary>
        /// <param name="userId">User ID to check</param>
        /// <returns>True if user can edit</returns>
        public bool CanBeEditedBy(Guid userId)
        {
            if (IsDeleted) return false;

            // Only uploader or workspace participants can edit
            return UploadedBy == userId || Workspace?.IsAccessibleBy(userId) == true;
        }

        /// <summary>
        /// Checks if a user can delete this document
        /// </summary>
        /// <param name="userId">User ID to check</param>
        /// <returns>True if user can delete</returns>
        public bool CanBeDeletedBy(Guid userId)
        {
            if (IsDeleted) return false;

            // Only uploader can delete their own documents
            return UploadedBy == userId;
        }

        /// <summary>
        /// Checks if a user can restore this document from trash
        /// </summary>
        /// <param name="userId">User ID to check</param>
        /// <returns>True if user can restore</returns>
        public bool CanBeRestoredBy(Guid userId)
        {
            if (!IsDeleted) return false;

            // Only uploader can restore their own documents
            return UploadedBy == userId;
        }
    }
}