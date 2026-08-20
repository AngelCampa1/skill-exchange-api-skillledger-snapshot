using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.Entities;

public class ProjectApplicationAttachment
{
    public ProjectApplicationAttachment()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Unique identifier for the attachment
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the project application
    /// </summary>
    public Guid ProjectApplicationId { get; set; }

    /// <summary>
    /// Original filename of the uploaded file
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = null!;

    /// <summary>
    /// File content type (MIME type)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ContentType { get; set; } = null!;

    /// <summary>
    /// File size in bytes
    /// </summary>
    [Range(1, 10 * 1024 * 1024)] // Max 10MB
    public long FileSize { get; set; }

    /// <summary>
    /// Storage URL or path for the file
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string StorageUrl { get; set; } = null!;

    /// <summary>
    /// Optional description of the portfolio item
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Whether the file has passed virus scanning
    /// </summary>
    public bool IsVirusScanned { get; set; } = false;

    /// <summary>
    /// Whether the file is safe (passed virus scan)
    /// </summary>
    public bool IsSafe { get; set; } = false;

    /// <summary>
    /// When the attachment was uploaded
    /// </summary>
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the project application
    /// </summary>
    public virtual ProjectApplication ProjectApplication { get; set; } = null!;
}