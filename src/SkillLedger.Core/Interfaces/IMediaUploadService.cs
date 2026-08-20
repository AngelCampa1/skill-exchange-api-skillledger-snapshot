using SkillLedger.Core.Enums;
using SkillLedger.Core.Models;

namespace SkillLedger.Core.Interfaces;

/// <summary>
/// Service for handling media uploads with security and moderation
/// </summary>
public interface IMediaUploadService
{
    /// <summary>
    /// Upload profile photo with automatic moderation
    /// </summary>
    /// <param name="userId">User uploading the photo</param>
    /// <param name="imageStream">Image data stream</param>
    /// <param name="fileName">Original file name</param>
    /// <param name="contentType">MIME type of the image</param>
    /// <returns>Upload result with moderation status</returns>
    Task<MediaUploadResult> UploadProfilePhotoAsync(Guid userId, Stream imageStream, string fileName, string contentType);

    /// <summary>
    /// Upload project attachment with virus scanning and moderation
    /// </summary>
    /// <param name="userId">User uploading the file</param>
    /// <param name="fileStream">File data stream</param>
    /// <param name="fileName">Original file name</param>
    /// <param name="contentType">MIME type of the file</param>
    /// <returns>Upload result</returns>
    Task<MediaUploadResult> UploadProjectAttachmentAsync(Guid userId, Stream fileStream, string fileName, string contentType);

    /// <summary>
    /// Generate optimized image variants (thumbnails, different sizes)
    /// </summary>
    /// <param name="originalImageUrl">URL of original image</param>
    /// <param name="variants">Required image variants</param>
    /// <returns>URLs of generated variants</returns>
    Task<Dictionary<string, string>> GenerateImageVariantsAsync(string originalImageUrl, ImageVariant[] variants);

    /// <summary>
    /// Scan file for malware and security threats
    /// </summary>
    /// <param name="fileStream">File to scan</param>
    /// <param name="fileName">Original file name</param>
    /// <returns>Security scan result</returns>
    Task<SecurityScanResult> ScanFileSecurityAsync(Stream fileStream, string fileName);

    /// <summary>
    /// Get signed URL for secure file access
    /// </summary>
    /// <param name="fileId">File identifier</param>
    /// <param name="expirationMinutes">URL expiration time</param>
    /// <returns>Signed URL for file access</returns>
    Task<string?> GetSecureFileUrlAsync(Guid fileId, int expirationMinutes = 60);

    /// <summary>
    /// Delete uploaded file and all variants
    /// </summary>
    /// <param name="fileId">File identifier</param>
    /// <param name="userId">User requesting deletion (for authorization)</param>
    /// <returns>Success indicator</returns>
    Task<bool> DeleteFileAsync(Guid fileId, Guid userId);

    /// <summary>
    /// Get user's upload quota and usage
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Quota information</returns>
    Task<UploadQuota> GetUploadQuotaAsync(Guid userId);
}

/// <summary>
/// Result of media upload operation
/// </summary>
public class MediaUploadResult
{
    public bool Success { get; set; }
    public Guid? FileId { get; set; }
    public string? FileUrl { get; set; }
    public Dictionary<string, string> ImageVariants { get; set; } = new();
    public ContentModerationResult? ModerationResult { get; set; }
    public SecurityScanResult? SecurityScanResult { get; set; }
    public string? ErrorMessage { get; set; }
    public long FileSizeBytes { get; set; }
    public string? ContentType { get; set; }
    public bool RequiresApproval { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}

// SecurityScanResult moved to SkillLedger.Core.Models namespace

/// <summary>
/// Image variant specifications
/// </summary>
public class ImageVariant
{
    public string Name { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public ImageQuality Quality { get; set; } = ImageQuality.High;
    public bool MaintainAspectRatio { get; set; } = true;
    public string Format { get; set; } = "webp"; // webp, jpeg, png
}

/// <summary>
/// User upload quota and usage
/// </summary>
public class UploadQuota
{
    public long TotalQuotaBytes { get; set; }
    public long UsedQuotaBytes { get; set; }
    public long RemainingQuotaBytes => TotalQuotaBytes - UsedQuotaBytes;
    public int MaxFileSizeBytes { get; set; }
    public int MaxFilesPerDay { get; set; }
    public int FilesUploadedToday { get; set; }
    public DateTime QuotaPeriodStart { get; set; }
    public DateTime QuotaPeriodEnd { get; set; }
}

// SecurityRiskLevel enum moved to SkillLedger.Core.Enums namespace

/// <summary>
/// Image quality settings
/// </summary>
public enum ImageQuality
{
    Low = 60,
    Medium = 80,
    High = 90,
    Maximum = 100
}