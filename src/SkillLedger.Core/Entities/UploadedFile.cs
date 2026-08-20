using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.Entities;

/// <summary>
/// Metadata for uploaded files
/// </summary>
public class UploadedFile
{
    public UploadedFile()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User who uploaded the file
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Original file name
    /// </summary>
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// MIME type of the file
    /// </summary>
    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Blob name in storage
    /// </summary>
    [MaxLength(500)]
    public string BlobName { get; set; } = string.Empty;

    /// <summary>
    /// Container name in blob storage
    /// </summary>
    [MaxLength(100)]
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>
    /// Type of file uploaded
    /// </summary>
    public int FileType { get; set; }

    /// <summary>
    /// Whether file is approved for public access
    /// </summary>
    public bool IsApproved { get; set; }

    /// <summary>
    /// Whether file requires human review
    /// </summary>
    public bool RequiresHumanReview { get; set; }

    /// <summary>
    /// Whether file passed security scanning
    /// </summary>
    public bool SecurityScanPassed { get; set; }

    /// <summary>
    /// Content moderation result (JSON)
    /// </summary>
    public string? ModerationResult { get; set; }

    /// <summary>
    /// Security scan result (JSON)
    /// </summary>
    public string? SecurityScanResult { get; set; }

    /// <summary>
    /// URLs of generated image variants (JSON)
    /// </summary>
    public string? ImageVariants { get; set; }

    /// <summary>
    /// When file was uploaded
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When file was last accessed
    /// </summary>
    public DateTime? LastAccessedAt { get; set; }

    /// <summary>
    /// Navigation property to user
    /// </summary>
    public virtual User User { get; set; } = null!;
}