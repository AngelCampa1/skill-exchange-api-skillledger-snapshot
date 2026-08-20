using System.ComponentModel.DataAnnotations;
using SkillLedger.Core.Enums;

namespace SkillLedger.Core.Entities;

/// <summary>
/// Entity for managing export templates used in financial reporting
/// Allows users to save custom export formats and configurations
/// </summary>
public class ExportTemplate
{
    public ExportTemplate()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Unique identifier for the export template
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User-friendly name for the template
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Optional description of what this template is for
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Export format this template is designed for
    /// </summary>
    public ExportFormat Format { get; set; }

    /// <summary>
    /// JSON-serialized template configuration data
    /// Contains formatting rules, styling, column selections, etc.
    /// </summary>
    public string? TemplateData { get; set; }

    /// <summary>
    /// Reference to the user who created this template
    /// Null for system-wide templates
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Whether this template is currently active and available for use
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether this is a system template (available to all users)
    /// </summary>
    public bool IsSystemTemplate { get; set; } = false;

    /// <summary>
    /// When this template was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this template was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Navigation property to the user who created this template
    /// </summary>
    public virtual User? User { get; set; }

    /// <summary>
    /// Helper method to check if the template is accessible by a specific user
    /// </summary>
    /// <param name="userId">The user ID to check access for</param>
    /// <returns>True if the user can access this template</returns>
    public bool IsAccessibleBy(Guid userId)
    {
        return IsSystemTemplate || UserId == userId;
    }

    /// <summary>
    /// Helper method to validate template data structure
    /// </summary>
    /// <returns>True if template data is valid</returns>
    public bool HasValidTemplateData()
    {
        if (string.IsNullOrWhiteSpace(TemplateData))
            return true; // Empty template data is valid

        try
        {
            // Basic JSON validation
            System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(TemplateData);
            return true;
        }
        catch
        {
            return false;
        }
    }
}