using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.Entities;

/// <summary>
/// Represents a complete response to a questionnaire by a user
/// </summary>
public class QuestionnaireResponse
{
    public QuestionnaireResponse()
    {
        Id = Guid.NewGuid();
        QuestionResponses = new HashSet<QuestionResponse>();
    }

    /// <summary>
    /// Unique identifier for the response
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The questionnaire this response is for
    /// </summary>
    public Guid QuestionnaireId { get; set; }

    /// <summary>
    /// The user who submitted this response
    /// </summary>
    public Guid RespondentUserId { get; set; }

    /// <summary>
    /// Current status of the response
    /// </summary>
    public ResponseStatus Status { get; set; } = ResponseStatus.Draft;

    /// <summary>
    /// Whether this response has been submitted (vs. saved as draft)
    /// </summary>
    public bool IsSubmitted { get; set; } = false;

    /// <summary>
    /// Whether this response is complete (all required questions answered)
    /// </summary>
    public bool IsComplete { get; set; } = false;

    /// <summary>
    /// When the response was started
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the response was submitted
    /// </summary>
    public DateTime? SubmittedAt { get; set; }

    /// <summary>
    /// When the response was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// IP address from which the response was submitted
    /// </summary>
    [MaxLength(45)] // IPv6 max length
    public string? SubmittedFromIP { get; set; }

    /// <summary>
    /// User agent string from submission
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Additional metadata for the response
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Review notes (if applicable)
    /// </summary>
    [MaxLength(2000)]
    public string? ReviewNotes { get; set; }

    /// <summary>
    /// Who reviewed this response (if applicable)
    /// </summary>
    public Guid? ReviewedByUserId { get; set; }

    /// <summary>
    /// When the response was reviewed
    /// </summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>
    /// Navigation property to the questionnaire
    /// </summary>
    public virtual Questionnaire Questionnaire { get; set; } = null!;

    /// <summary>
    /// Navigation property to the respondent user
    /// </summary>
    public virtual User RespondentUser { get; set; } = null!;

    /// <summary>
    /// Navigation property to the reviewer user
    /// </summary>
    public virtual User? ReviewedByUser { get; set; }

    /// <summary>
    /// Collection of individual question responses
    /// </summary>
    public virtual ICollection<QuestionResponse> QuestionResponses { get; set; }

    /// <summary>
    /// Submit the response and mark as completed
    /// </summary>
    public void Submit(string? ipAddress = null, string? userAgent = null)
    {
        if (IsSubmitted)
            throw new InvalidOperationException("Response has already been submitted.");

        IsSubmitted = true;
        SubmittedAt = DateTime.UtcNow;
        Status = ResponseStatus.Submitted;
        SubmittedFromIP = ipAddress;
        UserAgent = userAgent;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Check if all required questions have been answered
    /// </summary>
    public bool CheckCompleteness()
    {
        if (Questionnaire?.Questions == null || !Questionnaire.Questions.Any())
            return true;

        var requiredQuestions = Questionnaire.Questions.Where(q => q.IsRequired && q.IsActive).ToList();
        var answeredQuestionIds = QuestionResponses.Where(r => !string.IsNullOrWhiteSpace(r.ResponseValue))
            .Select(r => r.QuestionId).ToHashSet();

        IsComplete = requiredQuestions.All(q => answeredQuestionIds.Contains(q.Id));
        return IsComplete;
    }

    /// <summary>
    /// Get the response for a specific question
    /// </summary>
    public QuestionResponse? GetResponseForQuestion(Guid questionId)
    {
        return QuestionResponses.FirstOrDefault(r => r.QuestionId == questionId);
    }

    /// <summary>
    /// Get the response value for a specific question
    /// </summary>
    public string? GetResponseValue(Guid questionId)
    {
        return GetResponseForQuestion(questionId)?.ResponseValue;
    }

    /// <summary>
    /// Calculate completion percentage
    /// </summary>
    public double GetCompletionPercentage()
    {
        if (Questionnaire?.Questions == null || !Questionnaire.Questions.Any())
            return 100.0;

        var totalQuestions = Questionnaire.Questions.Count(q => q.IsActive);
        if (totalQuestions == 0) return 100.0;

        var answeredQuestions = QuestionResponses.Count(r => !string.IsNullOrWhiteSpace(r.ResponseValue));
        return (double)answeredQuestions / totalQuestions * 100.0;
    }
}

/// <summary>
/// Status of a questionnaire response
/// </summary>
public enum ResponseStatus
{
    /// <summary>
    /// Response is being worked on but not submitted
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Response has been submitted
    /// </summary>
    Submitted = 1,

    /// <summary>
    /// Response is under review
    /// </summary>
    UnderReview = 2,

    /// <summary>
    /// Response has been approved
    /// </summary>
    Approved = 3,

    /// <summary>
    /// Response has been rejected
    /// </summary>
    Rejected = 4,

    /// <summary>
    /// Response requires revision
    /// </summary>
    NeedsRevision = 5
}