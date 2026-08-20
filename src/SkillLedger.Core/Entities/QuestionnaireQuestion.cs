using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.Entities;

/// <summary>
/// Represents a question within a questionnaire
/// </summary>
public class QuestionnaireQuestion
{
    public QuestionnaireQuestion()
    {
        Id = Guid.NewGuid();
        Options = new HashSet<QuestionOption>();
        Responses = new HashSet<QuestionResponse>();
    }

    /// <summary>
    /// Unique identifier for the question
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The questionnaire this question belongs to
    /// </summary>
    public Guid QuestionnaireId { get; set; }

    /// <summary>
    /// The question text
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string QuestionText { get; set; } = string.Empty;

    /// <summary>
    /// Additional description or help text for the question
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// The type of question (text, multiple choice, etc.)
    /// </summary>
    public QuestionType Type { get; set; } = QuestionType.Text;

    /// <summary>
    /// Whether this question is required to be answered
    /// </summary>
    public bool IsRequired { get; set; } = false;

    /// <summary>
    /// Display order of the question within the questionnaire
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// JSON configuration for question-specific settings
    /// </summary>
    public string? Configuration { get; set; }

    /// <summary>
    /// Default value for the question (if applicable)
    /// </summary>
    [MaxLength(1000)]
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Placeholder text for input fields
    /// </summary>
    [MaxLength(200)]
    public string? PlaceholderText { get; set; }

    /// <summary>
    /// Regular expression for validation (for text inputs)
    /// </summary>
    [MaxLength(500)]
    public string? ValidationRegex { get; set; }

    /// <summary>
    /// Custom validation error message
    /// </summary>
    [MaxLength(200)]
    public string? ValidationMessage { get; set; }

    /// <summary>
    /// Minimum value/length constraint
    /// </summary>
    public int? MinValue { get; set; }

    /// <summary>
    /// Maximum value/length constraint
    /// </summary>
    public int? MaxValue { get; set; }

    /// <summary>
    /// Whether the question is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the question was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the question was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the parent questionnaire
    /// </summary>
    public virtual Questionnaire Questionnaire { get; set; } = null!;

    /// <summary>
    /// Collection of options for multiple choice questions
    /// </summary>
    public virtual ICollection<QuestionOption> Options { get; set; }

    /// <summary>
    /// Collection of responses to this question
    /// </summary>
    public virtual ICollection<QuestionResponse> Responses { get; set; }

    /// <summary>
    /// Check if this question supports multiple options
    /// </summary>
    public bool SupportsOptions()
    {
        return Type == QuestionType.MultipleChoice ||
               Type == QuestionType.Dropdown ||
               Type == QuestionType.Checkbox ||
               Type == QuestionType.Radio;
    }

    /// <summary>
    /// Check if this question allows multiple selections
    /// </summary>
    public bool AllowsMultipleSelections()
    {
        return Type == QuestionType.Checkbox;
    }

    /// <summary>
    /// Get the validation constraints for this question
    /// </summary>
    public (bool IsValid, string? ErrorMessage) ValidateResponse(string? response)
    {
        // Check if required
        if (IsRequired && string.IsNullOrWhiteSpace(response))
        {
            return (false, "This field is required.");
        }

        // Skip further validation if empty and not required
        if (string.IsNullOrWhiteSpace(response))
        {
            return (true, null);
        }

        // Check length constraints for text inputs
        if (Type == QuestionType.Text || Type == QuestionType.LongText)
        {
            if (MinValue.HasValue && response.Length < MinValue.Value)
            {
                return (false, $"Minimum length is {MinValue.Value} characters.");
            }

            if (MaxValue.HasValue && response.Length > MaxValue.Value)
            {
                return (false, $"Maximum length is {MaxValue.Value} characters.");
            }
        }

        // Check numeric constraints
        if (Type == QuestionType.Number)
        {
            if (!int.TryParse(response, out int numValue))
            {
                return (false, "Please enter a valid number.");
            }

            if (MinValue.HasValue && numValue < MinValue.Value)
            {
                return (false, $"Minimum value is {MinValue.Value}.");
            }

            if (MaxValue.HasValue && numValue > MaxValue.Value)
            {
                return (false, $"Maximum value is {MaxValue.Value}.");
            }
        }

        // Check regex validation
        if (!string.IsNullOrEmpty(ValidationRegex))
        {
            try
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(response, ValidationRegex))
                {
                    return (false, ValidationMessage ?? "Invalid format.");
                }
            }
            catch
            {
                // Invalid regex - skip validation but log the error
                return (true, null);
            }
        }

        return (true, null);
    }
}

/// <summary>
/// Types of questions that can be created
/// </summary>
public enum QuestionType
{
    /// <summary>
    /// Single line text input
    /// </summary>
    Text = 0,

    /// <summary>
    /// Multi-line text area
    /// </summary>
    LongText = 1,

    /// <summary>
    /// Numeric input
    /// </summary>
    Number = 2,

    /// <summary>
    /// Email address input
    /// </summary>
    Email = 3,

    /// <summary>
    /// Phone number input
    /// </summary>
    Phone = 4,

    /// <summary>
    /// Date picker
    /// </summary>
    Date = 5,

    /// <summary>
    /// Time picker
    /// </summary>
    Time = 6,

    /// <summary>
    /// Date and time picker
    /// </summary>
    DateTime = 7,

    /// <summary>
    /// Yes/No boolean question
    /// </summary>
    Boolean = 8,

    /// <summary>
    /// Single selection from multiple options
    /// </summary>
    Radio = 9,

    /// <summary>
    /// Multiple selections from options
    /// </summary>
    Checkbox = 10,

    /// <summary>
    /// Dropdown selection
    /// </summary>
    Dropdown = 11,

    /// <summary>
    /// Multiple choice with single selection
    /// </summary>
    MultipleChoice = 12,

    /// <summary>
    /// Rating scale (1-5, 1-10, etc.)
    /// </summary>
    Rating = 13,

    /// <summary>
    /// File upload input
    /// </summary>
    FileUpload = 14,

    /// <summary>
    /// URL input
    /// </summary>
    Url = 15
}