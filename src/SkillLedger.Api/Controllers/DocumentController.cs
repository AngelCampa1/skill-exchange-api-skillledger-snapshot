using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Interfaces;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Models;
using SkillLedger.Core.Enums;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace SkillLedger.Api.Controllers
{
    /// <summary>
    /// Controller for document and file management operations
    /// Provides secure file upload, download, and management functionality
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DocumentController : ControllerBase
    {
        private readonly IDocumentService _documentService;
        private readonly IBackupService _backupService;
        private readonly ICdnService _cdnService;
        private readonly IDocumentSearchService _documentSearchService;
        private readonly IFilePreviewService _filePreviewService;
        private readonly IDocumentSharingService _documentSharingService;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<DocumentController> _logger;

        public DocumentController(
            IDocumentService documentService,
            IBackupService backupService,
            ICdnService cdnService,
            IDocumentSearchService documentSearchService,
            IFilePreviewService filePreviewService,
            IDocumentSharingService documentSharingService,
            IAuditLogService auditLogService,
            ILogger<DocumentController> logger)
        {
            _documentService = documentService;
            _backupService = backupService;
            _cdnService = cdnService;
            _documentSearchService = documentSearchService;
            _filePreviewService = filePreviewService;
            _documentSharingService = documentSharingService;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        /// <summary>
        /// Uploads a document to a workspace
        /// </summary>
        /// <param name="workspaceId">Workspace ID</param>
        /// <param name="file">File to upload</param>
        /// <param name="folderId">Optional folder ID</param>
        /// <param name="description">Optional description</param>
        /// <param name="replaceExisting">Whether to replace existing file with same name</param>
        /// <returns>Upload result</returns>
        [HttpPost("upload")]
        [EnableRateLimiting("FileUploadPolicy")]
        [RequestSizeLimit(52428800)] // 50MB limit
        [RequestFormLimits(MultipartBodyLengthLimit = 52428800)]
        public async Task<ActionResult<DocumentUploadResponse>> UploadDocument(
            [FromForm] Guid workspaceId,
            [FromForm] IFormFile file,
            [FromForm] Guid? folderId = null,
            [FromForm] string? description = null,
            [FromForm] bool replaceExisting = false)
        {
            try
            {
                var userId = GetCurrentUserId();

                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { error = "No file provided" });
                }

                // Validate file size
                if (file.Length > 52428800) // 50MB
                {
                    return BadRequest(new { error = "File size exceeds maximum limit of 50MB" });
                }

                // SECURITY FIX: Wrap stream in using to ensure proper disposal and prevent resource leaks
                using var fileStream = file.OpenReadStream();
                var request = new DocumentUploadRequest
                {
                    WorkspaceId = workspaceId,
                    FolderId = folderId,
                    FileName = file.FileName,
                    FileStream = fileStream,
                    ContentType = file.ContentType,
                    FileSize = file.Length,
                    Description = description,
                    ReplaceExisting = replaceExisting
                };

                var result = await _documentService.UploadDocumentAsync(request, userId);

                if (!result.Success)
                {
                    return BadRequest(new { error = result.ErrorMessage });
                }

                // Ensure document is not null after successful upload
                if (result.Document == null)
                {
                    _logger.LogError("Document upload succeeded but Document object is null for user {UserId}", userId);
                    return StatusCode(500, new { error = "Upload succeeded but document data is missing" });
                }

                var response = new DocumentUploadResponse
                {
                    Success = true,
                    DocumentId = result.Document.Id,
                    FileName = result.Document.FileName,
                    FileSize = result.Document.FileSize,
                    MimeType = result.Document.MimeType,
                    UploadedAt = result.Document.CreatedAt,
                    SecurityScanPassed = result.Document.SecurityScanPassed,
                    SecurityScanDetails = result.SecurityScan
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document to workspace {WorkspaceId} by user {UserId}",
                    workspaceId, GetCurrentUserId());

                return StatusCode(500, new { error = "An error occurred while uploading the document" });
            }
        }

        /// <summary>
        /// Downloads a document by ID
        /// </summary>
        /// <param name="documentId">Document ID</param>
        /// <returns>File stream</returns>
        [HttpGet("{documentId}/download")]
        [EnableRateLimiting("FileDownloadPolicy")]
        public async Task<IActionResult> DownloadDocument(Guid documentId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _documentService.DownloadDocumentAsync(documentId, userId);

                if (!result.Success)
                {
                    // BUG-CRIT-006 FIX: Dispose stream if operation failed but stream was created
                    result.FileStream?.Dispose();

                    if (result.ErrorMessage == "Document not found")
                        return NotFound(new { error = result.ErrorMessage });
                    if (result.ErrorMessage == "Access denied")
                        return Forbid();

                    return BadRequest(new { error = result.ErrorMessage });
                }

                var document = result.Document!;
                var fileStream = result.FileStream!;

                // BUG-SEC-007 FIX: Block downloads of files that failed security scan
                if (!document.SecurityScanPassed)
                {
                    fileStream.Dispose();
                    _logger.LogWarning("Blocked download of document {DocumentId} that failed security scan by user {UserId}",
                        documentId, userId);
                    return BadRequest(new { error = "This file cannot be downloaded because it failed security scanning. Please contact support if you believe this is an error." });
                }

                return File(fileStream, document.MimeType, document.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading document {DocumentId} by user {UserId}",
                    documentId, GetCurrentUserId());

                return StatusCode(500, new { error = "An error occurred while downloading the document" });
            }
        }

        /// <summary>
        /// Gets document metadata
        /// </summary>
        /// <param name="documentId">Document ID</param>
        /// <returns>Document metadata</returns>
        [HttpGet("{documentId}")]
        public async Task<ActionResult<DocumentResponse>> GetDocument(Guid documentId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var document = await _documentService.GetDocumentAsync(documentId, userId);

                if (document == null)
                {
                    return NotFound(new { error = "Document not found or access denied" });
                }

                var response = new DocumentResponse
                {
                    Id = document.Id,
                    WorkspaceId = document.WorkspaceId,
                    FolderId = document.FolderId,
                    FileName = document.FileName,
                    FileSize = document.FileSize,
                    MimeType = document.MimeType,
                    VersionNumber = document.VersionNumber,
                    UploadedBy = document.UploadedBy,
                    CreatedAt = document.CreatedAt,
                    LastAccessedAt = document.LastAccessedAt,
                    SecurityScanPassed = document.SecurityScanPassed
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document {DocumentId} by user {UserId}",
                    documentId, GetCurrentUserId());

                return StatusCode(500, new { error = "An error occurred while retrieving the document" });
            }
        }

        /// <summary>
        /// Gets documents in a workspace or folder
        /// </summary>
        /// <param name="workspaceId">Workspace ID</param>
        /// <param name="folderId">Optional folder ID</param>
        /// <param name="includeDeleted">Include soft-deleted documents</param>
        /// <returns>List of documents</returns>
        [HttpGet("workspace/{workspaceId}")]
        public async Task<ActionResult<DocumentListResponse>> GetDocuments(
            Guid workspaceId,
            [FromQuery] Guid? folderId = null,
            [FromQuery] bool includeDeleted = false)
        {
            try
            {
                var userId = GetCurrentUserId();
                var documents = await _documentService.GetDocumentsAsync(workspaceId, folderId, userId, includeDeleted);

                var response = new DocumentListResponse
                {
                    WorkspaceId = workspaceId,
                    FolderId = folderId,
                    Documents = documents.Select(d => new DocumentResponse
                    {
                        Id = d.Id,
                        WorkspaceId = d.WorkspaceId,
                        FolderId = d.FolderId,
                        FileName = d.FileName,
                        FileSize = d.FileSize,
                        MimeType = d.MimeType,
                        VersionNumber = d.VersionNumber,
                        UploadedBy = d.UploadedBy,
                        CreatedAt = d.CreatedAt,
                        LastAccessedAt = d.LastAccessedAt,
                        SecurityScanPassed = d.SecurityScanPassed,
                        IsDeleted = d.IsDeleted
                    }).ToList()
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting documents for workspace {WorkspaceId} by user {UserId}",
                    workspaceId, GetCurrentUserId());

                return StatusCode(500, new { error = "An error occurred while retrieving documents" });
            }
        }

        /// <summary>
        /// Updates document metadata
        /// </summary>
        /// <param name="documentId">Document ID</param>
        /// <param name="request">Update request</param>
        /// <returns>Updated document</returns>
        [HttpPut("{documentId}")]
        public async Task<ActionResult<DocumentResponse>> UpdateDocument(
            Guid documentId,
            [FromBody] DocumentUpdateRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var document = await _documentService.UpdateDocumentAsync(documentId, request, userId);

                if (document == null)
                {
                    return NotFound(new { error = "Document not found or access denied" });
                }

                var response = new DocumentResponse
                {
                    Id = document.Id,
                    WorkspaceId = document.WorkspaceId,
                    FolderId = document.FolderId,
                    FileName = document.FileName,
                    FileSize = document.FileSize,
                    MimeType = document.MimeType,
                    VersionNumber = document.VersionNumber,
                    UploadedBy = document.UploadedBy,
                    CreatedAt = document.CreatedAt,
                    LastAccessedAt = document.LastAccessedAt,
                    SecurityScanPassed = document.SecurityScanPassed
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating document {DocumentId} by user {UserId}",
                    documentId, GetCurrentUserId());

                return StatusCode(500, new { error = "An error occurred while updating the document" });
            }
        }

        /// <summary>
        /// Soft deletes a document
        /// </summary>
        /// <param name="documentId">Document ID</param>
        /// <returns>Success result</returns>
        [HttpDelete("{documentId}")]
        public async Task<ActionResult> DeleteDocument(Guid documentId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var success = await _documentService.DeleteDocumentAsync(documentId, userId);

                if (!success)
                {
                    return NotFound(new { error = "Document not found or access denied" });
                }

                return Ok(new { success = true, message = "Document deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document {DocumentId} by user {UserId}",
                    documentId, GetCurrentUserId());

                return StatusCode(500, new { error = "An error occurred while deleting the document" });
            }
        }

        /// <summary>
        /// Restores a soft-deleted document
        /// </summary>
        /// <param name="documentId">Document ID</param>
        /// <returns>Restored document</returns>
        [HttpPost("{documentId}/restore")]
        public async Task<ActionResult<DocumentResponse>> RestoreDocument(Guid documentId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var document = await _documentService.RestoreDocumentAsync(documentId, userId);

                if (document == null)
                {
                    return NotFound(new { error = "Document not found or access denied" });
                }

                var response = new DocumentResponse
                {
                    Id = document.Id,
                    WorkspaceId = document.WorkspaceId,
                    FolderId = document.FolderId,
                    FileName = document.FileName,
                    FileSize = document.FileSize,
                    MimeType = document.MimeType,
                    VersionNumber = document.VersionNumber,
                    UploadedBy = document.UploadedBy,
                    CreatedAt = document.CreatedAt,
                    LastAccessedAt = document.LastAccessedAt,
                    SecurityScanPassed = document.SecurityScanPassed
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring document {DocumentId} by user {UserId}",
                    documentId, GetCurrentUserId());

                return StatusCode(500, new { error = "An error occurred while restoring the document" });
            }
        }

        /// <summary>
        /// Moves a document to a different folder
        /// </summary>
        /// <param name="documentId">Document ID</param>
        /// <param name="request">Move request</param>
        /// <returns>Success result</returns>
        [HttpPost("{documentId}/move")]
        public async Task<ActionResult> MoveDocument(
            Guid documentId,
            [FromBody] DocumentMoveRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var success = await _documentService.MoveDocumentAsync(documentId, request.TargetFolderId, userId);

                if (!success)
                {
                    return NotFound(new { error = "Document not found or access denied" });
                }

                return Ok(new { success = true, message = "Document moved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving document {DocumentId} by user {UserId}",
                    documentId, GetCurrentUserId());

                return StatusCode(500, new { error = "An error occurred while moving the document" });
            }
        }

        /// <summary>
        /// Creates a new folder
        /// </summary>
        /// <param name="request">Folder creation request</param>
        /// <returns>Created folder</returns>
        [HttpPost("folders")]
        public async Task<ActionResult<DocumentFolderResponse>> CreateFolder(
            [FromBody] DocumentFolderCreateRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var folder = await _documentService.CreateFolderAsync(request, userId);

                if (folder == null)
                {
                    return BadRequest(new { error = "Failed to create folder or access denied" });
                }

                var response = new DocumentFolderResponse
                {
                    Id = folder.Id,
                    WorkspaceId = folder.WorkspaceId,
                    ParentFolderId = folder.ParentFolderId,
                    FolderName = folder.FolderName,
                    Description = folder.Description,
                    CreatedBy = folder.CreatedBy,
                    CreatedAt = folder.CreatedAt,
                    SortOrder = folder.SortOrder
                };

                return CreatedAtAction(nameof(GetFolder), new { folderId = folder.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating folder {FolderName} by user {UserId}",
                    request.FolderName, GetCurrentUserId());

                return StatusCode(500, new { error = "An error occurred while creating the folder" });
            }
        }

        /// <summary>
        /// Gets folders in a workspace
        /// </summary>
        /// <param name="workspaceId">Workspace ID</param>
        /// <param name="parentFolderId">Parent folder ID</param>
        /// <param name="includeDeleted">Include soft-deleted folders</param>
        /// <returns>List of folders</returns>
        [HttpGet("folders/workspace/{workspaceId}")]
        public async Task<ActionResult<DocumentFolderListResponse>> GetFolders(
            Guid workspaceId,
            [FromQuery] Guid? parentFolderId = null,
            [FromQuery] bool includeDeleted = false)
        {
            try
            {
                var userId = GetCurrentUserId();
                var folders = await _documentService.GetFoldersAsync(workspaceId, parentFolderId, userId, includeDeleted);

                var response = new DocumentFolderListResponse
                {
                    WorkspaceId = workspaceId,
                    ParentFolderId = parentFolderId,
                    Folders = folders.Select(f => new DocumentFolderResponse
                    {
                        Id = f.Id,
                        WorkspaceId = f.WorkspaceId,
                        ParentFolderId = f.ParentFolderId,
                        FolderName = f.FolderName,
                        Description = f.Description,
                        CreatedBy = f.CreatedBy,
                        CreatedAt = f.CreatedAt,
                        SortOrder = f.SortOrder,
                        IsDeleted = f.IsDeleted
                    }).ToList()
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting folders for workspace {WorkspaceId} by user {UserId}",
                    workspaceId, GetCurrentUserId());

                return StatusCode(500, new { error = "An error occurred while retrieving folders" });
            }
        }

        /// <summary>
        /// Gets a specific folder
        /// </summary>
        /// <param name="folderId">Folder ID</param>
        /// <returns>Folder details</returns>
        [HttpGet("folders/{folderId}")]
        public async Task<ActionResult<DocumentFolderResponse>> GetFolder(Guid folderId)
        {
            try
            {
                // This would need to be implemented in the service
                return NotFound(new { error = "Method not implemented" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting folder {FolderId} by user {UserId}",
                    folderId, GetCurrentUserId());

                return StatusCode(500, new { error = "An error occurred while retrieving the folder" });
            }
        }

        /// <summary>
        /// Gets storage statistics for a workspace
        /// </summary>
        /// <param name="workspaceId">Workspace ID</param>
        /// <returns>Storage statistics</returns>
        [HttpGet("workspace/{workspaceId}/stats")]
        public async Task<ActionResult<DocumentStorageStatsResponse>> GetStorageStats(Guid workspaceId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var stats = await _documentService.GetStorageStatsAsync(workspaceId, userId);

                var response = new DocumentStorageStatsResponse
                {
                    WorkspaceId = stats.WorkspaceId,
                    TotalFiles = stats.TotalFiles,
                    TotalSizeBytes = stats.TotalSizeBytes,
                    TotalSizeMB = Math.Round(stats.TotalSizeBytes / (1024.0 * 1024.0), 2),
                    TotalFolders = stats.TotalFolders,
                    FileTypeDistribution = stats.FileTypeDistribution,
                    LastActivity = stats.LastActivity
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting storage stats for workspace {WorkspaceId} by user {UserId}",
                    workspaceId, GetCurrentUserId());

                return StatusCode(500, new { error = "An error occurred while retrieving storage statistics" });
            }
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }

        /// <summary>
        /// Generate a preview for a document
        /// </summary>
        [HttpGet("{documentId}/preview")]
        [ProducesResponseType(typeof(FilePreviewResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [EnableRateLimiting("DocumentPreviewPolicy")]
        public async Task<ActionResult<FilePreviewResult>> GetDocumentPreview(Guid documentId)
        {
            try
            {
                // First check if we have a cached preview
                var cachedPreview = await _filePreviewService.GetCachedPreviewAsync(documentId);
                if (cachedPreview != null && cachedPreview.IsGenerated)
                {
                    return Ok(cachedPreview);
                }

                // Get the document through the document service
                var document = await _documentService.GetDocumentAsync(documentId, GetCurrentUserId());

                if (document == null)
                {
                    return NotFound();
                }

                // Generate preview from file
                var filePath = Path.Combine("uploads", document.FilePath);
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound("Document file not found");
                }

                using var fileStream = System.IO.File.OpenRead(filePath);
                var preview = await _filePreviewService.GeneratePreviewAsync(fileStream, document.FileName, document.MimeType);

                return Ok(preview);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating preview for document {DocumentId}", documentId);
                return StatusCode(500, "An error occurred while generating the preview");
            }
        }
    }

    // Response DTOs
    public class DocumentUploadResponse
    {
        public bool Success { get; set; }
        public Guid DocumentId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string MimeType { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
        public bool SecurityScanPassed { get; set; }
        public SecurityScanResult? SecurityScanDetails { get; set; }
    }

    public class DocumentResponse
    {
        public Guid Id { get; set; }
        public Guid WorkspaceId { get; set; }
        public Guid? FolderId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string MimeType { get; set; } = string.Empty;
        public int VersionNumber { get; set; }
        public Guid UploadedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastAccessedAt { get; set; }
        public bool SecurityScanPassed { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class DocumentListResponse
    {
        public Guid WorkspaceId { get; set; }
        public Guid? FolderId { get; set; }
        public List<DocumentResponse> Documents { get; set; } = new();
    }

    public class DocumentMoveRequest
    {
        public Guid? TargetFolderId { get; set; }
    }

    public class DocumentFolderResponse
    {
        public Guid Id { get; set; }
        public Guid WorkspaceId { get; set; }
        public Guid? ParentFolderId { get; set; }
        public string FolderName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public int SortOrder { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class DocumentFolderListResponse
    {
        public Guid WorkspaceId { get; set; }
        public Guid? ParentFolderId { get; set; }
        public List<DocumentFolderResponse> Folders { get; set; } = new();
    }

    public class DocumentStorageStatsResponse
    {
        public Guid WorkspaceId { get; set; }
        public int TotalFiles { get; set; }
        public long TotalSizeBytes { get; set; }
        public double TotalSizeMB { get; set; }
        public int TotalFolders { get; set; }
        public Dictionary<string, int> FileTypeDistribution { get; set; } = new();
        public DateTime LastActivity { get; set; }
    }

    public class BackupScheduleRequest
    {
        public BackupFrequency Frequency { get; set; } = BackupFrequency.Daily;
        public int RetentionDays { get; set; } = 90;
        public int MaxBackupsPerDocument { get; set; } = 10;
        public bool CompressBackups { get; set; } = true;
        public bool VerifyIntegrity { get; set; } = true;
    }
}
