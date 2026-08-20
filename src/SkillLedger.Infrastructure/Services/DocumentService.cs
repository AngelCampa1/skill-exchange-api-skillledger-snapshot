using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Interfaces;
using SkillLedger.Core.Models;
using SkillLedger.Infrastructure.Configuration;
using SkillLedger.Infrastructure.Data;

namespace SkillLedger.Infrastructure.Services
{
    /// <summary>
    /// Service for managing workspace documents and folders
    /// Implements comprehensive document management functionality with security
    /// </summary>
    public class DocumentService : IDocumentService
    {
        private readonly SkillLedgerDbContext _context;
        private readonly IFileStorageService _fileStorageService;
        private readonly IVirusScanService _virusScanService;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<DocumentService> _logger;
        private readonly MediaUploadConfiguration _config;

        public DocumentService(
            SkillLedgerDbContext context,
            IFileStorageService fileStorageService,
            IVirusScanService virusScanService,
            IAuditLogService auditLogService,
            ILogger<DocumentService> logger,
            IOptions<MediaUploadConfiguration> config)
        {
            _context = context;
            _fileStorageService = fileStorageService;
            _virusScanService = virusScanService;
            _auditLogService = auditLogService;
            _logger = logger;
            _config = config.Value;
        }

        public async Task<DocumentUploadResult> UploadDocumentAsync(DocumentUploadRequest request, Guid userId)
        {
            var result = new DocumentUploadResult();

            try
            {
                // Validate the upload request
                var validationResult = await ValidateUploadAsync(request);
                if (!validationResult.IsValid)
                {
                    result.Success = false;
                    result.ErrorMessage = string.Join("; ", validationResult.Errors);
                    return result;
                }

                // Verify workspace access
                var workspace = await _context.ProjectWorkspaces
                    .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId);

                if (workspace == null || !workspace.IsAccessibleBy(userId))
                {
                    result.Success = false;
                    result.ErrorMessage = "Workspace not found or access denied";
                    return result;
                }

                // Verify folder access if specified
                DocumentFolder? folder = null;
                if (request.FolderId.HasValue)
                {
                    folder = await _context.DocumentFolders
                        .FirstOrDefaultAsync(f => f.Id == request.FolderId.Value && !f.IsDeleted);

                    if (folder == null || !folder.CanBeAccessedBy(userId))
                    {
                        result.Success = false;
                        result.ErrorMessage = "Folder not found or access denied";
                        return result;
                    }
                }

                // Perform virus scan
                var scanResult = await _virusScanService.ScanFileAsync(request.FileStream, request.FileName, request.ContentType);
                result.SecurityScan = new SecurityScanResult
                {
                    ScanPassed = scanResult.IsClean,
                    ThreatDetected = !scanResult.IsClean,
                    ThreatTypes = scanResult.Threats.Select(t => t.ThreatName),
                    ScanEngine = scanResult.ScanEngine,
                    ScanTimestamp = scanResult.ScanDate
                };

                if (!scanResult.IsClean)
                {
                    result.Success = false;
                    result.ErrorMessage = $"File failed security scan: {string.Join(", ", scanResult.Threats.Select(t => t.ThreatName))}";

                    await _auditLogService.LogEventAsync(
                        userId,
                        "DocumentUpload",
                        "unknown", // IP address not available in service layer
                        null, // User agent not available in service layer
                        false,
                        $"Virus detected in file upload: {request.FileName}. Threats: {string.Join(", ", scanResult.Threats.Select(t => t.ThreatName))}",
                        "File failed security scan");

                    return result;
                }

                // Reset stream position after scanning
                request.FileStream.Position = 0;

                // Handle file replacement if requested
                WorkspaceDocument? existingDocument = null;
                if (request.ReplaceExisting)
                {
                    existingDocument = await _context.WorkspaceDocuments
                        .FirstOrDefaultAsync(d => d.WorkspaceId == request.WorkspaceId &&
                                                  d.FolderId == request.FolderId &&
                                                  d.FileName == request.FileName &&
                                                  !d.IsDeleted);
                }

                // Upload file to storage
                var containerPath = $"workspaces/{request.WorkspaceId}";
                var storageRequest = new FileStorageUploadRequest
                {
                    FileName = request.FileName,
                    FileStream = request.FileStream,
                    ContentType = request.ContentType,
                    FileSize = request.FileSize,
                    ContainerPath = containerPath,
                    Metadata = request.Metadata,
                    OverwriteIfExists = request.ReplaceExisting
                };

                var storageResult = await _fileStorageService.UploadFileAsync(storageRequest);
                if (!storageResult.Success)
                {
                    result.Success = false;
                    result.ErrorMessage = $"File storage failed: {storageResult.ErrorMessage}";
                    return result;
                }

                // Create or update document entity
                WorkspaceDocument document;

                if (existingDocument != null)
                {
                    // Create new version
                    document = new WorkspaceDocument
                    {
                        WorkspaceId = request.WorkspaceId,
                        FolderId = request.FolderId,
                        FileName = request.FileName,
                        FilePath = storageResult.FilePath!,
                        FileSize = request.FileSize,
                        MimeType = request.ContentType,
                        UploadedBy = userId,
                        VersionNumber = existingDocument.VersionNumber + 1,
                        ParentDocumentId = existingDocument.ParentDocumentId ?? existingDocument.Id,
                        SecurityScanPassed = true,
                        SecurityScanResult = System.Text.Json.JsonSerializer.Serialize(result.SecurityScan)
                    };

                    // Mark existing as previous version
                    existingDocument.PreviousVersions.Add(document);
                }
                else
                {
                    // Create new document
                    document = new WorkspaceDocument
                    {
                        WorkspaceId = request.WorkspaceId,
                        FolderId = request.FolderId,
                        FileName = request.FileName,
                        FilePath = storageResult.FilePath!,
                        FileSize = request.FileSize,
                        MimeType = request.ContentType,
                        UploadedBy = userId,
                        SecurityScanPassed = true,
                        SecurityScanResult = System.Text.Json.JsonSerializer.Serialize(result.SecurityScan)
                    };
                }

                _context.WorkspaceDocuments.Add(document);
                await _context.SaveChangesAsync();

                result.Success = true;
                result.Document = document;

                await _auditLogService.LogEventAsync(
                    userId,
                    "DocumentUploaded",
                    "system",
                    null,
                    true,
                    System.Text.Json.JsonSerializer.Serialize(new { DocumentId = document.Id, WorkspaceId = request.WorkspaceId, FileName = request.FileName }));

                _logger.LogInformation("Document uploaded successfully: {DocumentId} by user {UserId}",
                    document.Id, userId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document {FileName} to workspace {WorkspaceId} by user {UserId}",
                    request.FileName, request.WorkspaceId, userId);

                result.Success = false;
                result.ErrorMessage = "An error occurred while uploading the document";
                return result;
            }
        }

        public async Task<DocumentDownloadResult> DownloadDocumentAsync(Guid documentId, Guid userId)
        {
            var result = new DocumentDownloadResult();

            try
            {
                // BUG-MED-008 FIX: Use AsSplitQuery for multiple includes to prevent cartesian explosion
                var document = await _context.WorkspaceDocuments
                    .Include(d => d.Workspace)
                    .Include(d => d.Uploader)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted);

                if (document == null)
                {
                    result.Success = false;
                    result.ErrorMessage = "Document not found";
                    return result;
                }

                if (!document.CanBeAccessedBy(userId))
                {
                    result.Success = false;
                    result.ErrorMessage = "Access denied";

                    await _auditLogService.LogEventAsync(
                        userId,
                        "DocumentAccessDenied",
                        "system",
                        null,
                        false,
                        System.Text.Json.JsonSerializer.Serialize(new { DocumentId = documentId, FileName = document.FileName }),
                        "Unauthorized access attempt");

                    return result;
                }

                // BUG FIX BE-HIGH-003: Download file from storage with proper resource management
                // Use try-catch to ensure fileStream is disposed on error
                Stream? fileStream = null;
                try
                {
                    fileStream = await _fileStorageService.DownloadFileAsync(document.FilePath);
                    if (fileStream == null)
                    {
                        result.Success = false;
                        result.ErrorMessage = "File not found in storage";
                        return result;
                    }

                    // Record access
                    document.RecordAccess(userId);
                    await _context.SaveChangesAsync();

                    result.Success = true;
                    result.Document = document;
                    result.FileStream = fileStream;

                    await _auditLogService.LogEventAsync(
                        userId,
                        "DocumentDownloaded",
                        "system",
                        null,
                        true,
                        System.Text.Json.JsonSerializer.Serialize(new { DocumentId = documentId, FileName = document.FileName }));

                    return result;
                }
                catch
                {
                    // Dispose the stream on error to prevent resource leak
                    fileStream?.Dispose();
                    throw; // Re-throw to be handled by outer catch
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading document {DocumentId} by user {UserId}", documentId, userId);

                result.Success = false;
                result.ErrorMessage = "An error occurred while downloading the document";
                return result;
            }
        }

        public async Task<WorkspaceDocument?> GetDocumentAsync(Guid documentId, Guid userId)
        {
            try
            {
                // BUG-MED-008 FIX: Use AsSplitQuery for multiple includes to prevent cartesian explosion
                var document = await _context.WorkspaceDocuments
                    .Include(d => d.Workspace)
                    .Include(d => d.Uploader)
                    .Include(d => d.Folder)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted);

                if (document == null || !document.CanBeAccessedBy(userId))
                {
                    return null;
                }

                return document;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document {DocumentId} by user {UserId}", documentId, userId);
                return null;
            }
        }

        public async Task<List<WorkspaceDocument>> GetDocumentsAsync(Guid workspaceId, Guid? folderId = null, Guid? userId = null, bool includeDeleted = false)
        {
            try
            {
                // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
                var query = _context.WorkspaceDocuments
                    .Include(d => d.Uploader)
                    .Include(d => d.Folder)
                    .AsSplitQuery()
                    .Where(d => d.WorkspaceId == workspaceId);

                if (folderId.HasValue)
                {
                    query = query.Where(d => d.FolderId == folderId.Value);
                }
                else
                {
                    query = query.Where(d => d.FolderId == null);
                }

                if (!includeDeleted)
                {
                    query = query.Where(d => !d.IsDeleted);
                }

                if (userId.HasValue)
                {
                    // Filter by documents the user can access
                    var workspace = await _context.ProjectWorkspaces.FindAsync(workspaceId);
                    if (workspace == null || !workspace.IsAccessibleBy(userId.Value))
                    {
                        return new List<WorkspaceDocument>();
                    }
                }

                return await query.OrderBy(d => d.FileName).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting documents for workspace {WorkspaceId}", workspaceId);
                return new List<WorkspaceDocument>();
            }
        }

        public async Task<WorkspaceDocument?> UpdateDocumentAsync(Guid documentId, DocumentUpdateRequest request, Guid userId)
        {
            try
            {
                var document = await _context.WorkspaceDocuments
                    .FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted);

                if (document == null || !document.CanBeEditedBy(userId))
                {
                    return null;
                }

                if (!string.IsNullOrWhiteSpace(request.FileName))
                {
                    document.FileName = request.FileName;
                }

                if (request.Metadata != null)
                {
                    // Update metadata (implementation depends on how you store metadata)
                    // This is a simplified version
                }

                await _context.SaveChangesAsync();

                await _auditLogService.LogEventAsync(
                    userId,
                    "DocumentUpdated",
                    "system",
                    null,
                    true,
                    System.Text.Json.JsonSerializer.Serialize(new { DocumentId = documentId, FileName = document.FileName }));

                return document;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating document {DocumentId} by user {UserId}", documentId, userId);
                return null;
            }
        }

        public async Task<bool> DeleteDocumentAsync(Guid documentId, Guid userId)
        {
            try
            {
                var document = await _context.WorkspaceDocuments
                    .FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted);

                if (document == null || !document.CanBeDeletedBy(userId))
                {
                    return false;
                }

                document.Delete(userId);
                await _context.SaveChangesAsync();

                await _auditLogService.LogEventAsync(
                    userId,
                    "DocumentDeleted",
                    "system",
                    null,
                    true,
                    System.Text.Json.JsonSerializer.Serialize(new { DocumentId = documentId, FileName = document.FileName }));

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document {DocumentId} by user {UserId}", documentId, userId);
                return false;
            }
        }

        public async Task<bool> PermanentlyDeleteDocumentAsync(Guid documentId, Guid userId)
        {
            try
            {
                var document = await _context.WorkspaceDocuments
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (document == null || !document.CanBeDeletedBy(userId))
                {
                    return false;
                }

                // Delete from storage
                await _fileStorageService.DeleteFileAsync(document.FilePath);

                // Remove from database
                _context.WorkspaceDocuments.Remove(document);
                await _context.SaveChangesAsync();

                await _auditLogService.LogEventAsync(
                    userId,
                    "DocumentPermanentlyDeleted",
                    "system",
                    null,
                    true,
                    System.Text.Json.JsonSerializer.Serialize(new { DocumentId = documentId, FileName = document.FileName }));

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error permanently deleting document {DocumentId} by user {UserId}", documentId, userId);
                return false;
            }
        }

        public async Task<WorkspaceDocument?> RestoreDocumentAsync(Guid documentId, Guid userId)
        {
            try
            {
                var document = await _context.WorkspaceDocuments
                    .FirstOrDefaultAsync(d => d.Id == documentId && d.IsDeleted);

                if (document == null || !document.CanBeEditedBy(userId))
                {
                    return null;
                }

                document.Restore();
                await _context.SaveChangesAsync();

                await _auditLogService.LogEventAsync(
                    userId,
                    "DocumentRestored",
                    "system",
                    null,
                    true,
                    System.Text.Json.JsonSerializer.Serialize(new { DocumentId = documentId, FileName = document.FileName }));

                return document;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring document {DocumentId} by user {UserId}", documentId, userId);
                return null;
            }
        }

        public async Task<bool> MoveDocumentAsync(Guid documentId, Guid? targetFolderId, Guid userId)
        {
            try
            {
                var document = await _context.WorkspaceDocuments
                    .FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted);

                if (document == null || !document.CanBeEditedBy(userId))
                {
                    return false;
                }

                // Verify target folder if specified
                if (targetFolderId.HasValue)
                {
                    var targetFolder = await _context.DocumentFolders
                        .FirstOrDefaultAsync(f => f.Id == targetFolderId.Value &&
                                                  f.WorkspaceId == document.WorkspaceId &&
                                                  !f.IsDeleted);

                    if (targetFolder == null || !targetFolder.CanBeAccessedBy(userId))
                    {
                        return false;
                    }
                }

                document.FolderId = targetFolderId;
                await _context.SaveChangesAsync();

                await _auditLogService.LogEventAsync(
                    userId,
                    "DocumentMoved",
                    "system",
                    null,
                    true,
                    System.Text.Json.JsonSerializer.Serialize(new { DocumentId = documentId, TargetFolderId = targetFolderId, FileName = document.FileName }));

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving document {DocumentId} by user {UserId}", documentId, userId);
                return false;
            }
        }

        public async Task<DocumentUploadResult> CreateDocumentVersionAsync(Guid documentId, DocumentUploadRequest request, Guid userId)
        {
            var result = new DocumentUploadResult();

            try
            {
                // Get the existing document
                var existingDocument = await _context.WorkspaceDocuments
                    .Include(d => d.Workspace)
                    .FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted);

                if (existingDocument == null || !existingDocument.CanBeEditedBy(userId))
                {
                    result.Success = false;
                    result.ErrorMessage = "Document not found or access denied";
                    return result;
                }

                // Set workspace and folder from existing document
                request.WorkspaceId = existingDocument.WorkspaceId;
                request.FolderId = existingDocument.FolderId;
                request.FileName = existingDocument.FileName;
                request.ReplaceExisting = true;

                // Use the existing upload logic with version handling
                return await UploadDocumentAsync(request, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating document version for {DocumentId} by user {UserId}", documentId, userId);
                result.Success = false;
                result.ErrorMessage = "An error occurred while creating document version";
                return result;
            }
        }

        public async Task<List<WorkspaceDocument>> GetDocumentVersionsAsync(Guid documentId, Guid userId)
        {
            try
            {
                var document = await _context.WorkspaceDocuments
                    .Include(d => d.PreviousVersions)
                    .FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted);

                if (document == null || !document.CanBeAccessedBy(userId))
                {
                    return new List<WorkspaceDocument>();
                }

                var versions = new List<WorkspaceDocument> { document };
                versions.AddRange(document.PreviousVersions.OrderByDescending(v => v.VersionNumber));

                return versions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document versions {DocumentId} by user {UserId}", documentId, userId);
                return new List<WorkspaceDocument>();
            }
        }

        public async Task<DocumentFolder?> CreateFolderAsync(DocumentFolderCreateRequest request, Guid userId)
        {
            try
            {
                var workspace = await _context.ProjectWorkspaces
                    .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId);

                if (workspace == null || !workspace.IsAccessibleBy(userId))
                {
                    return null;
                }

                var folder = new DocumentFolder
                {
                    WorkspaceId = request.WorkspaceId,
                    ParentFolderId = request.ParentFolderId,
                    FolderName = request.FolderName,
                    Description = request.Description,
                    CreatedBy = userId,
                    SortOrder = request.SortOrder
                };

                _context.DocumentFolders.Add(folder);
                await _context.SaveChangesAsync();

                await _auditLogService.LogEventAsync(
                    userId,
                    "FolderCreated",
                    "system",
                    null,
                    true,
                    System.Text.Json.JsonSerializer.Serialize(new { FolderId = folder.Id, WorkspaceId = request.WorkspaceId, FolderName = request.FolderName }));

                return folder;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating folder {FolderName} by user {UserId}", request.FolderName, userId);
                return null;
            }
        }

        public async Task<List<DocumentFolder>> GetFoldersAsync(Guid workspaceId, Guid? parentFolderId = null, Guid? userId = null, bool includeDeleted = false)
        {
            try
            {
                var query = _context.DocumentFolders
                    .Include(f => f.Creator)
                    .Where(f => f.WorkspaceId == workspaceId);

                if (parentFolderId.HasValue)
                {
                    query = query.Where(f => f.ParentFolderId == parentFolderId.Value);
                }
                else
                {
                    query = query.Where(f => f.ParentFolderId == null);
                }

                if (!includeDeleted)
                {
                    query = query.Where(f => !f.IsDeleted);
                }

                return await query.OrderBy(f => f.SortOrder).ThenBy(f => f.FolderName).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting folders for workspace {WorkspaceId}", workspaceId);
                return new List<DocumentFolder>();
            }
        }

        public async Task<DocumentFolder?> UpdateFolderAsync(Guid folderId, DocumentFolderUpdateRequest request, Guid userId)
        {
            try
            {
                var folder = await _context.DocumentFolders
                    .FirstOrDefaultAsync(f => f.Id == folderId && !f.IsDeleted);

                if (folder == null || !folder.CanBeEditedBy(userId))
                {
                    return null;
                }

                if (!string.IsNullOrWhiteSpace(request.FolderName))
                {
                    folder.FolderName = request.FolderName;
                }

                if (!string.IsNullOrWhiteSpace(request.Description))
                {
                    folder.Description = request.Description;
                }

                if (request.SortOrder.HasValue)
                {
                    folder.SortOrder = request.SortOrder.Value;
                }

                await _context.SaveChangesAsync();

                await _auditLogService.LogEventAsync(
                    userId,
                    "FolderUpdated",
                    "system",
                    null,
                    true,
                    System.Text.Json.JsonSerializer.Serialize(new { FolderId = folderId, FolderName = folder.FolderName }));

                return folder;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating folder {FolderId} by user {UserId}", folderId, userId);
                return null;
            }
        }

        public async Task<bool> DeleteFolderAsync(Guid folderId, Guid userId)
        {
            try
            {
                // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
                var folder = await _context.DocumentFolders
                    .Include(f => f.ChildFolders)
                    .Include(f => f.Documents)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync(f => f.Id == folderId && !f.IsDeleted);

                if (folder == null || !folder.CanBeDeletedBy(userId))
                {
                    return false;
                }

                folder.Delete(userId);
                await _context.SaveChangesAsync();

                await _auditLogService.LogEventAsync(
                    userId,
                    "FolderDeleted",
                    "system",
                    null,
                    true,
                    System.Text.Json.JsonSerializer.Serialize(new { FolderId = folderId, FolderName = folder.FolderName }));

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting folder {FolderId} by user {UserId}", folderId, userId);
                return false;
            }
        }

        public async Task<DocumentFolder?> RestoreFolderAsync(Guid folderId, Guid userId)
        {
            try
            {
                var folder = await _context.DocumentFolders
                    .FirstOrDefaultAsync(f => f.Id == folderId && f.IsDeleted);

                if (folder == null || !folder.CanBeEditedBy(userId))
                {
                    return null;
                }

                folder.Restore();
                await _context.SaveChangesAsync();

                await _auditLogService.LogEventAsync(
                    userId,
                    "FolderRestored",
                    "system",
                    null,
                    true,
                    System.Text.Json.JsonSerializer.Serialize(new { FolderId = folderId, FolderName = folder.FolderName }));

                return folder;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring folder {FolderId} by user {UserId}", folderId, userId);
                return null;
            }
        }

        public async Task<bool> MoveFolderAsync(Guid folderId, Guid? targetParentFolderId, Guid userId)
        {
            try
            {
                var folder = await _context.DocumentFolders
                    .FirstOrDefaultAsync(f => f.Id == folderId && !f.IsDeleted);

                if (folder == null || !folder.CanBeEditedBy(userId))
                {
                    return false;
                }

                // Verify target parent if specified
                if (targetParentFolderId.HasValue)
                {
                    var targetParent = await _context.DocumentFolders
                        .FirstOrDefaultAsync(f => f.Id == targetParentFolderId.Value &&
                                                  f.WorkspaceId == folder.WorkspaceId &&
                                                  !f.IsDeleted);

                    if (targetParent == null || !targetParent.CanBeAccessedBy(userId))
                    {
                        return false;
                    }

                    // Check for circular reference
                    if (await IsCircularReference(folderId, targetParentFolderId.Value))
                    {
                        return false;
                    }
                }

                folder.ParentFolderId = targetParentFolderId;
                await _context.SaveChangesAsync();

                await _auditLogService.LogEventAsync(
                    userId,
                    "FolderMoved",
                    "system",
                    null,
                    true,
                    System.Text.Json.JsonSerializer.Serialize(new { FolderId = folderId, TargetParentFolderId = targetParentFolderId, FolderName = folder.FolderName }));

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving folder {FolderId} by user {UserId}", folderId, userId);
                return false;
            }
        }

        public async Task<DocumentBasicSearchResult> SearchDocumentsAsync(Guid workspaceId, string searchQuery, Guid userId, DocumentSearchFilters? filters = null)
        {
            try
            {
                var workspace = await _context.ProjectWorkspaces.FindAsync(workspaceId);
                if (workspace == null || !workspace.IsAccessibleBy(userId))
                {
                    return new DocumentBasicSearchResult();
                }

                // BE-MED-002 FIX: Add search query length validation to prevent DoS
                const int MaxSearchQueryLength = 500;
                var query = searchQuery?.ToLower() ?? string.Empty;
                if (query.Length > MaxSearchQueryLength)
                {
                    _logger.LogWarning("Search query too long ({Length} chars) from user {UserId}, truncating to {MaxLength}",
                        query.Length, userId, MaxSearchQueryLength);
                    query = query[..MaxSearchQueryLength];
                }

                var result = new DocumentBasicSearchResult { SearchQuery = searchQuery ?? string.Empty };

                // Search documents
                // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
                var documentsQuery = _context.WorkspaceDocuments
                    .Include(d => d.Uploader)
                    .Include(d => d.Folder)
                    .AsSplitQuery()
                    .Where(d => d.WorkspaceId == workspaceId && !d.IsDeleted);

                if (!string.IsNullOrEmpty(query))
                {
                    documentsQuery = documentsQuery.Where(d => d.FileName.ToLower().Contains(query));
                }

                // Apply filters
                if (filters != null)
                {
                    if (!string.IsNullOrEmpty(filters.FileType))
                    {
                        documentsQuery = documentsQuery.Where(d => d.MimeType.Contains(filters.FileType));
                    }
                    if (filters.CreatedAfter.HasValue)
                    {
                        documentsQuery = documentsQuery.Where(d => d.CreatedAt >= filters.CreatedAfter.Value);
                    }
                    if (filters.CreatedBefore.HasValue)
                    {
                        documentsQuery = documentsQuery.Where(d => d.CreatedAt <= filters.CreatedBefore.Value);
                    }
                    if (filters.UploadedBy.HasValue)
                    {
                        documentsQuery = documentsQuery.Where(d => d.UploadedBy == filters.UploadedBy.Value);
                    }
                    if (filters.MinSize.HasValue)
                    {
                        documentsQuery = documentsQuery.Where(d => d.FileSize >= filters.MinSize.Value);
                    }
                    if (filters.MaxSize.HasValue)
                    {
                        documentsQuery = documentsQuery.Where(d => d.FileSize <= filters.MaxSize.Value);
                    }
                }

                result.Documents = await documentsQuery.OrderBy(d => d.FileName).ToListAsync();

                // Search folders
                var foldersQuery = _context.DocumentFolders
                    .Include(f => f.Creator)
                    .Where(f => f.WorkspaceId == workspaceId && !f.IsDeleted);

                if (!string.IsNullOrEmpty(query))
                {
                    foldersQuery = foldersQuery.Where(f => f.FolderName.ToLower().Contains(query) ||
                                                           (f.Description != null && f.Description.ToLower().Contains(query)));
                }

                result.Folders = await foldersQuery.OrderBy(f => f.FolderName).ToListAsync();
                result.TotalResults = result.Documents.Count + result.Folders.Count;

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching documents in workspace {WorkspaceId} by user {UserId}", workspaceId, userId);
                return new DocumentBasicSearchResult();
            }
        }

        public async Task<List<DocumentAccess>> GetDocumentAccessHistoryAsync(Guid documentId, Guid userId)
        {
            try
            {
                var document = await _context.WorkspaceDocuments
                    .Include(d => d.Workspace)
                    .FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted);

                if (document == null || !document.CanBeAccessedBy(userId))
                {
                    return new List<DocumentAccess>();
                }

                // Note: Single Include, no need for AsSplitQuery
                return await _context.DocumentAccesses
                    .Include(a => a.User)
                    .Where(a => a.DocumentId == documentId)
                    .OrderByDescending(a => a.AccessedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting access history for document {DocumentId} by user {UserId}", documentId, userId);
                return new List<DocumentAccess>();
            }
        }

        public async Task<DocumentStorageStats> GetStorageStatsAsync(Guid workspaceId, Guid userId)
        {
            try
            {
                var workspace = await _context.ProjectWorkspaces.FindAsync(workspaceId);
                if (workspace == null || !workspace.IsAccessibleBy(userId))
                {
                    return new DocumentStorageStats { WorkspaceId = workspaceId };
                }

                var documents = await _context.WorkspaceDocuments
                    .Where(d => d.WorkspaceId == workspaceId && !d.IsDeleted)
                    .ToListAsync();

                var folders = await _context.DocumentFolders
                    .Where(f => f.WorkspaceId == workspaceId && !f.IsDeleted)
                    .CountAsync();

                var stats = new DocumentStorageStats
                {
                    WorkspaceId = workspaceId,
                    TotalFiles = documents.Count,
                    TotalSizeBytes = documents.Sum(d => d.FileSize),
                    TotalFolders = folders,
                    LastActivity = documents.Any() ? documents.Max(d => d.CreatedAt) : DateTime.MinValue
                };

                // Calculate file type distribution
                var fileTypes = documents
                    .GroupBy(d => d.MimeType)
                    .ToDictionary(g => g.Key, g => g.Count());

                stats.FileTypeDistribution = fileTypes;

                var fileTypeSizes = documents
                    .GroupBy(d => d.MimeType)
                    .ToDictionary(g => g.Key, g => g.Sum(d => d.FileSize));

                stats.FileTypeSizes = fileTypeSizes;

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting storage stats for workspace {WorkspaceId} by user {UserId}", workspaceId, userId);
                return new DocumentStorageStats { WorkspaceId = workspaceId };
            }
        }

        public async Task<SecurityScanResult> ScanDocumentAsync(Guid documentId, Guid userId)
        {
            try
            {
                var document = await _context.WorkspaceDocuments
                    .Include(d => d.Workspace)
                    .FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted);

                if (document == null || !document.CanBeAccessedBy(userId))
                {
                    return new SecurityScanResult
                    {
                        ScanPassed = false,
                        ThreatDetected = false,
                        ScanTimestamp = DateTime.UtcNow,
                        ScanEngine = "None",
                        ThreatTypes = new List<string>()
                    };
                }

                // Re-scan the file if it exists
                // BUG-CRIT-006 FIX: Dispose fileStream properly to prevent resource leak
                await using var fileStream = await _fileStorageService.DownloadFileAsync(document.FilePath);
                if (fileStream != null)
                {
                    var scanResult = await _virusScanService.ScanFileAsync(fileStream, document.FileName, document.MimeType);

                    return new SecurityScanResult
                    {
                        ScanPassed = scanResult.IsClean,
                        ThreatDetected = !scanResult.IsClean,
                        ThreatTypes = scanResult.Threats.Select(t => t.ThreatName),
                        ScanEngine = scanResult.ScanEngine,
                        ScanTimestamp = scanResult.ScanDate
                    };
                }

                return new SecurityScanResult
                {
                    ScanPassed = document.SecurityScanPassed,
                    ThreatDetected = !document.SecurityScanPassed,
                    ScanTimestamp = DateTime.UtcNow,
                    ScanEngine = "Cached",
                    ThreatTypes = new List<string>()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning document {DocumentId} by user {UserId}", documentId, userId);
                return new SecurityScanResult
                {
                    ScanPassed = false,
                    ThreatDetected = false,
                    ScanTimestamp = DateTime.UtcNow,
                    ScanEngine = "Error",
                    ThreatTypes = new List<string>()
                };
            }
        }

        public async Task<string?> GetSecureDownloadUrlAsync(Guid documentId, Guid userId, int expirationMinutes = 60)
        {
            try
            {
                var document = await _context.WorkspaceDocuments
                    .Include(d => d.Workspace)
                    .FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted);

                if (document == null || !document.CanBeAccessedBy(userId))
                {
                    return null;
                }

                // Generate secure URL through file storage service
                var secureUrl = await _fileStorageService.GetSecureUrlAsync(
                    document.FilePath,
                    expirationMinutes,
                    FileAccessPermission.Read);

                // Record access
                if (secureUrl != null)
                {
                    document.RecordAccess(userId);
                    await _context.SaveChangesAsync();
                }

                return secureUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating secure URL for document {DocumentId} by user {UserId}", documentId, userId);
                return null;
            }
        }

        public async Task<DocumentValidationResult> ValidateUploadAsync(DocumentUploadRequest request)
        {
            var result = new DocumentValidationResult { IsValid = true };

            try
            {
                // File size validation
                if (request.FileSize > _config.MaxFileSizeBytes)
                {
                    result.Errors.Add($"File size exceeds maximum allowed size of {_config.MaxFileSizeBytes / (1024 * 1024)} MB");
                }

                if (request.FileSize <= 0)
                {
                    result.Errors.Add("File size must be greater than 0");
                }

                // File name validation
                if (string.IsNullOrWhiteSpace(request.FileName))
                {
                    result.Errors.Add("File name is required");
                }
                else if (request.FileName.Length > 500)
                {
                    result.Errors.Add("File name is too long (max 500 characters)");
                }

                // Content type validation
                if (string.IsNullOrWhiteSpace(request.ContentType))
                {
                    result.Errors.Add("Content type is required");
                }

                // File type validation
                if (!await _virusScanService.IsFileTypeAllowedAsync(request.FileName, request.ContentType))
                {
                    result.Errors.Add("File type is not allowed");
                }

                // Workspace validation
                if (request.WorkspaceId == Guid.Empty)
                {
                    result.Errors.Add("Workspace ID is required");
                }

                result.IsValid = !result.Errors.Any();
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating upload request");
                result.Errors.Add("Validation failed due to an error");
                result.IsValid = false;
                return result;
            }
        }

        public async Task<DocumentTreeResult> GetDocumentTreeAsync(Guid workspaceId, Guid userId, bool includeDeleted = false)
        {
            try
            {
                var workspace = await _context.ProjectWorkspaces.FindAsync(workspaceId);
                if (workspace == null || !workspace.IsAccessibleBy(userId))
                {
                    return new DocumentTreeResult { WorkspaceId = workspaceId };
                }

                // Get all folders and documents
                // PERFORMANCE FIX: Use AsNoTracking for read-only tree structure query
                var foldersQuery = _context.DocumentFolders
                    .AsNoTracking()
                    .Where(f => f.WorkspaceId == workspaceId);
                var documentsQuery = _context.WorkspaceDocuments
                    .AsNoTracking()
                    .Where(d => d.WorkspaceId == workspaceId);

                if (!includeDeleted)
                {
                    foldersQuery = foldersQuery.Where(f => !f.IsDeleted);
                    documentsQuery = documentsQuery.Where(d => !d.IsDeleted);
                }

                // Note: Single Include on each query, no need for AsSplitQuery
                var folders = await foldersQuery.Include(f => f.Creator).ToListAsync();
                var documents = await documentsQuery.Include(d => d.Uploader).ToListAsync();

                // Build tree structure starting with root items
                var rootNodes = new List<DocumentTreeNode>();

                // Add root folders
                var rootFolders = folders.Where(f => f.ParentFolderId == null).OrderBy(f => f.SortOrder).ThenBy(f => f.FolderName);
                foreach (var folder in rootFolders)
                {
                    var folderNode = CreateFolderNode(folder, folders, documents);
                    rootNodes.Add(folderNode);
                }

                // Add root documents (not in any folder)
                var rootDocuments = documents.Where(d => d.FolderId == null).OrderBy(d => d.FileName);
                foreach (var document in rootDocuments)
                {
                    var documentNode = CreateDocumentNode(document);
                    rootNodes.Add(documentNode);
                }

                return new DocumentTreeResult
                {
                    WorkspaceId = workspaceId,
                    RootNodes = rootNodes
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document tree for workspace {WorkspaceId} by user {UserId}", workspaceId, userId);
                return new DocumentTreeResult { WorkspaceId = workspaceId };
            }
        }

        private DocumentTreeNode CreateFolderNode(DocumentFolder folder, List<DocumentFolder> allFolders, List<WorkspaceDocument> allDocuments)
        {
            var folderNode = new DocumentTreeNode
            {
                Id = folder.Id,
                Name = folder.FolderName,
                IsFolder = true,
                CreatedAt = folder.CreatedAt,
                CreatedBy = folder.CreatedBy,
                IsDeleted = folder.IsDeleted,
                Children = new List<DocumentTreeNode>()
            };

            // Add child folders
            var childFolders = allFolders.Where(f => f.ParentFolderId == folder.Id).OrderBy(f => f.SortOrder).ThenBy(f => f.FolderName);
            foreach (var childFolder in childFolders)
            {
                var childNode = CreateFolderNode(childFolder, allFolders, allDocuments);
                folderNode.Children.Add(childNode);
            }

            // Add documents in this folder
            var folderDocuments = allDocuments.Where(d => d.FolderId == folder.Id).OrderBy(d => d.FileName);
            foreach (var document in folderDocuments)
            {
                var documentNode = CreateDocumentNode(document);
                folderNode.Children.Add(documentNode);
            }

            return folderNode;
        }

        private DocumentTreeNode CreateDocumentNode(WorkspaceDocument document)
        {
            return new DocumentTreeNode
            {
                Id = document.Id,
                Name = document.FileName,
                IsFolder = false,
                FileSize = document.FileSize,
                MimeType = document.MimeType,
                CreatedAt = document.CreatedAt,
                CreatedBy = document.UploadedBy,
                IsDeleted = document.IsDeleted,
                Children = new List<DocumentTreeNode>()
            };
        }

        private async Task<bool> IsCircularReference(Guid folderId, Guid targetParentFolderId)
        {
            // Check if the target parent is a descendant of the folder being moved
            var currentFolder = await _context.DocumentFolders.FindAsync(targetParentFolderId);
            var visited = new HashSet<Guid> { folderId }; // Prevent infinite loops

            while (currentFolder != null)
            {
                if (currentFolder.Id == folderId)
                {
                    return true; // Circular reference detected
                }

                if (visited.Contains(currentFolder.Id))
                {
                    break; // Prevent infinite loop
                }

                visited.Add(currentFolder.Id);

                if (currentFolder.ParentFolderId.HasValue)
                {
                    currentFolder = await _context.DocumentFolders.FindAsync(currentFolder.ParentFolderId.Value);
                }
                else
                {
                    break;
                }
            }

            return false;
        }
    }
}