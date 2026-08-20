using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.Entities;

/// <summary>
/// Represents a dynamic intake questionnaire that can be customized for different purposes
/// </summary>
public class Questionnaire
{
    public Questionnaire()
    {
        Id = Guid.NewGuid();
        Questions = new HashSet<QuestionnaireQuestion>();
        Responses = new HashSet<QuestionnaireResponse>();
    }

    /// <summary>
    /// Unique identifier for the questionnaire
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The title/name of the questionnaire
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the questionnaire purpose
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// The user who created this questionnaire
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    /// <summary>
    /// The type/category of questionnaire
    /// </summary>
    public QuestionnaireType Type { get; set; } = QuestionnaireType.General;

    /// <summary>
    /// Whether the questionnaire is currently active and can be used
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether the questionnaire is a template that can be cloned
    /// </summary>
    public bool IsTemplate { get; set; } = false;

    /// <summary>
    /// Whether responses require review/approval
    /// </summary>
    public bool RequiresReview { get; set; } = false;

    /// <summary>
    /// Maximum number of responses allowed (null = unlimited)
    /// </summary>
    public int? MaxResponses { get; set; }

    /// <summary>
    /// When the questionnaire becomes available
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// When the questionnaire is no longer available
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Version number for questionnaire revisions
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// JSON metadata for additional configuration
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// When the questionnaire was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the questionnaire was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the user who created this questionnaire
    /// </summary>
    public virtual User CreatedByUser { get; set; } = null!;

    /// <summary>
    /// Collection of questions in this questionnaire
    /// </summary>
    public virtual ICollection<QuestionnaireQuestion> Questions { get; set; }

    /// <summary>
    /// Collection of responses to this questionnaire
    /// </summary>
    public virtual ICollection<QuestionnaireResponse> Responses { get; set; }

    /// <summary>
    /// Check if the questionnaire is currently available
    /// </summary>
    public bool IsAvailable()
    {
        if (!IsActive) return false;

        var now = DateTime.UtcNow;

        if (StartDate.HasValue && now < StartDate.Value) return false;
        if (EndDate.HasValue && now > EndDate.Value) return false;

        if (MaxResponses.HasValue && Responses.Count >= MaxResponses.Value) return false;

        return true;
    }

    /// <summary>
    /// Get the total number of questions in this questionnaire
    /// </summary>
    public int GetQuestionCount()
    {
        return Questions?.Count ?? 0;
    }

    /// <summary>
    /// Get the total number of responses received
    /// </summary>
    public int GetResponseCount()
    {
        return Responses?.Count ?? 0;
    }
}

/// <summary>
/// Types of questionnaires available in the system
/// </summary>
public enum QuestionnaireType
{
    /// <summary>
    /// General purpose questionnaire
    /// </summary>
    General = 0,

    /// <summary>
    /// Pre-project intake questionnaire
    /// </summary>
    ProjectIntake = 1,

    /// <summary>
    /// Client onboarding questionnaire
    /// </summary>
    ClientOnboarding = 2,

    /// <summary>
    /// Service provider vetting questionnaire
    /// </summary>
    ProviderVetting = 3,

    /// <summary>
    /// Project completion feedback questionnaire
    /// </summary>
    ProjectFeedback = 4,

    /// <summary>
    /// Skill assessment questionnaire
    /// </summary>
    SkillAssessment = 5,

    /// <summary>
    /// Market research questionnaire
    /// </summary>
    MarketResearch = 6
}