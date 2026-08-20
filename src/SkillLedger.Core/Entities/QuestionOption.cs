using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.Entities;

/// <summary>
/// Represents an option for multiple choice questions
/// </summary>
public class QuestionOption
{
    public QuestionOption()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Unique identifier for the option
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The question this option belongs to
    /// </summary>
    public Guid QuestionId { get; set; }

    /// <summary>
    /// The display text for this option
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string OptionText { get; set; } = string.Empty;

    /// <summary>
    /// The value associated with this option (for data processing)
    /// </summary>
    [MaxLength(100)]
    public string? OptionValue { get; set; }

    /// <summary>
    /// Display order of this option within the question
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Whether this option is currently active/visible
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether this is the default selected option
    /// </summary>
    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// Additional metadata for the option
    /// </summary>
    [MaxLength(500)]
    public string? Metadata { get; set; }

    /// <summary>
    /// When the option was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the option was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the parent question
    /// </summary>
    public virtual QuestionnaireQuestion Question { get; set; } = null!;

    /// <summary>
    /// Get the effective value for this option (OptionValue if set, otherwise OptionText)
    /// </summary>
    public string GetEffectiveValue()
    {
        return !string.IsNullOrEmpty(OptionValue) ? OptionValue : OptionText;
    }
}