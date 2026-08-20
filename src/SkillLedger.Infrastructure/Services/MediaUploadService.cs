using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using SkillLedger.Core.Interfaces;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Models;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Infrastructure.Configuration;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Formats.Jpeg;
using System.Security.Cryptography;
using System.Text;

namespace SkillLedger.Infrastructure.Services;

/// <summary>
/// Service for secure media uploads with automatic moderation and optimization
/// </summary>
public class MediaUploadService : IMediaUploadService
{
    private readonly ILogger<MediaUploadService> _logger;
    private readonly SkillLedgerDbContext _context;
    private readonly IContentModerationService _contentModerationService;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly MediaUploadConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;

    // Supported image formats
    private static readonly HashSet<string> SupportedImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/jpg", "image/png", "image/webp", "image/gif"
    };

    // Supported document formats for project attachments
    private static readonly HashSet<string> SupportedDocumentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf", "text/plain", "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-powerpoint",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation"
    };

    // Dangerous file extensions
    private static readonly HashSet<string> DangerousExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".bat", ".cmd", ".com", ".pif", ".scr", ".vbs", ".js", ".jar",
        ".app", ".deb", ".pkg", ".dmg", ".rpm", ".msi", ".ps1", ".sh", ".bash"
    };

    public MediaUploadService(
        ILogger<MediaUploadService> logger,
        SkillLedgerDbContext context,
        IContentModerationService contentModerationService,
        BlobServiceClient blobServiceClient,
        IOptions<MediaUploadConfiguration> config,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _context = context;
        _contentModerationService = contentModerationService;
        _blobServiceClient = blobServiceClient;
        _config = config.Value;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Upload and moderate profile photo
    /// </summary>
    public async Task<MediaUploadResult> UploadProfilePhotoAsync(Guid userId, Stream imageStream, string fileName, string contentType)
    {
        var result = new MediaUploadResult
        {
            ContentType = contentType,
            FileSizeBytes = imageStream.Length
        };

        try
        {
            // Validate file type and size
            var validation = ValidateImageUpload(imageStream, fileName, contentType);
            if (!validation.IsValid)
            {
                result.ErrorMessage = validation.ErrorMessage;
                return result;
            }

            // Check user quota
            var quota = await GetUploadQuotaAsync(userId);
            if (quota.RemainingQuotaBytes < imageStream.Length)
            {
                result.ErrorMessage = "Upload quota exceeded";
                return result;
            }

            // Security scan
            imageStream.Position = 0;
            var securityScan = await ScanFileSecurityAsync(imageStream, fileName);
            result.SecurityScanResult = securityScan;

            if (!securityScan.IsSafe)
            {
                result.ErrorMessage = $"Security scan failed: {string.Join(", ", securityScan.ThreatTypes)}";
                return result;
            }

            // Content moderation
            imageStream.Position = 0;
            var moderation = await _contentModerationService.AnalyzeImageAsync(imageStream, userId);
            result.ModerationResult = moderation;
            result.RequiresApproval = !moderation.IsApproved || moderation.RequiresHumanReview;

            // Generate file ID and paths
            var fileId = Guid.NewGuid();
            var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
            var blobName = $"profiles/{userId}/photos/{fileId}{fileExtension}";

            // Upload original image
            imageStream.Position = 0;
            var containerClient = _blobServiceClient.GetBlobContainerClient(_config.ProfilePhotosContainer);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            var blobClient = containerClient.GetBlobClient(blobName);
            var uploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType,
                    CacheControl = "public, max-age=31536000" // 1 year cache
                },
                Metadata = new Dictionary<string, string>
                {
                    ["userId"] = userId.ToString(),
                    ["originalFileName"] = fileName,
                    ["uploadedAt"] = DateTime.UtcNow.ToString("O"),
                    ["moderated"] = moderation.IsApproved.ToString(),
                    ["requiresApproval"] = result.RequiresApproval.ToString()
                }
            };

            await blobClient.UploadAsync(imageStream, uploadOptions);

            // Generate optimized variants if approved
            if (moderation.IsApproved && !result.RequiresApproval)
            {
                var variants = new[]
                {
                    new ImageVariant { Name = "thumbnail", Width = 150, Height = 150 },
                    new ImageVariant { Name = "small", Width = 300, Height = 300 },
                    new ImageVariant { Name = "medium", Width = 600, Height = 600 },
                    new ImageVariant { Name = "large", Width = 1200, Height = 1200 }
                };

                result.ImageVariants = await GenerateImageVariantsAsync(blobClient.Uri.ToString(), variants);
            }

            // Store file metadata in database
            var uploadedFile = new UploadedFile
            {
                Id = fileId,
                UserId = userId,
                FileName = fileName,
                ContentType = contentType,
                FileSizeBytes = imageStream.Length,
                BlobName = blobName,
                ContainerName = _config.ProfilePhotosContainer,
                FileType = (int)FileType.ProfilePhoto,
                IsApproved = moderation.IsApproved && !result.RequiresApproval,
                RequiresHumanReview = moderation.RequiresHumanReview,
                SecurityScanPassed = securityScan.IsSafe,
                ModerationResult = System.Text.Json.JsonSerializer.Serialize(moderation)
            };

            _context.UploadedFiles.Add(uploadedFile);
            await _context.SaveChangesAsync();

            result.Success = true;
            result.FileId = fileId;
            result.FileUrl = result.RequiresApproval ? null : blobClient.Uri.ToString();

            _logger.LogInformation("Profile photo uploaded successfully for user {UserId}, FileId: {FileId}, RequiresApproval: {RequiresApproval}",
                userId, fileId, result.RequiresApproval);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading profile photo for user {UserId}", userId);
            result.ErrorMessage = "Upload service temporarily unavailable";
            return result;
        }
    }

    /// <summary>
    /// Upload project attachment with comprehensive security checks
    /// </summary>
    public async Task<MediaUploadResult> UploadProjectAttachmentAsync(Guid userId, Stream fileStream, string fileName, string contentType)
    {
        var result = new MediaUploadResult
        {
            ContentType = contentType,
            FileSizeBytes = fileStream.Length
        };

        try
        {
            // Validate file
            var validation = ValidateDocumentUpload(fileStream, fileName, contentType);
            if (!validation.IsValid)
            {
                result.ErrorMessage = validation.ErrorMessage;
                return result;
            }

            // Check quota
            var quota = await GetUploadQuotaAsync(userId);
            if (quota.RemainingQuotaBytes < fileStream.Length)
            {
                result.ErrorMessage = "Upload quota exceeded";
                return result;
            }

            // Security scan
            fileStream.Position = 0;
            var securityScan = await ScanFileSecurityAsync(fileStream, fileName);
            result.SecurityScanResult = securityScan;

            if (!securityScan.IsSafe)
            {
                result.ErrorMessage = $"Security scan failed: {string.Join(", ", securityScan.ThreatTypes)}";
                return result;
            }

            // Upload file
            var fileId = Guid.NewGuid();
            var sanitizedFileName = SanitizeFileName(fileName);
            var blobName = $"projects/{userId}/attachments/{fileId}_{sanitizedFileName}";

            fileStream.Position = 0;
            var containerClient = _blobServiceClient.GetBlobContainerClient(_config.ProjectAttachmentsContainer);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            var blobClient = containerClient.GetBlobClient(blobName);
            var uploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType,
                    ContentDisposition = $"attachment; filename=\"{sanitizedFileName}\""
                },
                Metadata = new Dictionary<string, string>
                {
                    ["userId"] = userId.ToString(),
                    ["originalFileName"] = fileName,
                    ["uploadedAt"] = DateTime.UtcNow.ToString("O"),
                    ["securityScanned"] = securityScan.IsSafe.ToString()
                }
            };

            await blobClient.UploadAsync(fileStream, uploadOptions);

            // Store metadata
            var uploadedFile = new UploadedFile
            {
                Id = fileId,
                UserId = userId,
                FileName = fileName,
                ContentType = contentType,
                FileSizeBytes = fileStream.Length,
                BlobName = blobName,
                ContainerName = _config.ProjectAttachmentsContainer,
                FileType = (int)FileType.ProjectAttachment,
                IsApproved = true, // Documents auto-approved if they pass security
                RequiresHumanReview = false,
                SecurityScanPassed = securityScan.IsSafe
            };

            _context.UploadedFiles.Add(uploadedFile);
            await _context.SaveChangesAsync();

            result.Success = true;
            result.FileId = fileId;
            result.FileUrl = await GetSecureFileUrlAsync(fileId, 60); // 1 hour expiry for documents

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading project attachment for user {UserId}", userId);
            result.ErrorMessage = "Upload service temporarily unavailable";
            return result;
        }
    }

    /// <summary>
    /// Generate optimized image variants
    /// </summary>
    public async Task<Dictionary<string, string>> GenerateImageVariantsAsync(string originalImageUrl, ImageVariant[] variants)
    {
        var variantUrls = new Dictionary<string, string>();

        try
        {
            // BUG-NEW-008 FIX: Use IHttpClientFactory instead of new HttpClient() to prevent socket exhaustion
            // Download original image
            using var httpClient = _httpClientFactory.CreateClient();
            using var originalStream = await httpClient.GetStreamAsync(originalImageUrl);
            using var image = await Image.LoadAsync(originalStream);

            foreach (var variant in variants)
            {
                try
                {
                    using var variantImage = image.Clone(ctx => { });

                    // Resize maintaining aspect ratio if specified
                    if (variant.MaintainAspectRatio)
                    {
                        variantImage.Mutate(x => x.Resize(variant.Width, variant.Height, KnownResamplers.Lanczos3));
                    }
                    else
                    {
                        variantImage.Mutate(x => x.Resize(variant.Width, variant.Height, KnownResamplers.Lanczos3));
                    }

                    // Generate blob name for variant
                    var originalUri = new Uri(originalImageUrl);
                    var originalBlobName = originalUri.Segments.Last();
                    var fileNameWithoutExt = Path.GetFileNameWithoutExtension(originalBlobName);
                    var variantBlobName = $"{fileNameWithoutExt}_{variant.Name}.{variant.Format}";
                    var fullVariantPath = originalUri.AbsolutePath.Replace(originalBlobName, variantBlobName);

                    // Upload variant
                    using var variantStream = new MemoryStream();

                    if (variant.Format.ToLower() == "webp")
                    {
                        await variantImage.SaveAsWebpAsync(variantStream, new WebpEncoder { Quality = (int)variant.Quality });
                    }
                    else
                    {
                        await variantImage.SaveAsJpegAsync(variantStream, new JpegEncoder { Quality = (int)variant.Quality });
                    }

                    variantStream.Position = 0;

                    var containerClient = _blobServiceClient.GetBlobContainerClient(_config.ProfilePhotosContainer);
                    var variantBlobClient = containerClient.GetBlobClient(fullVariantPath.TrimStart('/'));

                    await variantBlobClient.UploadAsync(variantStream, new BlobUploadOptions
                    {
                        HttpHeaders = new BlobHttpHeaders
                        {
                            ContentType = $"image/{variant.Format}",
                            CacheControl = "public, max-age=31536000"
                        }
                    });

                    variantUrls[variant.Name] = variantBlobClient.Uri.ToString();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating image variant {VariantName}", variant.Name);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating image variants for {ImageUrl}", originalImageUrl);
        }

        return variantUrls;
    }

    /// <summary>
    /// Scan file for security threats (basic implementation)
    /// </summary>
    public async Task<SecurityScanResult> ScanFileSecurityAsync(Stream fileStream, string fileName)
    {
        var result = new SecurityScanResult
        {
            ScanEngine = "SkillLedger Basic Scanner"
        };

        try
        {
            // Check for dangerous file extensions
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (DangerousExtensions.Contains(extension))
            {
                result.IsSafe = false;
                result.RiskLevel = SecurityRiskLevel.Critical;
                result.ThreatTypes = new[] { $"Potentially dangerous file type: {extension}" };
                return result;
            }

            // Check file size limits
            if (fileStream.Length > _config.MaxFileSizeBytes)
            {
                result.IsSafe = false;
                result.RiskLevel = SecurityRiskLevel.High;
                result.ThreatTypes = new[] { "File size exceeds security limits" };
                return result;
            }

            // Basic malware signature detection (simplified)
            fileStream.Position = 0;
            var buffer = new byte[Math.Min(4096, fileStream.Length)]; // Check first 4KB
            await fileStream.ReadExactlyAsync(buffer);

            // Check for common malware signatures (very basic)
            var suspiciousPatterns = new[]
            {
                new byte[] { 0x4D, 0x5A }, // PE header
                Encoding.ASCII.GetBytes("eval("),
                Encoding.ASCII.GetBytes("<script"),
                Encoding.ASCII.GetBytes("javascript:"),
                Encoding.ASCII.GetBytes("vbscript:")
            };

            foreach (var pattern in suspiciousPatterns)
            {
                if (ContainsPattern(buffer, pattern))
                {
                    result.IsSafe = false;
                    result.RiskLevel = SecurityRiskLevel.High;
                    result.ThreatTypes = new[] { "Potentially malicious content detected" };
                    return result;
                }
            }

            // File passed basic checks
            result.IsSafe = true;
            result.RiskLevel = SecurityRiskLevel.Low;

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning file security for {FileName}", fileName);

            // Err on the side of caution
            return new SecurityScanResult
            {
                IsSafe = false,
                RiskLevel = SecurityRiskLevel.Critical,
                ThreatTypes = new[] { "Security scan failed" },
                ScanEngine = "SkillLedger Basic Scanner"
            };
        }
    }

    /// <summary>
    /// Generate signed URL for secure file access
    /// </summary>
    public async Task<string?> GetSecureFileUrlAsync(Guid fileId, int expirationMinutes = 60)
    {
        try
        {
            var file = await _context.UploadedFiles
                .FirstOrDefaultAsync(f => f.Id == fileId && f.IsApproved && f.SecurityScanPassed);

            if (file == null)
                return null;

            var containerClient = _blobServiceClient.GetBlobContainerClient(file.ContainerName);
            var blobClient = containerClient.GetBlobClient(file.BlobName);

            // Generate SAS token for temporary access
            if (blobClient.CanGenerateSasUri)
            {
                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = file.ContainerName,
                    BlobName = file.BlobName,
                    Resource = "b",
                    ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(expirationMinutes)
                };
                sasBuilder.SetPermissions(BlobSasPermissions.Read);

                return blobClient.GenerateSasUri(sasBuilder).ToString();
            }

            return blobClient.Uri.ToString(); // Fallback to direct URL
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating secure URL for file {FileId}", fileId);
            return null;
        }
    }

    /// <summary>
    /// Delete file and all variants
    /// </summary>
    public async Task<bool> DeleteFileAsync(Guid fileId, Guid userId)
    {
        try
        {
            var file = await _context.UploadedFiles
                .FirstOrDefaultAsync(f => f.Id == fileId && f.UserId == userId);

            if (file == null)
                return false;

            // Delete from blob storage
            var containerClient = _blobServiceClient.GetBlobContainerClient(file.ContainerName);
            var blobClient = containerClient.GetBlobClient(file.BlobName);
            await blobClient.DeleteIfExistsAsync();

            // Delete variants if it's an image
            if (file.FileType == (int)FileType.ProfilePhoto)
            {
                var variants = new[] { "thumbnail", "small", "medium", "large" };
                var baseName = Path.GetFileNameWithoutExtension(file.BlobName);

                foreach (var variant in variants)
                {
                    var variantName = $"{baseName}_{variant}.webp";
                    var variantBlobClient = containerClient.GetBlobClient(variantName);
                    await variantBlobClient.DeleteIfExistsAsync();
                }
            }

            // Remove from database
            _context.UploadedFiles.Remove(file);
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {FileId} for user {UserId}", fileId, userId);
            return false;
        }
    }

    /// <summary>
    /// Get user's upload quota and current usage
    /// </summary>
    public async Task<UploadQuota> GetUploadQuotaAsync(Guid userId)
    {
        try
        {
            var today = DateTime.UtcNow.Date;

            var totalUsage = await _context.UploadedFiles
                .Where(f => f.UserId == userId)
                .SumAsync(f => f.FileSizeBytes);

            var todaysUploads = await _context.UploadedFiles
                .Where(f => f.UserId == userId && f.CreatedAt.Date == today)
                .CountAsync();

            return new UploadQuota
            {
                TotalQuotaBytes = _config.UserQuotaBytes,
                UsedQuotaBytes = totalUsage,
                MaxFileSizeBytes = _config.MaxFileSizeBytes,
                MaxFilesPerDay = _config.MaxFilesPerDay,
                FilesUploadedToday = todaysUploads,
                QuotaPeriodStart = today,
                QuotaPeriodEnd = today.AddDays(1)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting upload quota for user {UserId}", userId);
            return new UploadQuota(); // Return empty quota on error
        }
    }

    private static ValidationResult ValidateImageUpload(Stream imageStream, string fileName, string contentType)
    {
        if (!SupportedImageTypes.Contains(contentType))
        {
            return new ValidationResult(false, $"Unsupported image type: {contentType}");
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

        if (!allowedExtensions.Contains(extension))
        {
            return new ValidationResult(false, $"Unsupported file extension: {extension}");
        }

        if (imageStream.Length > 10 * 1024 * 1024) // 10MB limit for images
        {
            return new ValidationResult(false, "Image file size exceeds 10MB limit");
        }

        return new ValidationResult(true);
    }

    private static ValidationResult ValidateDocumentUpload(Stream fileStream, string fileName, string contentType)
    {
        if (!SupportedDocumentTypes.Contains(contentType))
        {
            return new ValidationResult(false, $"Unsupported document type: {contentType}");
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (DangerousExtensions.Contains(extension))
        {
            return new ValidationResult(false, $"Potentially dangerous file type: {extension}");
        }

        if (fileStream.Length > 50 * 1024 * 1024) // 50MB limit for documents
        {
            return new ValidationResult(false, "Document file size exceeds 50MB limit");
        }

        return new ValidationResult(true);
    }

    private static bool ContainsPattern(byte[] buffer, byte[] pattern)
    {
        for (int i = 0; i <= buffer.Length - pattern.Length; i++)
        {
            bool found = true;
            for (int j = 0; j < pattern.Length; j++)
            {
                if (buffer[i + j] != pattern[j])
                {
                    found = false;
                    break;
                }
            }
            if (found) return true;
        }
        return false;
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "file" : sanitized;
    }

    private class ValidationResult
    {
        public bool IsValid { get; }
        public string? ErrorMessage { get; }

        public ValidationResult(bool isValid, string? errorMessage = null)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
        }
    }
}

/// <summary>
/// Configuration for media upload service
/// </summary>
public class MediaUploadConfiguration
{
    public string ProfilePhotosContainer { get; set; } = "profile-photos";
    public string ProjectAttachmentsContainer { get; set; } = "project-attachments";
    public string WorkspaceDocumentsContainer { get; set; } = "workspace-documents";
    public long UserQuotaBytes { get; set; } = 1_073_741_824L; // 1GB
    public int MaxFileSizeBytes { get; set; } = 52_428_800; // 50MB
    public int MaxFilesPerDay { get; set; } = 50;
    public bool EnableImageOptimization { get; set; } = true;
    public bool EnableSecurityScanning { get; set; } = true;
    public string[] SupportedImageFormats { get; set; } = { "jpeg", "jpg", "png", "webp", "gif" };
    public string[] AllowedFileTypes { get; set; } = { "pdf", "docx", "txt", "jpg", "png", "gif", "jpeg", "webp" };
    public string? LocalStorageBasePath { get; set; }
    public string? SecurityKey { get; set; }
}

/// <summary>
/// File type enumeration
/// </summary>
public enum FileType
{
    ProfilePhoto = 0,
    ProjectAttachment = 1,
    Document = 2,
    Image = 3
}