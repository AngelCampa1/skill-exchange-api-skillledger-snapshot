using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Models;

namespace SkillLedger.Core.Interfaces
{
    /// <summary>
    /// Service for managing workspace documents and folders
    /// Provides comprehensive document management functionality
    /// </summary>
    public interface IDocumentService
    {
        /// <summary>
        /// Uploads a new document to a workspace
        /// </summary>
        /// <param name="request">Document upload request</param>
        /// <param name="userId">User performing the upload</param>
        /// <returns>Result of the upload operation</returns>
        Task<DocumentUploadResult> UploadDocumentAsync(DocumentUploadRequest request, Guid userId);

        /// <summary>
        /// Downloads a document by ID
        /// </summary>
        /// <param name="documentId">Document ID</param>
        /// <param name="userId">User requesting the download</param>
        /// <returns>Document download information</returns>
        Task<DocumentDownloadResult> DownloadDocumentAsync(Guid documentId, Guid userId);

        /// <summary>
        /// Gets document metadata without downloading the file
        /// </summary>
        /// <param name="documentId">Document ID</param>
        /// <param name="userId">User requesting the metadata</param>
        /// <returns>Document metadata</returns>
        Task<WorkspaceDocument?> GetDocumentAsync(Guid documentId, Guid userId);

        /// <summary>
        /// Gets all documents in a workspace or folder
        /// </summary>
        /// <param name="workspaceId">Workspace ID</param>
        /// <param name="folderId">Optional folder ID to filter by</param>
        /// <param name="userId">User requesting the documents</param>
        /// <param name="includeDeleted">Whether to include soft-deleted documents</param>
        /// <returns>List of documents</returns>
        Task<List<WorkspaceDocument>> GetDocumentsAsync(Guid workspaceId, Guid? folderId = null, Guid? userId = null, bool includeDeleted = false);

        /// <summary>
        /// Updates document metadata (name, description, etc.)
        /// </summary>
        /// <param name="documentId">Document ID</param>
        /// <param name="request">Update request</param>
        /// <param name="userId">User performing the update</param>
        /// <returns>Updated document</returns>
        Task<WorkspaceDocument?> UpdateDocumentAsync(Guid documentId, DocumentUpdateRequest request, Guid userId);

        /// <summary>
        /// Soft deletes a document
        /// </summary>
        /// <param name="documentId">Document ID</param>
        /// <param name="userId">User performing the deletion</param>
        /// <returns>True if deleted successfully</returns>
        Task<bool> DeleteDocumentAsync(Guid documentId, Guid userId);

        /// <summary>
        /// Permanently deletes a document and its file
        /// </summary>
        /// <param name="documentId">Document ID</param>
        /// <param name="userId">User performing the permanent deletion</param>
        /// <returns>True if permanently deleted</returns>
        Task<bool> PermanentlyDeleteDocumentAsync(Guid documentId, Guid userId);

        /// <summary>
        /// Restores a soft-deleted document
        /// </summary>
        /// <param name="documentId">Document ID</param>
        /// <param name="userId">User performing the restore</param>
        /// <returns>Restored document or null if failed</returns>
        Task<WorkspaceDocument?> RestoreDocumentAsync(Guid documentId, Guid userId);

        /// <summary>
        /// Moves a document to a different folder
        /// </summary>
        /// <param name="documentId">Document ID</param>
        /// <param name="targetFolderId">Target folder ID (null for root)</param>
        /// <param name="userId">User performing the move</param>
        /// <returns>True if moved successfully</returns>
        Task<bool> MoveDocumentAsync(Guid documentId, Guid? targetFolderId, Guid userId);

        /// <summary>
        /// Creates a new version of an existing document
        /// </summary>
        /// <param name="documentId">Original document ID</param>
        /// <param name="request">New version upload request</param>
        /// <param name="userId">User creating the version</param>
        /// <returns>New version document</returns>
        Task<DocumentUploadResult> CreateDocumentVersionAsync(Guid documentId, DocumentUploadRequest request, Guid userId);

        /// <summary>
        /// Gets all versions of a document
        /// </summary>
        /// <param name="documentId">Document ID</param>
        /// <param name="userId">User requesting the versions</param>
        /// <returns>List of document versions</returns>
        Task<List<WorkspaceDocument>> GetDocumentVersionsAsync(Guid documentId, Guid userId);

        /// <summary>
        /// Creates a new folder in a workspace
        /// </summary>
        /// <param name="request">Folder creation request</param>
        /// <param name="userId">User creating the folder</param>
        /// <returns>Created folder</returns>
        Task<DocumentFolder?> CreateFolderAsync(DocumentFolderCreateRequest request, Guid userId);

        /// <summary>
        /// Gets all folders in a workspace
        /// </summary>
        /// <param name="workspaceId">Workspace ID</param>
        /// <param name="parentFolderId">Parent folder ID (null for root folders)</param>
        /// <param name="userId">User requesting the folders</param>
        /// <param name="includeDeleted">Whether to include soft-deleted folders</param>
        /// <returns>List of folders</returns>
        Task<List<DocumentFolder>> GetFoldersAsync(Guid workspaceId, Guid? parentFolderId = null, Guid? userId = null, bool includeDeleted = false);

        /// <summary>
        /// Updates folder metadata
        /// </summary>
        /// <param name="folderId">Folder ID</param>
        /// <param name="request">Update request</param>
        /// <param name="userId">User performing the update</param>
        /// <returns>Updated folder</returns>
        Task<DocumentFolder?> UpdateFolderAsync(Guid folderId, DocumentFolderUpdateRequest request, Guid userId);

        /// <summary>
        /// Soft deletes a folder and all its contents
        /// </summary>
        /// <param name="folderId">Folder ID</param>
        /// <param name="userId">User performing the deletion</param>
        /// <returns>True if deleted successfully</returns>
        Task<bool> DeleteFolderAsync(Guid folderId, Guid userId);

        /// <summary>
        /// Restores a soft-deleted folder
        /// </summary>
        /// <param name="folderId">Folder ID</param>
        /// <param name="userId">User performing the restore</param>
        /// <returns>Restored folder or null if failed</returns>
        Task<DocumentFolder?> RestoreFolderAsync(Guid folderId, Guid userId);

        /// <summary>
        /// Moves a folder to a different parent folder
        /// </summary>
        /// <param name="folderId">Folder ID</param>
        /// <param name="targetParentFolderId">Target parent folder ID (null for root)</param>
        /// <param name="userId">User performing the move</param>
        /// <returns>True if moved successfully</returns>
        Task<bool> MoveFolderAsync(Guid folderId, Guid? targetParentFolderId, Guid userId);

        /// <summary>
        /// Searches for documents and folders
        /// </summary>
        /// <param name="workspaceId">Workspace ID</param>
        /// <param name="searchQuery">Search query</param>
        /// <param name="userId">User performing the search</param>
        /// <param name="filters">Optional search filters</param>
        /// <returns>Search results</returns>
        Task<DocumentBasicSearchResult> SearchDocumentsAsync(Guid workspaceId, string searchQuery, Guid userId, DocumentSearchFilters? filters = null);

        /// <summary>
        /// Gets document access history
        /// </summary>
        /// <param name="documentId">Document ID</param>
        /// <param name="userId">User requesting the history</param>
        /// <returns>Access history</returns>
        Task<List<DocumentAccess>> GetDocumentAccessHistoryAsync(Guid documentId, Guid userId);

        /// <summary>
        /// Gets storage usage statistics for a workspace
        /// </summary>
        /// <param name="workspaceId">Workspace ID</param>
        /// <param name="userId">User requesting the statistics</param>
        /// <returns>Storage statistics</returns>
        Task<DocumentStorageStats> GetStorageStatsAsync(Guid workspaceId, Guid userId);

        /// <summary>
        /// Scans a document for security threats
        /// </summary>
        /// <param name="documentId">Document ID</param>
        /// <param name="userId">User requesting the scan</param>
        /// <returns>Scan result</returns>
        Task<SecurityScanResult> ScanDocumentAsync(Guid documentId, Guid userId);

        /// <summary>
        /// Gets a secure, time-limited URL for file access
        /// </summary>
        /// <param name="documentId">Document ID</param>
        /// <param name="userId">User requesting access</param>
        /// <param name="expirationMinutes">URL expiration time in minutes</param>
        /// <returns>Secure URL</returns>
        Task<string?> GetSecureDownloadUrlAsync(Guid documentId, Guid userId, int expirationMinutes = 60);

        /// <summary>
        /// Validates file upload requirements
        /// </summary>
        /// <param name="request">Upload request to validate</param>
        /// <returns>Validation result</returns>
        Task<DocumentValidationResult> ValidateUploadAsync(DocumentUploadRequest request);

        /// <summary>
        /// Gets workspace document tree (folders and documents hierarchically)
        /// </summary>
        /// <param name="workspaceId">Workspace ID</param>
        /// <param name="userId">User requesting the tree</param>
        /// <param name="includeDeleted">Whether to include deleted items</param>
        /// <returns>Document tree structure</returns>
        Task<DocumentTreeResult> GetDocumentTreeAsync(Guid workspaceId, Guid userId, bool includeDeleted = false);
    }

    // Supporting DTOs
    public class DocumentUploadRequest
    {
        public Guid WorkspaceId { get; set; }
        public Guid? FolderId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public Stream FileStream { get; set; } = null!;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string? Description { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();
        public bool ReplaceExisting { get; set; } = false;
    }

    public class DocumentUploadResult
    {
        public bool Success { get; set; }
        public WorkspaceDocument? Document { get; set; }
        public string? ErrorMessage { get; set; }
        public SecurityScanResult? SecurityScan { get; set; }
    }

    public class DocumentDownloadResult
    {
        public bool Success { get; set; }
        public WorkspaceDocument? Document { get; set; }
        public Stream? FileStream { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class DocumentUpdateRequest
    {
        public string? FileName { get; set; }
        public string? Description { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
    }

    public class DocumentFolderCreateRequest
    {
        public Guid WorkspaceId { get; set; }
        public Guid? ParentFolderId { get; set; }
        public string FolderName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int SortOrder { get; set; } = 0;
    }

    public class DocumentFolderUpdateRequest
    {
        public string? FolderName { get; set; }
        public string? Description { get; set; }
        public int? SortOrder { get; set; }
    }

    public class DocumentSearchFilters
    {
        public string? FileType { get; set; }
        public DateTime? CreatedAfter { get; set; }
        public DateTime? CreatedBefore { get; set; }
        public Guid? UploadedBy { get; set; }
        public long? MinSize { get; set; }
        public long? MaxSize { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    public class DocumentBasicSearchResult
    {
        public List<WorkspaceDocument> Documents { get; set; } = new();
        public List<DocumentFolder> Folders { get; set; } = new();
        public int TotalResults { get; set; }
        public string SearchQuery { get; set; } = string.Empty;
    }

    public class DocumentStorageStats
    {
        public Guid WorkspaceId { get; set; }
        public int TotalFiles { get; set; }
        public long TotalSizeBytes { get; set; }
        public int TotalFolders { get; set; }
        public Dictionary<string, int> FileTypeDistribution { get; set; } = new();
        public Dictionary<string, long> FileTypeSizes { get; set; } = new();
        public DateTime LastActivity { get; set; }
    }

    // SecurityScanResult is defined in IMediaUploadService.cs - using that shared definition

    public class DocumentValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

    public class DocumentTreeResult
    {
        public Guid WorkspaceId { get; set; }
        public List<DocumentTreeNode> RootNodes { get; set; } = new();
    }

    public class DocumentTreeNode
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsFolder { get; set; }
        public long? FileSize { get; set; }
        public string? MimeType { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public List<DocumentTreeNode> Children { get; set; } = new();
    }
}