using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.Entities;

/// <summary>
/// Represents a response to a specific question within a questionnaire
/// </summary>
public class QuestionResponse
{
    public QuestionResponse()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Unique identifier for the question response
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The questionnaire response this belongs to
    /// </summary>
    public Guid QuestionnaireResponseId { get; set; }

    /// <summary>
    /// The specific question being answered
    /// </summary>
    public Guid QuestionId { get; set; }

    /// <summary>
    /// The response value as text
    /// </summary>
    public string? ResponseValue { get; set; }

    /// <summary>
    /// For multiple choice questions, the selected option IDs (JSON array)
    /// </summary>
    public string? SelectedOptionIds { get; set; }

    /// <summary>
    /// For file upload questions, the file paths or URLs (JSON array)
    /// </summary>
    public string? FileAttachments { get; set; }

    /// <summary>
    /// Additional metadata for the response
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Whether this response is valid according to question constraints
    /// </summary>
    public bool IsValid { get; set; } = true;

    /// <summary>
    /// Validation error message if applicable
    /// </summary>
    [MaxLength(500)]
    public string? ValidationError { get; set; }

    /// <summary>
    /// When this response was first created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this response was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the parent questionnaire response
    /// </summary>
    public virtual QuestionnaireResponse QuestionnaireResponse { get; set; } = null!;

    /// <summary>
    /// Navigation property to the question being answered
    /// </summary>
    public virtual QuestionnaireQuestion Question { get; set; } = null!;

    /// <summary>
    /// Check if this response has a value
    /// </summary>
    public bool HasValue()
    {
        return !string.IsNullOrWhiteSpace(ResponseValue) ||
               !string.IsNullOrWhiteSpace(SelectedOptionIds) ||
               !string.IsNullOrWhiteSpace(FileAttachments);
    }

    /// <summary>
    /// Get the selected option IDs as a list
    /// </summary>
    public List<Guid> GetSelectedOptionIds()
    {
        if (string.IsNullOrWhiteSpace(SelectedOptionIds))
            return new List<Guid>();

        try
        {
            var ids = System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(SelectedOptionIds);
            return ids ?? new List<Guid>();
        }
        catch
        {
            return new List<Guid>();
        }
    }

    /// <summary>
    /// Set the selected option IDs from a list
    /// </summary>
    public void SetSelectedOptionIds(List<Guid> optionIds)
    {
        if (optionIds == null || !optionIds.Any())
        {
            SelectedOptionIds = null;
            return;
        }

        SelectedOptionIds = System.Text.Json.JsonSerializer.Serialize(optionIds);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Get the file attachments as a list
    /// </summary>
    public List<string> GetFileAttachments()
    {
        if (string.IsNullOrWhiteSpace(FileAttachments))
            return new List<string>();

        try
        {
            var files = System.Text.Json.JsonSerializer.Deserialize<List<string>>(FileAttachments);
            return files ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    /// <summary>
    /// Set the file attachments from a list
    /// </summary>
    public void SetFileAttachments(List<string> filePaths)
    {
        if (filePaths == null || !filePaths.Any())
        {
            FileAttachments = null;
            return;
        }

        FileAttachments = System.Text.Json.JsonSerializer.Serialize(filePaths);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update the response value and mark as updated
    /// </summary>
    public void SetResponseValue(string? value)
    {
        ResponseValue = value;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Validate this response against its question's constraints
    /// </summary>
    public void ValidateResponse()
    {
        if (Question == null)
        {
            IsValid = true;
            ValidationError = null;
            return;
        }

        var (isValid, errorMessage) = Question.ValidateResponse(ResponseValue);
        IsValid = isValid;
        ValidationError = errorMessage;
    }
}