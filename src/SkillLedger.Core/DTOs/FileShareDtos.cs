using SkillLedger.Core.Enums;
using SkillLedger.Core.Models;
using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.DTOs
{
    // Document Upload DTOs
    public class UploadDocumentRequest
    {
        [Required]
        public Guid WorkspaceId { get; set; }

        [Required]
        public string FileName { get; set; } = string.Empty;

        [Required]
        public Stream FileStream { get; set; } = null!;

        [Required]
        public string ContentType { get; set; } = string.Empty;

        [Required]
        public long FileSize { get; set; }

        public Guid? FolderId { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(500)]
        public string? Tags { get; set; }

        public bool IsPrivate { get; set; } = false;

        public bool AutoGeneratePreview { get; set; } = true;
    }

    public class UploadMultipleDocumentsRequest
    {
        [Required]
        public Guid WorkspaceId { get; set; }

        [Required]
        public List<FileUploadItem> Files { get; set; } = new List<FileUploadItem>();

        public Guid? FolderId { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        public bool IsPrivate { get; set; } = false;
    }

    public class FileUploadItem
    {
        public string FileName { get; set; } = string.Empty;
        public Stream FileStream { get; set; } = null!;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
    }

    public class FileUploadResult
    {
        public bool Success { get; set; }
        public Guid? DocumentId { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string> ValidationErrors { get; set; } = new List<string>();
        public DocumentDto? Document { get; set; }
        public long FileSizeBytes { get; set; }
        public bool RequiresModerationApproval { get; set; }
        public SecurityScanResult? SecurityScanResult { get; set; }
    }

    // Document DTOs
    public class DocumentDto
    {
        public Guid Id { get; set; }
        public Guid WorkspaceId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string MimeType { get; set; } = string.Empty;
        public Guid UploadedBy { get; set; }
        public string UploaderName { get; set; } = string.Empty;
        public Guid? FolderId { get; set; }
        public string? FolderPath { get; set; }
        public int VersionNumber { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastAccessedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public bool SecurityScanPassed { get; set; }
        public string? PreviewUrl { get; set; }
        public string? DownloadUrl { get; set; }
        public string? Tags { get; set; }
        public string? Description { get; set; }
        public int AccessCount { get; set; }
        public int ShareCount { get; set; }
        public bool IsSharedWithUser { get; set; }
        public SharePermission? UserPermission { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanShare { get; set; }
        public List<DocumentShareDto> ActiveShares { get; set; } = new List<DocumentShareDto>();
        public DocumentVersionInfo? VersionInfo { get; set; }
    }

    public class DocumentVersionInfo
    {
        public bool IsLatestVersion { get; set; }
        public int TotalVersions { get; set; }
        public Guid? ParentDocumentId { get; set; }
        public List<DocumentVersionSummary> VersionHistory { get; set; } = new List<DocumentVersionSummary>();
    }

    public class DocumentVersionSummary
    {
        public int VersionNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UploaderName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string? ChangeDescription { get; set; }
    }

    public class UpdateDocumentRequest
    {
        [StringLength(500)]
        public string? FileName { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(500)]
        public string? Tags { get; set; }

        public Guid? FolderId { get; set; }

        public bool? IsPrivate { get; set; }
    }

    // Document Listing DTOs
    public class WorkspaceDocumentsRequest
    {
        [Required]
        public Guid WorkspaceId { get; set; }

        public Guid? FolderId { get; set; }
        public string? SearchQuery { get; set; }
        public List<string> FileTypes { get; set; } = new List<string>();
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public Guid? UploadedBy { get; set; }
        public bool IncludeDeleted { get; set; } = false;
        public DocumentSortBy SortBy { get; set; } = DocumentSortBy.CreatedAt;
        public bool SortDescending { get; set; } = true;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class SearchDocumentsRequest
    {
        [Required]
        public Guid WorkspaceId { get; set; }

        [Required]
        [StringLength(100)]
        public string SearchQuery { get; set; } = string.Empty;

        public List<string> FileTypes { get; set; } = new List<string>();
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public bool SearchInContent { get; set; } = false;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class DocumentListResponse
    {
        public List<DocumentDto> Documents { get; set; } = new List<DocumentDto>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
        public WorkspaceStorageStatsDto? StorageStats { get; set; }
    }

    // Folder Management DTOs
    public class DocumentFolderDto
    {
        public Guid Id { get; set; }
        public Guid WorkspaceId { get; set; }
        public string FolderName { get; set; } = string.Empty;
        public Guid? ParentFolderId { get; set; }
        public string? ParentFolderName { get; set; }
        public string FullPath { get; set; } = string.Empty;
        public Guid CreatedBy { get; set; }
        public string CreatorName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? Description { get; set; }
        public int SortOrder { get; set; }
        public int DocumentCount { get; set; }
        public int SubfolderCount { get; set; }
        public long TotalSize { get; set; }
        public bool IsDeleted { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public List<DocumentFolderDto> ChildFolders { get; set; } = new List<DocumentFolderDto>();
    }

    public class CreateFolderRequest
    {
        [Required]
        public Guid WorkspaceId { get; set; }

        [Required]
        [StringLength(200)]
        public string FolderName { get; set; } = string.Empty;

        public Guid? ParentFolderId { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }
    }

    public class UpdateFolderRequest
    {
        [StringLength(200)]
        public string? FolderName { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        public int? SortOrder { get; set; }
    }

    // Document Sharing DTOs
    public class ShareDocumentRequest
    {
        [Required]
        public Guid DocumentId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        public SharePermission Permission { get; set; } = SharePermission.View;

        public DateTime? ExpiresAt { get; set; }

        [StringLength(1000)]
        public string? ShareMessage { get; set; }

        public bool SendNotification { get; set; } = true;
    }

    public class DocumentShareDto
    {
        public Guid Id { get; set; }
        public Guid DocumentId { get; set; }
        public string DocumentName { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public Guid SharedBy { get; set; }
        public string SharedByName { get; set; } = string.Empty;
        public SharePermission Permission { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsActive { get; set; }
        public string? ShareMessage { get; set; }
        public DateTime? LastAccessedAt { get; set; }
        public int AccessCount { get; set; }
        public bool IsExpiringSoon { get; set; }
        public bool CanRevoke { get; set; }
        public bool CanModifyPermissions { get; set; }
    }

    public class BulkShareRequest
    {
        [Required]
        public List<Guid> DocumentIds { get; set; } = new List<Guid>();

        [Required]
        public List<Guid> UserIds { get; set; } = new List<Guid>();

        public SharePermission Permission { get; set; } = SharePermission.View;

        public DateTime? ExpiresAt { get; set; }

        [StringLength(1000)]
        public string? ShareMessage { get; set; }

        public bool SendNotifications { get; set; } = true;
    }

    // Version Control DTOs
    public class DocumentVersionDto
    {
        public Guid Id { get; set; }
        public int VersionNumber { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public Guid UploadedBy { get; set; }
        public string UploaderName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? ChangeDescription { get; set; }
        public bool IsCurrentVersion { get; set; }
        public string? DownloadUrl { get; set; }
        public bool CanRevert { get; set; }
    }

    // Bulk Operations DTOs
    public class BulkOperationResult
    {
        public int TotalRequested { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<BulkOperationError> Errors { get; set; } = new List<BulkOperationError>();
        public bool IsPartialSuccess => SuccessCount > 0 && FailureCount > 0;
        public bool IsCompleteSuccess => SuccessCount == TotalRequested;
        public bool IsCompleteFailure => SuccessCount == 0;
    }

    public class BulkOperationError
    {
        public Guid ItemId { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
    }

    // Storage and Analytics DTOs
    public class WorkspaceStorageStatsDto
    {
        public Guid WorkspaceId { get; set; }
        public long TotalSizeBytes { get; set; }
        public int DocumentCount { get; set; }
        public int FolderCount { get; set; }
        public int ActiveDocuments { get; set; }
        public int DeletedDocuments { get; set; }
        public long QuotaLimitBytes { get; set; }
        public double UsagePercentage { get; set; }
        public Dictionary<string, int> DocumentsByType { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, long> SizeByType { get; set; } = new Dictionary<string, long>();
        public List<DocumentDto> LargestDocuments { get; set; } = new List<DocumentDto>();
        public List<DocumentDto> MostAccessedDocuments { get; set; } = new List<DocumentDto>();
        public DateTime? LastActivityAt { get; set; }
    }

    public class UserStorageQuotaDto
    {
        public Guid UserId { get; set; }
        public long TotalQuotaBytes { get; set; }
        public long UsedBytes { get; set; }
        public long AvailableBytes { get; set; }
        public double UsagePercentage { get; set; }
        public int DocumentCount { get; set; }
        public int WorkspaceCount { get; set; }
        public DateTime QuotaPeriodStart { get; set; }
        public DateTime QuotaPeriodEnd { get; set; }
        public bool IsOverQuota { get; set; }
        public bool IsApproachingQuota { get; set; }
        public List<WorkspaceStorageStatsDto> WorkspaceBreakdown { get; set; } = new List<WorkspaceStorageStatsDto>();
    }

    public class DocumentAccessDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public DateTime AccessedAt { get; set; }
        public string AccessType { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? DeviceInfo { get; set; }
    }

    public class DocumentAnalyticsDto
    {
        public Guid DocumentId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public int TotalViews { get; set; }
        public int UniqueViewers { get; set; }
        public int DownloadCount { get; set; }
        public int ShareCount { get; set; }
        public DateTime? LastAccessedAt { get; set; }
        public List<DocumentAccessDto> RecentAccess { get; set; } = new List<DocumentAccessDto>();
        public Dictionary<DateTime, int> ViewsByDay { get; set; } = new Dictionary<DateTime, int>();
        public Dictionary<string, int> AccessByUserAgent { get; set; } = new Dictionary<string, int>();
        public List<DocumentShareDto> ActiveShares { get; set; } = new List<DocumentShareDto>();
        public TimeSpan AverageViewDuration { get; set; }
    }

    // Security and Validation DTOs - Uses existing SecurityScanResult from IMediaUploadService

    // Integration DTOs
    public class ShareDocumentInMessageRequest
    {
        [Required]
        public Guid WorkspaceId { get; set; }

        [Required]
        public Guid DocumentId { get; set; }

        [StringLength(1000)]
        public string? MessageText { get; set; }

        public Guid? ReplyToMessageId { get; set; }

        public bool ShareWithAllParticipants { get; set; } = false;

        public SharePermission Permission { get; set; } = SharePermission.View;
    }

    // Enums
    public enum DocumentSortBy
    {
        FileName = 0,
        FileSize = 1,
        CreatedAt = 2,
        LastAccessedAt = 3,
        UploadedBy = 4,
        FileType = 5
    }
}