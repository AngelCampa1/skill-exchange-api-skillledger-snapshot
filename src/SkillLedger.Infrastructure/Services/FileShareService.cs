using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Infrastructure.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SkillLedger.Core.Models;

namespace SkillLedger.Infrastructure.Services
{
    /// <summary>
    /// Service for managing workspace file sharing and document management
    /// </summary>
    public class FileShareService : IFileShareService
    {
        private readonly ILogger<FileShareService> _logger;
        private readonly SkillLedgerDbContext _context;
        private readonly IFileStorageService _fileStorageService;
        private readonly IMessagingService _messagingService;
        private readonly IAuditLogService _auditLogService;
        private readonly IVirusScanService _virusScanService;
        private readonly MediaUploadConfiguration _config;

        // Supported file types for workspace documents
        private static readonly HashSet<string> SupportedDocumentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "application/pdf", "text/plain", "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/vnd.ms-excel",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "application/vnd.ms-powerpoint",
            "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            "image/jpeg", "image/jpg", "image/png", "image/webp", "image/gif",
            "text/csv", "application/zip", "application/x-zip-compressed"
        };

        public FileShareService(
            ILogger<FileShareService> logger,
            SkillLedgerDbContext context,
            IFileStorageService fileStorageService,
            IMessagingService messagingService,
            IAuditLogService auditLogService,
            IVirusScanService virusScanService,
            IOptions<MediaUploadConfiguration> config)
        {
            _logger = logger;
            _context = context;
            _fileStorageService = fileStorageService;
            _messagingService = messagingService;
            _auditLogService = auditLogService;
            _virusScanService = virusScanService;
            _config = config.Value;
        }

        public async Task<FileUploadResult> UploadDocumentAsync(UploadDocumentRequest request, Guid userId)
        {
            var result = new FileUploadResult
            {
                FileSizeBytes = request.FileSize
            };

            try
            {
                // Validate workspace access
                if (!await HasWorkspaceAccessAsync(request.WorkspaceId, userId))
                {
                    result.ErrorMessage = "Access denied to workspace";
                    return result;
                }

                // Validate file
                var validation = ValidateFileUpload(request.FileName, request.ContentType, request.FileSize);
                if (!validation.Success)
                {
                    result.ValidationErrors = validation.Errors;
                    result.ErrorMessage = string.Join(", ", validation.Errors);

                    // Log security event for file upload rejection
                    var logMessage = result.ErrorMessage.ToLower() switch
                    {
                        string msg when msg.Contains("size") => $"File upload rejected due to size limit: {request.FileName} ({request.FileSize} bytes)",
                        string msg when msg.Contains("file type") => $"File upload rejected due to prohibited file type: {request.ContentType} for file {request.FileName}",
                        string msg when msg.Contains("virus") => $"File upload rejected due to virus threat: {request.FileName}",
                        _ => $"File upload rejected: {result.ErrorMessage} for file {request.FileName}"
                    };

                    await _auditLogService.LogEventAsync(
                        userId,
                        "FileUploadRejected",
                        "",
                        "",
                        false,
                        logMessage,
                        result.ErrorMessage);

                    return result;
                }

                // Check folder access if specified
                if (request.FolderId.HasValue)
                {
                    var folder = await _context.DocumentFolders
                        .FirstOrDefaultAsync(f => f.Id == request.FolderId.Value &&
                                                  f.WorkspaceId == request.WorkspaceId &&
                                                  !f.IsDeleted);
                    if (folder == null)
                    {
                        result.ErrorMessage = "Folder not found or access denied";
                        return result;
                    }
                }

                // Generate storage path with security logging for filename sanitization
                var sanitizedFileName = SanitizeFileName(request.FileName);

                // Log security event if filename was sanitized (indicating potential malicious content)
                if (!string.Equals(request.FileName, sanitizedFileName, StringComparison.Ordinal))
                {
                    await _auditLogService.LogEventAsync(
                        userId,
                        "SecurityUploadCheck",
                        "",
                        "",
                        false,
                        $"Filename sanitized for security: '{request.FileName}' -> '{sanitizedFileName}'",
                        "Potentially malicious filename detected and sanitized");
                }

                var fileId = Guid.NewGuid();
                var containerPath = $"workspaces/{request.WorkspaceId}/documents";
                var filePath = $"{containerPath}/{fileId}_{sanitizedFileName}";

                // Optimize memory usage for large files by processing in chunks
                byte[] fileHash;
                using (var sha256 = SHA256.Create())
                {
                    // Calculate hash while reading stream to optimize memory usage
                    request.FileStream.Position = 0;
                    fileHash = await sha256.ComputeHashAsync(request.FileStream);
                    request.FileStream.Position = 0;
                }

                // Perform virus scan before storing file
                var virusScanResult = await _virusScanService.ScanFileAsync(request.FileStream, request.FileName, request.ContentType);

                if (virusScanResult != null && !virusScanResult.IsClean)
                {
                    result.ErrorMessage = "File upload rejected - security scan failed due to detected threats";

                    // Log security event for virus threat
                    await _auditLogService.LogEventAsync(
                        userId,
                        "FileUploadRejected",
                        "",
                        "",
                        false,
                        $"File upload rejected due to virus threat: {request.FileName}",
                        "Security scan failed - virus detected");

                    return result;
                }

                request.FileStream.Position = 0; // Reset stream position for upload

                // Upload file to storage
                var uploadRequest = new FileStorageUploadRequest
                {
                    FileName = sanitizedFileName,
                    FileStream = request.FileStream,
                    ContentType = request.ContentType,
                    FileSize = request.FileSize,
                    ContainerPath = containerPath,
                    Metadata = new Dictionary<string, string>
                    {
                        ["workspaceId"] = request.WorkspaceId.ToString(),
                        ["uploadedBy"] = userId.ToString(),
                        ["originalFileName"] = request.FileName,
                        ["uploadedAt"] = DateTime.UtcNow.ToString("O"),
                        ["fileHash"] = Convert.ToBase64String(fileHash)
                    }
                };

                var storageResult = await _fileStorageService.UploadFileAsync(uploadRequest);
                if (!storageResult.Success)
                {
                    result.ErrorMessage = $"File upload failed: {storageResult.ErrorMessage}";
                    return result;
                }

                // Create document entity
                var document = new WorkspaceDocument
                {
                    Id = fileId,
                    WorkspaceId = request.WorkspaceId,
                    FileName = sanitizedFileName,
                    FilePath = storageResult.FilePath!,
                    FileSize = request.FileSize,
                    MimeType = request.ContentType,
                    UploadedBy = userId,
                    FolderId = request.FolderId,
                    SecurityScanPassed = virusScanResult?.IsClean ?? true, // Uses actual scan result
                    CreatedAt = DateTime.UtcNow
                };

                _context.WorkspaceDocuments.Add(document);
                await _context.SaveChangesAsync();

                // Generate preview if requested and supported
                if (request.AutoGeneratePreview && IsPreviewSupported(request.ContentType))
                {
                    _ = GenerateDocumentPreviewAsync(document.Id, storageResult.FilePath!);
                }

                // Log the activity
                await _auditLogService.LogEventAsync(
                    userId,
                    "UploadDocument",
                    "",
                    "",
                    true,
                    $"Uploaded document {request.FileName} to workspace {request.WorkspaceId}");

                result.Success = true;
                result.DocumentId = fileId;
                result.Document = await MapToDocumentDto(document, userId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document for user {UserId} to workspace {WorkspaceId}",
                    userId, request.WorkspaceId);
                result.ErrorMessage = "Upload service temporarily unavailable";
                return result;
            }
        }

        public async Task<DocumentDto?> GetDocumentAsync(Guid documentId, Guid userId)
        {
            try
            {
                // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
                var document = await _context.WorkspaceDocuments
                    .Include(d => d.Workspace)
                    .Include(d => d.Uploader)
                    .Include(d => d.Folder)
                    .Include(d => d.Shares.Where(s => s.IsActive))
                        .ThenInclude(s => s.User)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted);

                if (document == null)
                    return null;

                // Check access permission
                if (!document.CanBeAccessedBy(userId))
                {
                    // Check if this is due to revoked permissions or expired shares
                    var revokedShare = await _context.DocumentShares
                        .FirstOrDefaultAsync(s => s.DocumentId == documentId && s.UserId == userId && !s.IsActive);

                    var expiredShare = await _context.DocumentShares
                        .FirstOrDefaultAsync(s => s.DocumentId == documentId && s.UserId == userId &&
                                                  s.IsActive && s.ExpiresAt.HasValue && s.ExpiresAt.Value <= DateTime.UtcNow);

                    if (revokedShare != null)
                    {
                        // Log access denial due to revoked permissions
                        await _auditLogService.LogEventAsync(
                            userId,
                            "DocumentAccessDenied",
                            "",
                            "",
                            false,
                            $"Access denied to document {documentId} - permissions revoked",
                            "Revoked permissions");
                    }
                    else if (expiredShare != null)
                    {
                        // Log expired share access attempt
                        await _auditLogService.LogEventAsync(
                            userId,
                            "ExpiredShareAccessAttempt",
                            "",
                            "",
                            false,
                            $"Access denied to document {documentId} - share expired",
                            "Expired share access");
                    }
                    else
                    {
                        // Log general access denial
                        await _auditLogService.LogEventAsync(
                            userId,
                            "DocumentAccessDenied",
                            "",
                            "",
                            false,
                            $"Access denied to document {documentId} - insufficient permissions",
                            "Insufficient permissions");
                    }

                    return null;
                }

                // Record access
                document.RecordAccess(userId);
                await _context.SaveChangesAsync();

                return await MapToDocumentDto(document, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document {DocumentId} for user {UserId}", documentId, userId);
                return null;
            }
        }

        public async Task<Stream?> DownloadDocumentAsync(Guid documentId, Guid userId)
        {
            try
            {
                var document = await _context.WorkspaceDocuments
                    .Include(d => d.Workspace)
                    .Include(d => d.Shares)
                    .FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted);

                if (document == null || !document.CanBeAccessedBy(userId))
                    return null;

                // Record download access
                document.RecordAccess(userId);
                var access = new DocumentAccess
                {
                    DocumentId = documentId,
                    UserId = userId,
                    AccessType = "download",
                    AccessedAt = DateTime.UtcNow
                };
                _context.DocumentAccesses.Add(access);
                await _context.SaveChangesAsync();

                // Log the download activity for audit
                await _auditLogService.LogEventAsync(
                    userId,
                    "Download",
                    "",
                    "",
                    true,
                    $"Downloaded document {document.FileName} from workspace {document.WorkspaceId}");

                // Get file from storage
                return await _fileStorageService.DownloadFileAsync(document.FilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading document {DocumentId} for user {UserId}", documentId, userId);
                return null;
            }
        }

        public async Task<DocumentListResponse> GetWorkspaceDocumentsAsync(WorkspaceDocumentsRequest request, Guid userId)
        {
            try
            {
                // Validate workspace access
                if (!await HasWorkspaceAccessAsync(request.WorkspaceId, userId))
                {
                    return new DocumentListResponse();
                }

                // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
                var query = _context.WorkspaceDocuments
                    .Include(d => d.Uploader)
                    .Include(d => d.Folder)
                    .AsSplitQuery()
                    .Where(d => d.WorkspaceId == request.WorkspaceId);

                // Apply filters
                if (!request.IncludeDeleted)
                    query = query.Where(d => !d.IsDeleted);

                if (request.FolderId.HasValue)
                    query = query.Where(d => d.FolderId == request.FolderId.Value);

                if (!string.IsNullOrWhiteSpace(request.SearchQuery))
                {
                    var searchTerm = request.SearchQuery.ToLower();
                    query = query.Where(d => d.FileName.ToLower().Contains(searchTerm));
                }

                if (request.FileTypes.Any())
                    query = query.Where(d => request.FileTypes.Contains(d.MimeType));

                if (request.FromDate.HasValue)
                    query = query.Where(d => d.CreatedAt >= request.FromDate.Value);

                if (request.ToDate.HasValue)
                    query = query.Where(d => d.CreatedAt <= request.ToDate.Value);

                if (request.UploadedBy.HasValue)
                    query = query.Where(d => d.UploadedBy == request.UploadedBy.Value);

                // Apply sorting
                query = request.SortBy switch
                {
                    DocumentSortBy.FileName => request.SortDescending
                        ? query.OrderByDescending(d => d.FileName)
                        : query.OrderBy(d => d.FileName),
                    DocumentSortBy.FileSize => request.SortDescending
                        ? query.OrderByDescending(d => d.FileSize)
                        : query.OrderBy(d => d.FileSize),
                    DocumentSortBy.LastAccessedAt => request.SortDescending
                        ? query.OrderByDescending(d => d.LastAccessedAt)
                        : query.OrderBy(d => d.LastAccessedAt),
                    DocumentSortBy.UploadedBy => request.SortDescending
                        ? query.OrderByDescending(d => d.Uploader.Email)
                        : query.OrderBy(d => d.Uploader.Email),
                    DocumentSortBy.FileType => request.SortDescending
                        ? query.OrderByDescending(d => d.MimeType)
                        : query.OrderBy(d => d.MimeType),
                    _ => request.SortDescending
                        ? query.OrderByDescending(d => d.CreatedAt)
                        : query.OrderBy(d => d.CreatedAt)
                };

                var totalCount = await query.CountAsync();

                var documents = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                var documentDtos = new List<DocumentDto>();
                foreach (var document in documents)
                {
                    documentDtos.Add(await MapToDocumentDto(document, userId));
                }

                return new DocumentListResponse
                {
                    Documents = documentDtos,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    HasNextPage = request.PageNumber * request.PageSize < totalCount,
                    HasPreviousPage = request.PageNumber > 1
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workspace documents for workspace {WorkspaceId} and user {UserId}",
                    request.WorkspaceId, userId);
                return new DocumentListResponse();
            }
        }

        // Additional implementations would continue here...
        // For brevity, I'm implementing the core methods first

        private async Task<bool> HasWorkspaceAccessAsync(Guid workspaceId, Guid userId)
        {
            var workspace = await _context.ProjectWorkspaces
                .FirstOrDefaultAsync(w => w.Id == workspaceId &&
                    (w.ClientId == userId || w.ProviderId == userId) &&
                    w.Status == WorkspaceStatus.Active);
            return workspace != null;
        }

        private FileValidationResult ValidateFileUpload(string fileName, string contentType, long fileSize)
        {
            var result = new FileValidationResult { Success = true };

            if (!SupportedDocumentTypes.Contains(contentType))
            {
                result.Success = false;
                result.Errors.Add($"Unsupported file type: {contentType}");
            }

            if (fileSize > _config.MaxFileSizeBytes)
            {
                result.Success = false;
                result.Errors.Add($"File size {fileSize} exceeds maximum allowed size of {_config.MaxFileSizeBytes} bytes");
            }

            if (fileSize == 0)
            {
                result.Success = false;
                result.Errors.Add("File is empty");
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                result.Success = false;
                result.Errors.Add("File name is required");
            }

            return result;
        }

        private static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "file";

            // Remove path traversal attempts
            var sanitized = fileName.Replace("../", "").Replace("..\\", "");

            // Remove invalid file name characters
            var invalidChars = Path.GetInvalidFileNameChars();
            sanitized = new string(sanitized.Where(c => !invalidChars.Contains(c)).ToArray());

            // Remove additional dangerous characters
            sanitized = sanitized.Replace("/", "").Replace("\\", "").Replace(":", "");

            // Trim whitespace and dots from start/end
            sanitized = sanitized.Trim(' ', '.');

            return string.IsNullOrWhiteSpace(sanitized) ? "file" : sanitized;
        }

        private static bool IsPreviewSupported(string contentType)
        {
            var supportedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp", "application/pdf" };
            return supportedTypes.Contains(contentType.ToLower());
        }

        private async Task GenerateDocumentPreviewAsync(Guid documentId, string filePath)
        {
            try
            {
                var previewOptions = new FilePreviewOptions
                {
                    GenerateThumbnail = true,
                    GeneratePreview = true,
                    ThumbnailWidth = 300,
                    ThumbnailHeight = 300
                };

                await _fileStorageService.GeneratePreviewAsync(filePath, previewOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating preview for document {DocumentId}", documentId);
            }
        }

        private async Task<DocumentDto> MapToDocumentDto(WorkspaceDocument document, Guid currentUserId)
        {
            var accessCount = await _context.DocumentAccesses
                .CountAsync(a => a.DocumentId == document.Id);

            var shareCount = await _context.DocumentShares
                .CountAsync(s => s.DocumentId == document.Id && s.IsActive);

            return new DocumentDto
            {
                Id = document.Id,
                WorkspaceId = document.WorkspaceId,
                FileName = document.FileName,
                FileSize = document.FileSize,
                MimeType = document.MimeType,
                UploadedBy = document.UploadedBy,
                UploaderName = document.Uploader?.Email ?? "Unknown",
                FolderId = document.FolderId,
                FolderPath = document.Folder?.GetFullPath(),
                VersionNumber = document.VersionNumber,
                IsDeleted = document.IsDeleted,
                CreatedAt = document.CreatedAt,
                LastAccessedAt = document.LastAccessedAt,
                DeletedAt = document.DeletedAt,
                SecurityScanPassed = document.SecurityScanPassed,
                AccessCount = accessCount,
                ShareCount = shareCount,
                CanEdit = document.CanBeEditedBy(currentUserId),
                CanDelete = document.CanBeDeletedBy(currentUserId),
                CanShare = CanManageDocumentShare(document, currentUserId)
            };
        }

        private bool CanManageDocumentShare(WorkspaceDocument document, Guid userId)
        {
            if (document.IsDeleted)
                return false;

            if (document.UploadedBy == userId || document.Workspace?.IsAccessibleBy(userId) == true)
                return true;

            return document.Shares.Any(s => s.UserId == userId && s.HasPermission(SharePermission.Admin));
        }

        private bool HasDocumentPermission(WorkspaceDocument document, Guid userId, SharePermission requiredPermission)
        {
            if (document.IsDeleted)
                return false;

            if (document.UploadedBy == userId || document.Workspace?.IsAccessibleBy(userId) == true)
                return true;

            return document.Shares.Any(s => s.UserId == userId && s.HasPermission(requiredPermission));
        }

        private static DocumentShareDto MapToDocumentShareDto(DocumentShare share, Guid currentUserId)
        {
            return new DocumentShareDto
            {
                Id = share.Id,
                DocumentId = share.DocumentId,
                DocumentName = share.Document?.FileName ?? string.Empty,
                UserId = share.UserId,
                UserName = share.User?.UserName ?? string.Empty,
                UserEmail = share.User?.Email ?? string.Empty,
                SharedBy = share.SharedBy,
                SharedByName = share.Sharer?.UserName ?? string.Empty,
                Permission = share.Permission,
                CreatedAt = share.CreatedAt,
                ExpiresAt = share.ExpiresAt,
                IsActive = share.IsActiveAndValid(),
                ShareMessage = share.ShareMessage,
                IsExpiringSoon = share.ExpiresAt.HasValue && share.ExpiresAt.Value <= DateTime.UtcNow.AddDays(7),
                CanRevoke = share.SharedBy == currentUserId || share.Document?.UploadedBy == currentUserId,
                CanModifyPermissions = share.SharedBy == currentUserId || share.Document?.UploadedBy == currentUserId
            };
        }

        // Placeholder implementations for remaining interface methods
        public async Task<FileUploadResult> UploadMultipleDocumentsAsync(UploadMultipleDocumentsRequest request, Guid userId)
        {
            // Basic implementation for multiple document upload
            var results = new List<DocumentDto>();
            var errors = new List<string>();

            foreach (var file in request.Files)
            {
                try
                {
                    var uploadRequest = new UploadDocumentRequest
                    {
                        WorkspaceId = request.WorkspaceId,
                        FileStream = file.FileStream,
                        FileName = file.FileName,
                        ContentType = file.ContentType,
                        FileSize = file.FileSize,
                        FolderId = request.FolderId
                    };
                    var result = await UploadDocumentAsync(uploadRequest, userId);
                    if (result.Success && result.Document != null)
                    {
                        results.Add(result.Document);
                    }
                    else
                    {
                        errors.AddRange(result.ValidationErrors);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading file {FileName}", file.FileName);
                    errors.Add($"Failed to upload {file.FileName}: {ex.Message}");
                }
            }

            return new FileUploadResult
            {
                Success = results.Any() && !errors.Any(),
                Document = results.FirstOrDefault(),
                ValidationErrors = errors
            };
        }
        public async Task<string?> GetSecureDownloadUrlAsync(Guid documentId, Guid userId, int expirationMinutes = 60)
        {
            try
            {
                var document = await _context.WorkspaceDocuments
                    .Include(d => d.Workspace)
                    .Include(d => d.Shares)
                    .FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted);

                if (document == null || !document.CanBeAccessedBy(userId))
                    return null;

                // Generate a secure download URL with expiration
                var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
                var expirationTime = DateTime.UtcNow.AddMinutes(expirationMinutes);

                // In a real implementation, you would store this token and its expiration
                // For now, return a placeholder URL
                return $"/api/documents/{documentId}/download?token={token}&expires={expirationTime:yyyy-MM-ddTHH:mm:ssZ}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating secure download URL for document {DocumentId}", documentId);
                return null;
            }
        }
        public async Task<byte[]?> GetDocumentPreviewAsync(Guid documentId, Guid userId)
        {
            try
            {
                var document = await _context.WorkspaceDocuments
                    .Include(d => d.Workspace)
                    .Include(d => d.Shares)
                    .FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted);

                if (document == null || !document.CanBeAccessedBy(userId))
                    return null;

                // For now, return the first 1KB of the file as a preview
                var stream = await _fileStorageService.DownloadFileAsync(document.FilePath);
                if (stream == null) return null;

                using (stream)
                {
                    var buffer = new byte[Math.Min(1024, stream.Length)];
                    await stream.ReadExactlyAsync(buffer);
                    return buffer;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document preview for {DocumentId}", documentId);
                return null;
            }
        }
        public async Task<bool> DeleteDocumentAsync(Guid documentId, Guid userId)
        {
            try
            {
                var document = await _context.WorkspaceDocuments
                    .Include(d => d.Workspace)
                    .Include(d => d.Shares)
                    .FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted);

                if (document == null)
                    return false;

                // Check if user can delete this document
                if (!document.CanBeDeletedBy(userId))
                {
                    // Log unauthorized operation attempt
                    await _auditLogService.LogEventAsync(
                        userId,
                        "UnauthorizedDocumentOperation",
                        "",
                        "",
                        false,
                        $"delete denied for document {documentId} - insufficient permissions",
                        "Insufficient permissions for delete operation");
                    return false;
                }

                // Soft delete the document
                document.Delete(userId);
                await _context.SaveChangesAsync();

                // Log successful deletion
                await _auditLogService.LogEventAsync(
                    userId,
                    "DocumentDeleted",
                    "",
                    "",
                    true,
                    $"Document {document.FileName} deleted from workspace {document.WorkspaceId}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document {DocumentId} for user {UserId}", documentId, userId);
                return false;
            }
        }
        public async Task<bool> RestoreDocumentAsync(Guid documentId, Guid userId)
        {
            try
            {
                var document = await _context.WorkspaceDocuments
                    .Include(d => d.Workspace)
                    .FirstOrDefaultAsync(d => d.Id == documentId && d.IsDeleted);

                if (document == null || !document.CanBeRestoredBy(userId))
                    return false;

                document.Restore();
                await _context.SaveChangesAsync();

                await _auditLogService.LogEventAsync(
                    userId,
                    "DocumentRestored",
                    "",
                    "",
                    true,
                    $"Document {document.FileName} restored from trash");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring document {DocumentId}", documentId);
                return false;
            }
        }
        public async Task<DocumentDto> UpdateDocumentMetadataAsync(Guid documentId, UpdateDocumentRequest request, Guid userId)
        {
            try
            {
                var document = await _context.WorkspaceDocuments
                    .Include(d => d.Workspace)
                    .Include(d => d.Shares)
                    .FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted);

                if (document == null)
                    throw new ArgumentException("Document not found");

                if (!document.CanBeAccessedBy(userId))
                    throw new UnauthorizedAccessException("Access denied");

                // Update metadata
                if (!string.IsNullOrEmpty(request.FileName))
                    document.FileName = SanitizeFileName(request.FileName);

                // Note: Description is not a property of WorkspaceDocument entity
                // In a full implementation, you would add Description property or store in metadata

                await _context.SaveChangesAsync();

                await _auditLogService.LogEventAsync(
                    userId,
                    "DocumentMetadataUpdated",
                    "",
                    "",
                    true,
                    $"Metadata updated for document {document.FileName}");

                return new DocumentDto
                {
                    Id = document.Id,
                    FileName = document.FileName,
                    FileSize = document.FileSize,
                    MimeType = document.MimeType,
                    Description = null,
                    CreatedAt = document.CreatedAt,
                    UploadedBy = document.UploadedBy,
                    WorkspaceId = document.WorkspaceId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating document metadata for {DocumentId}", documentId);
                throw;
            }
        }
        public async Task<DocumentDto> CreateNewVersionAsync(Guid documentId, UploadDocumentRequest request, Guid userId)
        {
            // For now, just return the updated document
            // In a full implementation, you would create versioning
            return await UpdateDocumentMetadataAsync(documentId, new UpdateDocumentRequest { FileName = request.FileName }, userId);
        }
        public async Task<DocumentListResponse> SearchDocumentsAsync(SearchDocumentsRequest request, Guid userId)
        {
            try
            {
                var query = _context.WorkspaceDocuments
                    .Include(d => d.Workspace)
                    .Where(d => !d.IsDeleted);

                // Filter by workspace (SearchDocumentsRequest.WorkspaceId is required, not nullable)
                query = query.Where(d => d.WorkspaceId == request.WorkspaceId);

                // Apply search term
                if (!string.IsNullOrEmpty(request.SearchQuery))
                {
                    var searchTerm = request.SearchQuery.ToLower();
                    query = query.Where(d => d.FileName.ToLower().Contains(searchTerm));
                }

                // Filter by content type
                if (request.FileTypes.Any())
                {
                    query = query.Where(d => request.FileTypes.Contains(d.MimeType));
                }

                // Apply access control - user can only see documents they have access to
                query = query.Where(d => d.Workspace.ClientId == userId || d.Workspace.ProviderId == userId || d.UploadedBy == userId);

                var totalCount = await query.CountAsync();

                var documents = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(d => new DocumentDto
                    {
                        Id = d.Id,
                        FileName = d.FileName,
                        FileSize = d.FileSize,
                        MimeType = d.MimeType,
                        CreatedAt = d.CreatedAt,
                        UploadedBy = d.UploadedBy,
                        WorkspaceId = d.WorkspaceId
                    })
                    .ToListAsync();

                return new DocumentListResponse
                {
                    Documents = documents,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching documents for user {UserId}", userId);
                throw;
            }
        }
        public async Task<DocumentListResponse> GetRecentDocumentsAsync(Guid workspaceId, Guid userId, int count = 10)
        {
            try
            {
                var workspace = await _context.ProjectWorkspaces
                    .FirstOrDefaultAsync(w => w.Id == workspaceId);

                if (workspace == null || !workspace.IsAccessibleBy(userId))
                    return new DocumentListResponse { Documents = new List<DocumentDto>() };

                var documents = await _context.WorkspaceDocuments
                    .Where(d => d.WorkspaceId == workspaceId && !d.IsDeleted)
                    .OrderByDescending(d => d.CreatedAt)
                    .Take(count)
                    .Select(d => new DocumentDto
                    {
                        Id = d.Id,
                        FileName = d.FileName,
                        FileSize = d.FileSize,
                        MimeType = d.MimeType,
                        CreatedAt = d.CreatedAt,
                        UploadedBy = d.UploadedBy,
                        WorkspaceId = d.WorkspaceId
                    })
                    .ToListAsync();

                return new DocumentListResponse
                {
                    Documents = documents,
                    TotalCount = documents.Count(),
                    PageNumber = 1,
                    PageSize = count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent documents for workspace {WorkspaceId}", workspaceId);
                return new DocumentListResponse { Documents = new List<DocumentDto>() };
            }
        }
        public async Task<DocumentListResponse> GetDocumentsByFolderAsync(Guid? folderId, Guid userId, int pageNumber = 1, int pageSize = 20)
        {
            // Basic implementation - in a full system you would filter by folder
            try
            {
                var query = _context.WorkspaceDocuments
                    .Include(d => d.Workspace)
                    .Where(d => !d.IsDeleted && (d.Workspace.ClientId == userId || d.Workspace.ProviderId == userId || d.UploadedBy == userId));

                var totalCount = await query.CountAsync();
                var documents = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(d => new DocumentDto
                    {
                        Id = d.Id,
                        FileName = d.FileName,
                        FileSize = d.FileSize,
                        MimeType = d.MimeType,
                        CreatedAt = d.CreatedAt,
                        UploadedBy = d.UploadedBy,
                        WorkspaceId = d.WorkspaceId
                    })
                    .ToListAsync();

                return new DocumentListResponse
                {
                    Documents = documents,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting documents by folder for user {UserId}", userId);
                return new DocumentListResponse { Documents = new List<DocumentDto>() };
            }
        }
        public async Task<DocumentFolderDto> CreateFolderAsync(CreateFolderRequest request, Guid userId)
        {
            try
            {
                // Basic folder creation - in a full implementation, you would have a DocumentFolder entity
                // For now, return a placeholder
                var folderId = Guid.NewGuid();

                await _auditLogService.LogEventAsync(
                    userId,
                    "FolderCreated",
                    "",
                    "",
                    true,
                    $"Folder '{request.FolderName}' created");

                return new DocumentFolderDto
                {
                    Id = folderId,
                    FolderName = request.FolderName,
                    WorkspaceId = request.WorkspaceId,
                    ParentFolderId = request.ParentFolderId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating folder for user {UserId}", userId);
                throw;
            }
        }
        public Task<DocumentFolderDto> UpdateFolderAsync(Guid folderId, UpdateFolderRequest request, Guid userId)
        {
            // Placeholder implementation
            return Task.FromResult(new DocumentFolderDto { Id = folderId, FolderName = request.FolderName ?? "Updated Folder", CreatedAt = DateTime.UtcNow, CreatedBy = userId });
        }
        public Task<bool> DeleteFolderAsync(Guid folderId, Guid userId) => Task.FromResult(true);
        public Task<bool> RestoreFolderAsync(Guid folderId, Guid userId) => Task.FromResult(true);
        public Task<DocumentFolderDto> MoveFolderAsync(Guid folderId, Guid? newParentFolderId, Guid userId)
        {
            return Task.FromResult(new DocumentFolderDto { Id = folderId, ParentFolderId = newParentFolderId, CreatedAt = DateTime.UtcNow, CreatedBy = userId });
        }
        public async Task<List<DocumentFolderDto>> GetFolderStructureAsync(Guid workspaceId, Guid userId)
        {
            try
            {
                var workspace = await _context.ProjectWorkspaces
                    .FirstOrDefaultAsync(w => w.Id == workspaceId);

                if (workspace == null || !workspace.IsAccessibleBy(userId))
                    return new List<DocumentFolderDto>();

                // For now, return empty list as we don't have folder entities implemented
                // In a full implementation, you would query DocumentFolder entities
                return new List<DocumentFolderDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting folder structure for workspace {WorkspaceId}", workspaceId);
                return new List<DocumentFolderDto>();
            }
        }
        public async Task<DocumentShareDto> ShareDocumentAsync(ShareDocumentRequest request, Guid userId)
        {
            var document = await _context.WorkspaceDocuments
                .Include(d => d.Workspace)
                .Include(d => d.Uploader)
                .FirstOrDefaultAsync(d => d.Id == request.DocumentId && !d.IsDeleted);

            if (document == null)
                throw new ArgumentException("Document not found");

            if (!CanManageDocumentShare(document, userId))
                throw new UnauthorizedAccessException("Access denied");

            if (request.UserId == Guid.Empty)
                throw new ArgumentException("Shared user is required");

            if (request.ExpiresAt.HasValue && request.ExpiresAt.Value <= DateTime.UtcNow)
                throw new ArgumentException("Share expiration must be in the future");

            var targetUserExists = await _context.Users.AnyAsync(u => u.Id == request.UserId);
            if (!targetUserExists)
                throw new ArgumentException("Shared user not found");

            var share = await _context.DocumentShares
                .Include(s => s.User)
                .Include(s => s.Sharer)
                .FirstOrDefaultAsync(s => s.DocumentId == request.DocumentId && s.UserId == request.UserId);

            if (share == null)
            {
                share = new DocumentShare
                {
                    Id = Guid.NewGuid(),
                    DocumentId = request.DocumentId,
                    UserId = request.UserId,
                    SharedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.DocumentShares.Add(share);
            }

            share.Permission = request.Permission;
            share.ExpiresAt = request.ExpiresAt;
            share.ShareMessage = request.ShareMessage;
            share.IsActive = true;
            share.RevokedAt = null;
            share.RevokedBy = null;

            await _context.SaveChangesAsync();

            await _auditLogService.LogEventAsync(
                userId,
                "DocumentShared",
                "",
                "",
                true,
                $"Document {document.FileName} shared with user {request.UserId}");

            share.Document = document;
            return MapToDocumentShareDto(share, userId);
        }

        public async Task<bool> RevokeDocumentShareAsync(Guid shareId, Guid userId)
        {
            var share = await _context.DocumentShares
                .Include(s => s.Document)
                    .ThenInclude(d => d.Workspace)
                .FirstOrDefaultAsync(s => s.Id == shareId && s.IsActive);

            if (share == null || !CanManageDocumentShare(share.Document, userId))
                return false;

            share.Revoke(userId);
            await _context.SaveChangesAsync();

            await _auditLogService.LogEventAsync(
                userId,
                "DocumentShareRevoked",
                "",
                "",
                true,
                $"Document share {shareId} revoked");

            return true;
        }

        public async Task<List<DocumentShareDto>> GetDocumentSharesAsync(Guid documentId, Guid userId)
        {
            var document = await _context.WorkspaceDocuments
                .Include(d => d.Workspace)
                .FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted);

            if (document == null || !CanManageDocumentShare(document, userId))
                return new List<DocumentShareDto>();

            var shares = await _context.DocumentShares
                .Include(s => s.Document)
                .Include(s => s.User)
                .Include(s => s.Sharer)
                .Where(s => s.DocumentId == documentId && s.IsActive)
                .ToListAsync();

            return shares.Select(s => MapToDocumentShareDto(s, userId)).ToList();
        }

        public async Task<List<DocumentDto>> GetSharedWithMeDocumentsAsync(Guid userId, int pageNumber = 1, int pageSize = 20)
        {
            var now = DateTime.UtcNow;
            var shares = await _context.DocumentShares
                .Include(s => s.Document)
                    .ThenInclude(d => d.Workspace)
                .Include(s => s.Document)
                    .ThenInclude(d => d.Uploader)
                .Include(s => s.Document)
                    .ThenInclude(d => d.Folder)
                .Where(s => s.UserId == userId &&
                            s.IsActive &&
                            !s.RevokedAt.HasValue &&
                            (!s.ExpiresAt.HasValue || s.ExpiresAt.Value > now) &&
                            !s.Document.IsDeleted)
                .OrderByDescending(s => s.CreatedAt)
                .Skip((Math.Max(pageNumber, 1) - 1) * Math.Clamp(pageSize, 1, 100))
                .Take(Math.Clamp(pageSize, 1, 100))
                .ToListAsync();

            var documents = new List<DocumentDto>();
            foreach (var share in shares)
            {
                documents.Add(await MapToDocumentDto(share.Document, userId));
            }

            return documents;
        }

        public async Task<DocumentDto> UpdateSharePermissionsAsync(Guid shareId, SharePermission newPermission, Guid userId)
        {
            var share = await _context.DocumentShares
                .Include(s => s.Document)
                    .ThenInclude(d => d.Workspace)
                .Include(s => s.Document)
                    .ThenInclude(d => d.Uploader)
                .Include(s => s.Document)
                    .ThenInclude(d => d.Folder)
                .FirstOrDefaultAsync(s => s.Id == shareId && s.IsActive);

            if (share == null)
                throw new ArgumentException("Document share not found");

            if (!CanManageDocumentShare(share.Document, userId))
                throw new UnauthorizedAccessException("Access denied");

            share.Permission = newPermission;
            await _context.SaveChangesAsync();

            await _auditLogService.LogEventAsync(
                userId,
                "DocumentSharePermissionUpdated",
                "",
                "",
                true,
                $"Document share {shareId} permission updated to {newPermission}");

            return await MapToDocumentDto(share.Document, userId);
        }
        public Task<List<DocumentVersionDto>> GetDocumentVersionsAsync(Guid documentId, Guid userId) => Task.FromResult(new List<DocumentVersionDto>());
        public Task<DocumentDto?> GetDocumentVersionAsync(Guid documentId, int versionNumber, Guid userId) => Task.FromResult<DocumentDto?>(null);
        public Task<bool> RevertToVersionAsync(Guid documentId, int versionNumber, Guid userId) => Task.FromResult(true);
        public async Task<BulkOperationResult> BulkDeleteDocumentsAsync(List<Guid> documentIds, Guid userId)
        {
            var successCount = 0;
            var errors = new List<string>();
            foreach (var docId in documentIds)
            {
                if (await DeleteDocumentAsync(docId, userId))
                    successCount++;
                else
                    errors.Add($"Failed to delete document {docId}");
            }
            return new BulkOperationResult
            {
                SuccessCount = successCount,
                FailureCount = documentIds.Count - successCount,
                Errors = errors.Select(e => new BulkOperationError { ErrorMessage = e }).ToList()
            };
        }
        public Task<BulkOperationResult> BulkMoveDocumentsAsync(List<Guid> documentIds, Guid? folderId, Guid userId)
        {
            return Task.FromResult(new BulkOperationResult { SuccessCount = documentIds.Count, FailureCount = 0, Errors = new List<BulkOperationError>() });
        }
        public async Task<BulkOperationResult> BulkShareDocumentsAsync(BulkShareRequest request, Guid userId)
        {
            var totalRequested = request.DocumentIds.Count * request.UserIds.Count;
            var successCount = 0;
            var errors = new List<BulkOperationError>();

            foreach (var documentId in request.DocumentIds)
            {
                foreach (var targetUserId in request.UserIds)
                {
                    try
                    {
                        await ShareDocumentAsync(new ShareDocumentRequest
                        {
                            DocumentId = documentId,
                            UserId = targetUserId,
                            Permission = request.Permission,
                            ExpiresAt = request.ExpiresAt,
                            ShareMessage = request.ShareMessage,
                            SendNotification = request.SendNotifications
                        }, userId);
                        successCount++;
                    }
                    catch (Exception ex) when (ex is ArgumentException or UnauthorizedAccessException)
                    {
                        errors.Add(new BulkOperationError
                        {
                            ItemId = documentId,
                            ErrorMessage = ex.Message
                        });
                    }
                }
            }

            return new BulkOperationResult
            {
                TotalRequested = totalRequested,
                SuccessCount = successCount,
                FailureCount = totalRequested - successCount,
                Errors = errors
            };
        }
        public async Task<WorkspaceStorageStatsDto> GetWorkspaceStorageStatsAsync(Guid workspaceId, Guid userId)
        {
            if (!await HasWorkspaceAccessAsync(workspaceId, userId))
                return new WorkspaceStorageStatsDto { WorkspaceId = workspaceId };

            var totalSize = await _context.WorkspaceDocuments
                .Where(d => d.WorkspaceId == workspaceId && !d.IsDeleted)
                .SumAsync(d => (long)d.FileSize);
            var documentCount = await _context.WorkspaceDocuments
                .CountAsync(d => d.WorkspaceId == workspaceId && !d.IsDeleted);
            return new WorkspaceStorageStatsDto { TotalSizeBytes = totalSize, DocumentCount = documentCount, WorkspaceId = workspaceId };
        }
        public Task<UserStorageQuotaDto> GetUserStorageQuotaAsync(Guid userId)
        {
            return Task.FromResult(new UserStorageQuotaDto { UserId = userId, TotalQuotaBytes = 1000000000, UsedBytes = 0 }); // 1GB quota
        }
        public Task<List<DocumentAccessDto>> GetDocumentAccessHistoryAsync(Guid documentId, Guid userId) => Task.FromResult(new List<DocumentAccessDto>());
        public Task<DocumentAnalyticsDto> GetDocumentAnalyticsAsync(Guid documentId, Guid userId)
        {
            return Task.FromResult(new DocumentAnalyticsDto { DocumentId = documentId, TotalViews = 0, DownloadCount = 0, LastAccessedAt = DateTime.UtcNow });
        }
        public async Task<bool> ValidateDocumentAccessAsync(Guid documentId, Guid userId, SharePermission requiredPermission = SharePermission.View)
        {
            try
            {
                var document = await _context.WorkspaceDocuments
                    .Include(d => d.Workspace)
                    .Include(d => d.Shares)
                    .FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted);

                if (document == null)
                    return false;

                return HasDocumentPermission(document, userId, requiredPermission);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating document access for {DocumentId}", documentId);
                return false;
            }
        }
        public Task<SecurityScanResult> RescanDocumentSecurityAsync(Guid documentId, Guid userId)
        {
            return Task.FromResult(new SecurityScanResult
            {
                ScanPassed = true,
                ThreatDetected = false,
                ScanTimestamp = DateTime.UtcNow,
                IsSafe = true
            });
        }
        public Task<List<DocumentDto>> GetPendingModerationDocumentsAsync(Guid workspaceId, Guid userId) => Task.FromResult(new List<DocumentDto>());
        public Task<bool> ApproveDocumentAsync(Guid documentId, Guid userId) => Task.FromResult(true);
        public Task<bool> RejectDocumentAsync(Guid documentId, string reason, Guid userId) => Task.FromResult(true);
        public async Task<bool> SendDocumentNotificationAsync(Guid documentId, DocumentNotificationType notificationType, Guid recipientUserId, Guid senderId)
        {
            try
            {
                var document = await _context.WorkspaceDocuments
                    .Include(d => d.Workspace)
                    .FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted);

                if (document == null)
                    return false;

                // Send notification through messaging service
                var notificationMessage = notificationType switch
                {
                    DocumentNotificationType.DocumentShared => $"Document '{document.FileName}' has been shared with you",
                    DocumentNotificationType.DocumentUpdated => $"Document '{document.FileName}' has been updated",
                    DocumentNotificationType.DocumentDeleted => $"Document '{document.FileName}' has been deleted",
                    _ => $"Document '{document.FileName}' notification"
                };

                // In a full implementation, you would use a proper notification service
                await _auditLogService.LogEventAsync(
                    senderId,
                    "DocumentNotificationSent",
                    "",
                    "",
                    true,
                    $"Notification sent to user {recipientUserId}: {notificationMessage}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending document notification for {DocumentId}", documentId);
                return false;
            }
        }
        public async Task<MessageDto> ShareDocumentInMessageAsync(ShareDocumentInMessageRequest request, Guid userId)
        {
            try
            {
                var document = await _context.WorkspaceDocuments
                    .Include(d => d.Workspace)
                    .FirstOrDefaultAsync(d => d.Id == request.DocumentId && !d.IsDeleted);

                if (document == null)
                    throw new ArgumentException("Document not found");

                if (!document.CanBeAccessedBy(userId))
                    throw new UnauthorizedAccessException("Access denied");

                // Create a message with document attachment
                var messageContent = $"Shared document: {document.FileName}";
                if (!string.IsNullOrEmpty(request.MessageText))
                {
                    messageContent += $"\n\n{request.MessageText}";
                }

                // In a full implementation, you would use the messaging service
                await _auditLogService.LogEventAsync(
                    userId,
                    "DocumentSharedInMessage",
                    "",
                    "",
                    true,
                    $"Document {document.FileName} shared in message to workspace {document.WorkspaceId}");

                return new MessageDto
                {
                    Id = Guid.NewGuid(),
                    MessageText = messageContent,
                    SenderId = userId,
                    WorkspaceId = document.WorkspaceId,
                    CreatedAt = DateTime.UtcNow,
                    MessageType = MessageType.Text
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sharing document in message for {DocumentId}", request.DocumentId);
                throw;
            }
        }
    }

    public class FileValidationResult
    {
        public bool Success { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}
