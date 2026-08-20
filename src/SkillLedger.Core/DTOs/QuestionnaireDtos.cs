using SkillLedger.Core.Entities;
using SkillLedger.Core.Attributes;
using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.DTOs;

/// <summary>
/// DTO for creating a new questionnaire
/// </summary>
public class CreateQuestionnaireDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public QuestionnaireType Type { get; set; } = QuestionnaireType.General;

    public bool IsTemplate { get; set; } = false;

    public bool RequiresReview { get; set; } = false;

    public int? MaxResponses { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string? Metadata { get; set; }

    public List<CreateQuestionDto> Questions { get; set; } = new();
}

/// <summary>
/// DTO for updating an existing questionnaire
/// </summary>
public class UpdateQuestionnaireDto
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public QuestionnaireType Type { get; set; }

    public bool IsActive { get; set; }

    public bool IsTemplate { get; set; }

    public bool RequiresReview { get; set; }

    public int? MaxResponses { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string? Metadata { get; set; }
}

/// <summary>
/// DTO for questionnaire details
/// </summary>
public class QuestionnaireDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string? CreatedByUserName { get; set; }
    public QuestionnaireType Type { get; set; }
    public bool IsActive { get; set; }
    public bool IsTemplate { get; set; }
    public bool RequiresReview { get; set; }
    public int? MaxResponses { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Version { get; set; }
    public string? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int QuestionCount { get; set; }
    public int ResponseCount { get; set; }
    public bool IsAvailable { get; set; }
    public List<QuestionnaireQuestionDto> Questions { get; set; } = new();
}

/// <summary>
/// DTO for creating a new question
/// </summary>
public class CreateQuestionDto
{
    [Required]
    [MaxLength(500)]
    public string QuestionText { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public QuestionType Type { get; set; } = QuestionType.Text;

    public bool IsRequired { get; set; } = false;

    public int DisplayOrder { get; set; }

    public string? Configuration { get; set; }

    [MaxLength(1000)]
    public string? DefaultValue { get; set; }

    [MaxLength(200)]
    public string? PlaceholderText { get; set; }

    [MaxLength(500)]
    [SafeRegex] // VULN-018 FIX: Validate regex patterns to prevent ReDoS attacks
    public string? ValidationRegex { get; set; }

    [MaxLength(200)]
    public string? ValidationMessage { get; set; }

    public int? MinValue { get; set; }

    public int? MaxValue { get; set; }

    public List<CreateQuestionOptionDto> Options { get; set; } = new();
}

/// <summary>
/// DTO for updating an existing question
/// </summary>
public class UpdateQuestionDto
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(500)]
    public string QuestionText { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public QuestionType Type { get; set; }

    public bool IsRequired { get; set; }

    public bool IsActive { get; set; }

    public int DisplayOrder { get; set; }

    public string? Configuration { get; set; }

    [MaxLength(1000)]
    public string? DefaultValue { get; set; }

    [MaxLength(200)]
    public string? PlaceholderText { get; set; }

    [MaxLength(500)]
    [SafeRegex] // VULN-018 FIX: Validate regex patterns to prevent ReDoS attacks
    public string? ValidationRegex { get; set; }

    [MaxLength(200)]
    public string? ValidationMessage { get; set; }

    public int? MinValue { get; set; }

    public int? MaxValue { get; set; }
}

/// <summary>
/// DTO for question details
/// </summary>
public class QuestionnaireQuestionDto
{
    public Guid Id { get; set; }
    public Guid QuestionnaireId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string? Description { get; set; }
    public QuestionType Type { get; set; }
    public bool IsRequired { get; set; }
    public int DisplayOrder { get; set; }
    public string? Configuration { get; set; }
    public string? DefaultValue { get; set; }
    public string? PlaceholderText { get; set; }
    public string? ValidationRegex { get; set; }
    public string? ValidationMessage { get; set; }
    public int? MinValue { get; set; }
    public int? MaxValue { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<QuestionOptionDto> Options { get; set; } = new();
}

/// <summary>
/// DTO for creating a question option
/// </summary>
public class CreateQuestionOptionDto
{
    [Required]
    [MaxLength(200)]
    public string OptionText { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? OptionValue { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsDefault { get; set; } = false;

    [MaxLength(500)]
    public string? Metadata { get; set; }
}

/// <summary>
/// DTO for question option details
/// </summary>
public class QuestionOptionDto
{
    public Guid Id { get; set; }
    public Guid QuestionId { get; set; }
    public string OptionText { get; set; } = string.Empty;
    public string? OptionValue { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public string? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for submitting a questionnaire response
/// </summary>
public class SubmitQuestionnaireResponseDto
{
    public Guid QuestionnaireId { get; set; }
    public List<SubmitQuestionResponseDto> QuestionResponses { get; set; } = new();
    public string? Metadata { get; set; }
}

/// <summary>
/// DTO for submitting a question response
/// </summary>
public class SubmitQuestionResponseDto
{
    public Guid QuestionId { get; set; }
    public string? ResponseValue { get; set; }
    public List<Guid>? SelectedOptionIds { get; set; }
    public List<string>? FileAttachments { get; set; }
    public string? Metadata { get; set; }
}

/// <summary>
/// DTO for questionnaire response details
/// </summary>
public class QuestionnaireResponseDto
{
    public Guid Id { get; set; }
    public Guid QuestionnaireId { get; set; }
    public string QuestionnaireTitle { get; set; } = string.Empty;
    public Guid RespondentUserId { get; set; }
    public string? RespondentUserName { get; set; }
    public ResponseStatus Status { get; set; }
    public bool IsSubmitted { get; set; }
    public bool IsComplete { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? SubmittedFromIP { get; set; }
    public string? UserAgent { get; set; }
    public string? Metadata { get; set; }
    public string? ReviewNotes { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public string? ReviewedByUserName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public double CompletionPercentage { get; set; }
    public List<QuestionResponseDto> QuestionResponses { get; set; } = new();
}

/// <summary>
/// DTO for question response details
/// </summary>
public class QuestionResponseDto
{
    public Guid Id { get; set; }
    public Guid QuestionnaireResponseId { get; set; }
    public Guid QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; }
    public string? ResponseValue { get; set; }
    public List<Guid>? SelectedOptionIds { get; set; }
    public List<string>? FileAttachments { get; set; }
    public string? Metadata { get; set; }
    public bool IsValid { get; set; }
    public string? ValidationError { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for updating response status (for reviewers)
/// </summary>
public class UpdateResponseStatusDto
{
    public Guid ResponseId { get; set; }
    public ResponseStatus Status { get; set; }
    public string? ReviewNotes { get; set; }
}

/// <summary>
/// DTO for questionnaire search/filter criteria
/// </summary>
public class QuestionnaireSearchDto
{
    public string? SearchTerm { get; set; }
    public QuestionnaireType? Type { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsTemplate { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTime? StartDateFrom { get; set; }
    public DateTime? StartDateTo { get; set; }
    public DateTime? EndDateFrom { get; set; }
    public DateTime? EndDateTo { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Page must be at least 1")]
    public int Page { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100")] // VULN-021 FIX: Limit page size to prevent DoS
    public int PageSize { get; set; } = 20;

    public string? SortBy { get; set; } = "UpdatedAt";
    public bool SortDescending { get; set; } = true;
}

/// <summary>
/// DTO for paginated questionnaire results
/// </summary>
public class QuestionnaireSearchResultDto
{
    public List<QuestionnaireDto> Questionnaires { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}