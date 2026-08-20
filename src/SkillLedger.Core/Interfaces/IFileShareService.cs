using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Models;

namespace SkillLedger.Core.Interfaces
{
    /// <summary>
    /// Service for managing workspace file sharing and document management
    /// </summary>
    public interface IFileShareService
    {
        // Document Upload Operations
        Task<FileUploadResult> UploadDocumentAsync(UploadDocumentRequest request, Guid userId);
        Task<FileUploadResult> UploadMultipleDocumentsAsync(UploadMultipleDocumentsRequest request, Guid userId);

        // Document Access Operations
        Task<DocumentDto?> GetDocumentAsync(Guid documentId, Guid userId);
        Task<Stream?> DownloadDocumentAsync(Guid documentId, Guid userId);
        Task<string?> GetSecureDownloadUrlAsync(Guid documentId, Guid userId, int expirationMinutes = 60);
        Task<byte[]?> GetDocumentPreviewAsync(Guid documentId, Guid userId);

        // Document Management Operations
        Task<bool> DeleteDocumentAsync(Guid documentId, Guid userId);
        Task<bool> RestoreDocumentAsync(Guid documentId, Guid userId);
        Task<DocumentDto> UpdateDocumentMetadataAsync(Guid documentId, UpdateDocumentRequest request, Guid userId);
        Task<DocumentDto> CreateNewVersionAsync(Guid documentId, UploadDocumentRequest request, Guid userId);

        // Document Listing and Search
        Task<DocumentListResponse> GetWorkspaceDocumentsAsync(WorkspaceDocumentsRequest request, Guid userId);
        Task<DocumentListResponse> SearchDocumentsAsync(SearchDocumentsRequest request, Guid userId);
        Task<DocumentListResponse> GetRecentDocumentsAsync(Guid workspaceId, Guid userId, int count = 10);
        Task<DocumentListResponse> GetDocumentsByFolderAsync(Guid? folderId, Guid userId, int pageNumber = 1, int pageSize = 20);

        // Folder Management Operations
        Task<DocumentFolderDto> CreateFolderAsync(CreateFolderRequest request, Guid userId);
        Task<DocumentFolderDto> UpdateFolderAsync(Guid folderId, UpdateFolderRequest request, Guid userId);
        Task<bool> DeleteFolderAsync(Guid folderId, Guid userId);
        Task<bool> RestoreFolderAsync(Guid folderId, Guid userId);
        Task<DocumentFolderDto> MoveFolderAsync(Guid folderId, Guid? newParentFolderId, Guid userId);
        Task<List<DocumentFolderDto>> GetFolderStructureAsync(Guid workspaceId, Guid userId);

        // Document Sharing Operations
        Task<DocumentShareDto> ShareDocumentAsync(ShareDocumentRequest request, Guid userId);
        Task<bool> RevokeDocumentShareAsync(Guid shareId, Guid userId);
        Task<List<DocumentShareDto>> GetDocumentSharesAsync(Guid documentId, Guid userId);
        Task<List<DocumentDto>> GetSharedWithMeDocumentsAsync(Guid userId, int pageNumber = 1, int pageSize = 20);
        Task<DocumentDto> UpdateSharePermissionsAsync(Guid shareId, SharePermission newPermission, Guid userId);

        // Version Control Operations
        Task<List<DocumentVersionDto>> GetDocumentVersionsAsync(Guid documentId, Guid userId);
        Task<DocumentDto?> GetDocumentVersionAsync(Guid documentId, int versionNumber, Guid userId);
        Task<bool> RevertToVersionAsync(Guid documentId, int versionNumber, Guid userId);

        // Bulk Operations
        Task<BulkOperationResult> BulkDeleteDocumentsAsync(List<Guid> documentIds, Guid userId);
        Task<BulkOperationResult> BulkMoveDocumentsAsync(List<Guid> documentIds, Guid? folderId, Guid userId);
        Task<BulkOperationResult> BulkShareDocumentsAsync(BulkShareRequest request, Guid userId);

        // Storage and Analytics
        Task<WorkspaceStorageStatsDto> GetWorkspaceStorageStatsAsync(Guid workspaceId, Guid userId);
        Task<UserStorageQuotaDto> GetUserStorageQuotaAsync(Guid userId);
        Task<List<DocumentAccessDto>> GetDocumentAccessHistoryAsync(Guid documentId, Guid userId);
        Task<DocumentAnalyticsDto> GetDocumentAnalyticsAsync(Guid documentId, Guid userId);

        // Security and Validation
        Task<bool> ValidateDocumentAccessAsync(Guid documentId, Guid userId, SharePermission requiredPermission = SharePermission.View);
        Task<SecurityScanResult> RescanDocumentSecurityAsync(Guid documentId, Guid userId);
        Task<List<DocumentDto>> GetPendingModerationDocumentsAsync(Guid workspaceId, Guid userId);
        Task<bool> ApproveDocumentAsync(Guid documentId, Guid userId);
        Task<bool> RejectDocumentAsync(Guid documentId, string reason, Guid userId);

        // Integration with Messaging System
        Task<bool> SendDocumentNotificationAsync(Guid documentId, DocumentNotificationType notificationType, Guid recipientUserId, Guid senderId);
        Task<MessageDto> ShareDocumentInMessageAsync(ShareDocumentInMessageRequest request, Guid userId);
    }
}

/// <summary>
/// Types of document notifications for messaging integration
/// </summary>
public enum DocumentNotificationType
{
    DocumentShared,
    DocumentUpdated,
    DocumentDeleted,
    DocumentCommented,
    NewVersion,
    ShareExpiring,
    ShareRevoked
}