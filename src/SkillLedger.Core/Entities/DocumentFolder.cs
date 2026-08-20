using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.Entities
{
    /// <summary>
    /// Represents a folder for organizing workspace documents
    /// </summary>
    public class DocumentFolder
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The workspace this folder belongs to
        /// </summary>
        [Required]
        public Guid WorkspaceId { get; set; }
        public ProjectWorkspace Workspace { get; set; } = null!;

        /// <summary>
        /// Name of the folder
        /// </summary>
        [Required]
        [StringLength(200)]
        public string FolderName { get; set; } = string.Empty;

        /// <summary>
        /// Parent folder for nested folder structure (optional)
        /// </summary>
        public Guid? ParentFolderId { get; set; }
        public DocumentFolder? ParentFolder { get; set; }

        /// <summary>
        /// User who created the folder
        /// </summary>
        [Required]
        public Guid CreatedBy { get; set; }
        public User Creator { get; set; } = null!;

        /// <summary>
        /// When the folder was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Whether the folder is marked as deleted
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// When the folder was deleted (soft delete)
        /// </summary>
        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// User who deleted the folder
        /// </summary>
        public Guid? DeletedBy { get; set; }
        public User? Deleter { get; set; }

        /// <summary>
        /// Description of the folder's purpose (optional)
        /// </summary>
        [StringLength(1000)]
        public string? Description { get; set; }

        /// <summary>
        /// Sort order within the parent folder
        /// </summary>
        public int SortOrder { get; set; } = 0;

        /// <summary>
        /// Child folders within this folder
        /// </summary>
        public ICollection<DocumentFolder> ChildFolders { get; set; } = new List<DocumentFolder>();

        /// <summary>
        /// Documents contained in this folder
        /// </summary>
        public ICollection<WorkspaceDocument> Documents { get; set; } = new List<WorkspaceDocument>();

        /// <summary>
        /// Gets the full path of the folder (including parent folders)
        /// </summary>
        /// <returns>The full folder path</returns>
        public string GetFullPath()
        {
            if (ParentFolder == null)
                return FolderName;

            return $"{ParentFolder.GetFullPath()}/{FolderName}";
        }

        /// <summary>
        /// Soft deletes the folder and all its contents
        /// </summary>
        /// <param name="userId">User performing the deletion</param>
        public void Delete(Guid userId)
        {
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
            DeletedBy = userId;

            // Soft delete all child folders
            foreach (var childFolder in ChildFolders.Where(f => !f.IsDeleted))
            {
                childFolder.Delete(userId);
            }

            // Soft delete all documents in this folder
            foreach (var document in Documents.Where(d => !d.IsDeleted))
            {
                document.Delete(userId);
            }
        }

        /// <summary>
        /// Restores a soft-deleted folder
        /// </summary>
        public void Restore()
        {
            IsDeleted = false;
            DeletedAt = null;
            DeletedBy = null;
        }

        /// <summary>
        /// Checks if a user can access this folder
        /// </summary>
        /// <param name="userId">User ID to check</param>
        /// <returns>True if user has access</returns>
        public bool CanBeAccessedBy(Guid userId)
        {
            if (IsDeleted) return false;

            // Check if user has workspace access
            return Workspace?.IsAccessibleBy(userId) == true;
        }

        /// <summary>
        /// Checks if a user can edit this folder
        /// </summary>
        /// <param name="userId">User ID to check</param>
        /// <returns>True if user can edit</returns>
        public bool CanBeEditedBy(Guid userId)
        {
            if (IsDeleted) return false;

            // Only creator or workspace participants can edit
            return CreatedBy == userId || Workspace?.IsAccessibleBy(userId) == true;
        }

        /// <summary>
        /// Checks if a user can delete this folder
        /// </summary>
        /// <param name="userId">User ID to check</param>
        /// <returns>True if user can delete</returns>
        public bool CanBeDeletedBy(Guid userId)
        {
            if (IsDeleted) return false;

            // Only creator can delete their own folders
            return CreatedBy == userId;
        }

    }
}