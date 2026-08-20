using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using System.Security.Claims;

namespace SkillLedger.Api.Controllers
{
    /// <summary>
    /// API controller for workspace file sharing and document management operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FileShareController : ControllerBase
    {
        private readonly IFileShareService _fileShareService;
        private readonly ILogger<FileShareController> _logger;

        public FileShareController(
            IFileShareService fileShareService,
            ILogger<FileShareController> logger)
        {
            _fileShareService = fileShareService;
            _logger = logger;
        }

        /// <summary>
        /// Uploads a document to a workspace
        /// </summary>
        [HttpPost("upload")]
        [RequestSizeLimit(50 * 1024 * 1024)] // 50MB limit
        public async Task<ActionResult<FileUploadResult>> UploadDocument([FromForm] UploadDocumentApiRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                    return Unauthorized("Invalid user session");

                // Convert IFormFile to stream-based request
                // SECURITY FIX: Wrap stream in using to ensure proper disposal and prevent resource leaks
                using var fileStream = request.File.OpenReadStream();
                var uploadRequest = new UploadDocumentRequest
                {
                    WorkspaceId = request.WorkspaceId,
                    FileName = request.File.FileName,
                    FileStream = fileStream,
                    ContentType = request.File.ContentType,
                    FileSize = request.File.Length,
                    FolderId = request.FolderId,
                    Description = request.Description,
                    Tags = request.Tags,
                    IsPrivate = request.IsPrivate,
                    AutoGeneratePreview = request.AutoGeneratePreview
                };

                var result = await _fileShareService.UploadDocumentAsync(uploadRequest, userId.Value);

                if (result.Success)
                    return Ok(result);
                else
                    return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document for user {UserId}", GetCurrentUserId());
                return StatusCode(500, new { message = "An error occurred while uploading the document" });
            }
        }

        /// <summary>
        /// Uploads multiple documents to a workspace
        /// </summary>
        [HttpPost("upload/multiple")]
        [RequestSizeLimit(200 * 1024 * 1024)] // 200MB limit for multiple files
        public async Task<ActionResult<List<FileUploadResult>>> UploadMultipleDocuments([FromForm] UploadMultipleDocumentsApiRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                    return Unauthorized("Invalid user session");

                // BUG-RES-017 FIX: Robust stream lifecycle management for multiple files
                // Use a list to track opened streams for cleanup
                var fileStreams = new List<Stream>();
                try
                {
                    var fileItems = new List<FileUploadItem>();
                    foreach (var file in request.Files)
                    {
                        Stream? stream = null;
                        try
                        {
                            stream = file.OpenReadStream();
                            fileStreams.Add(stream);
                            fileItems.Add(new FileUploadItem
                            {
                                FileName = file.FileName,
                                FileStream = stream,
                                ContentType = file.ContentType,
                                FileSize = file.Length
                            });
                        }
                        catch (Exception streamEx)
                        {
                            // BUG-RES-017 FIX: If stream opening fails, dispose it if partially created
                            stream?.Dispose();
                            _logger.LogError(streamEx, "Failed to open stream for file {FileName}", file.FileName);
                            throw new InvalidOperationException($"Failed to open file '{file.FileName}' for upload", streamEx);
                        }
                    }

                    var uploadRequest = new UploadMultipleDocumentsRequest
                    {
                        WorkspaceId = request.WorkspaceId,
                        Files = fileItems,
                        FolderId = request.FolderId,
                        Description = request.Description,
                        IsPrivate = request.IsPrivate
                    };

                    var result = await _fileShareService.UploadMultipleDocumentsAsync(uploadRequest, userId.Value);
                    return Ok(result);
                }
                finally
                {
                    // BUG-RES-017 FIX: Dispose all streams with individual error handling
                    foreach (var stream in fileStreams)
                    {
                        try
                        {
                            stream?.Dispose();
                        }
                        catch (Exception disposeEx)
                        {
                            // Log but don't throw - disposal errors shouldn't mask the original error
                            _logger.LogWarning(disposeEx, "Error disposing file stream during cleanup");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading multiple documents for user {UserId}", GetCurrentUserId());
                return StatusCode(500, new { message = "An error occurred while uploading the documents" });
            }
        }

        /// <summary>
        /// Gets a document by ID
        /// </summary>
        [HttpGet("{documentId:guid}")]
        public async Task<ActionResult<DocumentDto>> GetDocument(Guid documentId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                    return Unauthorized("Invalid user session");

                var document = await _fileShareService.GetDocumentAsync(documentId, userId.Value);

                if (document == null)
                    return NotFound("Document not found or access denied");

                return Ok(document);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document {DocumentId} for user {UserId}",
                    documentId, GetCurrentUserId());
                return StatusCode(500, new { message = "An error occurred while retrieving the document" });
            }
        }

        /// <summary>
        /// Downloads a document
        /// </summary>
        [HttpGet("{documentId:guid}/download")]
        public async Task<ActionResult> DownloadDocument(Guid documentId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                    return Unauthorized("Invalid user session");

                // First get document metadata
                var document = await _fileShareService.GetDocumentAsync(documentId, userId.Value);
                if (document == null)
                    return NotFound("Document not found or access denied");

                // Get file stream
                var fileStream = await _fileShareService.DownloadDocumentAsync(documentId, userId.Value);
                if (fileStream == null)
                    return NotFound("File not found");

                return File(fileStream, document.MimeType, document.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading document {DocumentId} for user {UserId}",
                    documentId, GetCurrentUserId());
                return StatusCode(500, new { message = "An error occurred while downloading the document" });
            }
        }

        /// <summary>
        /// Gets a secure download URL for a document
        /// </summary>
        [HttpGet("{documentId:guid}/secure-url")]
        public async Task<ActionResult<SecureUrlResponse>> GetSecureDownloadUrl(Guid documentId, [FromQuery] int expirationMinutes = 60)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                    return Unauthorized("Invalid user session");

                var secureUrl = await _fileShareService.GetSecureDownloadUrlAsync(documentId, userId.Value, expirationMinutes);

                if (secureUrl == null)
                    return NotFound("Document not found or access denied");

                return Ok(new SecureUrlResponse
                {
                    Url = secureUrl,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating secure URL for document {DocumentId} for user {UserId}",
                    documentId, GetCurrentUserId());
                return StatusCode(500, new { message = "An error occurred while generating the secure URL" });
            }
        }

        /// <summary>
        /// Gets documents in a workspace
        /// </summary>
        [HttpGet("workspace/{workspaceId:guid}")]
        public async Task<ActionResult<DocumentListResponse>> GetWorkspaceDocuments(
            Guid workspaceId,
            [FromQuery] Guid? folderId = null,
            [FromQuery] string? searchQuery = null,
            [FromQuery] string[]? fileTypes = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] Guid? uploadedBy = null,
            [FromQuery] bool includeDeleted = false,
            [FromQuery] DocumentSortBy sortBy = DocumentSortBy.CreatedAt,
            [FromQuery] bool sortDescending = true,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                    return Unauthorized("Invalid user session");

                var request = new WorkspaceDocumentsRequest
                {
                    WorkspaceId = workspaceId,
                    FolderId = folderId,
                    SearchQuery = searchQuery,
                    FileTypes = fileTypes?.ToList() ?? new List<string>(),
                    FromDate = fromDate,
                    ToDate = toDate,
                    UploadedBy = uploadedBy,
                    IncludeDeleted = includeDeleted,
                    SortBy = sortBy,
                    SortDescending = sortDescending,
                    PageNumber = pageNumber,
                    PageSize = Math.Min(pageSize, 100) // Limit page size
                };

                var result = await _fileShareService.GetWorkspaceDocumentsAsync(request, userId.Value);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workspace documents for workspace {WorkspaceId} and user {UserId}",
                    workspaceId, GetCurrentUserId());
                return StatusCode(500, new { message = "An error occurred while retrieving workspace documents" });
            }
        }

        /// <summary>
        /// Searches documents across workspaces
        /// </summary>
        [HttpGet("search")]
        [EnableRateLimiting("DocumentSearchPolicy")]
        public async Task<ActionResult<DocumentListResponse>> SearchDocuments(
            [FromQuery] Guid workspaceId,
            [FromQuery] string searchQuery,
            [FromQuery] string[]? fileTypes = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] bool searchInContent = false,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                    return Unauthorized("Invalid user session");

                if (string.IsNullOrWhiteSpace(searchQuery))
                    return BadRequest("Search query is required");

                var request = new SearchDocumentsRequest
                {
                    WorkspaceId = workspaceId,
                    SearchQuery = searchQuery,
                    FileTypes = fileTypes?.ToList() ?? new List<string>(),
                    FromDate = fromDate,
                    ToDate = toDate,
                    SearchInContent = searchInContent,
                    PageNumber = pageNumber,
                    PageSize = Math.Min(pageSize, 100)
                };

                var result = await _fileShareService.SearchDocumentsAsync(request, userId.Value);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching documents for user {UserId}", GetCurrentUserId());
                return StatusCode(500, new { message = "An error occurred while searching documents" });
            }
        }

        /// <summary>
        /// Deletes a document
        /// </summary>
        [HttpDelete("{documentId:guid}")]
        public async Task<ActionResult> DeleteDocument(Guid documentId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                    return Unauthorized("Invalid user session");

                var result = await _fileShareService.DeleteDocumentAsync(documentId, userId.Value);

                if (result)
                    return NoContent();
                else
                    return NotFound("Document not found or cannot be deleted");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document {DocumentId} for user {UserId}",
                    documentId, GetCurrentUserId());
                return StatusCode(500, new { message = "An error occurred while deleting the document" });
            }
        }

        /// <summary>
        /// Updates document metadata
        /// </summary>
        [HttpPut("{documentId:guid}")]
        public async Task<ActionResult<DocumentDto>> UpdateDocumentMetadata(Guid documentId, [FromBody] UpdateDocumentRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                    return Unauthorized("Invalid user session");

                var result = await _fileShareService.UpdateDocumentMetadataAsync(documentId, request, userId.Value);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating document {DocumentId} for user {UserId}",
                    documentId, GetCurrentUserId());
                return StatusCode(500, new { message = "An error occurred while updating the document" });
            }
        }

        /// <summary>
        /// Creates a folder in a workspace
        /// </summary>
        [HttpPost("folders")]
        public async Task<ActionResult<DocumentFolderDto>> CreateFolder([FromBody] CreateFolderRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                    return Unauthorized("Invalid user session");

                var result = await _fileShareService.CreateFolderAsync(request, userId.Value);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating folder for user {UserId}", GetCurrentUserId());
                return StatusCode(500, new { message = "An error occurred while creating the folder" });
            }
        }

        /// <summary>
        /// Gets folder structure for a workspace
        /// </summary>
        [HttpGet("workspace/{workspaceId:guid}/folders")]
        public async Task<ActionResult<List<DocumentFolderDto>>> GetFolderStructure(Guid workspaceId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                    return Unauthorized("Invalid user session");

                var result = await _fileShareService.GetFolderStructureAsync(workspaceId, userId.Value);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting folder structure for workspace {WorkspaceId} and user {UserId}",
                    workspaceId, GetCurrentUserId());
                return StatusCode(500, new { message = "An error occurred while retrieving folder structure" });
            }
        }

        /// <summary>
        /// Shares a document with another user
        /// </summary>
        [HttpPost("{documentId:guid}/share")]
        public async Task<ActionResult<DocumentShareDto>> ShareDocument(Guid documentId, [FromBody] ShareDocumentRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                    return Unauthorized("Invalid user session");

                request.DocumentId = documentId;
                var result = await _fileShareService.ShareDocumentAsync(request, userId.Value);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sharing document {DocumentId} for user {UserId}",
                    documentId, GetCurrentUserId());
                return StatusCode(500, new { message = "An error occurred while sharing the document" });
            }
        }

        /// <summary>
        /// Gets documents shared with the current user
        /// </summary>
        [HttpGet("shared-with-me")]
        public async Task<ActionResult<List<DocumentDto>>> GetSharedWithMeDocuments(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                    return Unauthorized("Invalid user session");

                var result = await _fileShareService.GetSharedWithMeDocumentsAsync(
                    userId.Value, pageNumber, Math.Min(pageSize, 100));
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shared documents for user {UserId}", GetCurrentUserId());
                return StatusCode(500, new { message = "An error occurred while retrieving shared documents" });
            }
        }

        /// <summary>
        /// Gets workspace storage statistics
        /// </summary>
        [HttpGet("workspace/{workspaceId:guid}/storage-stats")]
        public async Task<ActionResult<WorkspaceStorageStatsDto>> GetWorkspaceStorageStats(Guid workspaceId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                    return Unauthorized("Invalid user session");

                var result = await _fileShareService.GetWorkspaceStorageStatsAsync(workspaceId, userId.Value);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting storage stats for workspace {WorkspaceId} and user {UserId}",
                    workspaceId, GetCurrentUserId());
                return StatusCode(500, new { message = "An error occurred while retrieving storage statistics" });
            }
        }

        private Guid? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdClaim, out var userId))
                return userId;
            return null;
        }
    }

    // API-specific request DTOs
    public class UploadDocumentApiRequest
    {
        public Guid WorkspaceId { get; set; }
        public IFormFile File { get; set; } = null!;
        public Guid? FolderId { get; set; }
        public string? Description { get; set; }
        public string? Tags { get; set; }
        public bool IsPrivate { get; set; } = false;
        public bool AutoGeneratePreview { get; set; } = true;
    }

    public class UploadMultipleDocumentsApiRequest
    {
        public Guid WorkspaceId { get; set; }
        public List<IFormFile> Files { get; set; } = new List<IFormFile>();
        public Guid? FolderId { get; set; }
        public string? Description { get; set; }
        public bool IsPrivate { get; set; } = false;
    }

    public class SecureUrlResponse
    {
        public string Url { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}