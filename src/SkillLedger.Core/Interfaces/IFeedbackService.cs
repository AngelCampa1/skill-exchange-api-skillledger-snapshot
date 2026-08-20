using SkillLedger.Core.DTOs;

namespace SkillLedger.Core.Interfaces;

/// <summary>
/// Service interface for handling user feedback submissions
/// </summary>
public interface IFeedbackService
{
    /// <summary>
    /// Submit user feedback which will be sent via email to the admin
    /// </summary>
    /// <param name="dto">The feedback data</param>
    /// <param name="userId">Optional user ID if authenticated</param>
    /// <param name="userEmail">Optional user email if authenticated</param>
    /// <returns>True if feedback was submitted successfully</returns>
    Task<bool> SubmitFeedbackAsync(SubmitFeedbackDto dto, string? userId, string? userEmail);
}
