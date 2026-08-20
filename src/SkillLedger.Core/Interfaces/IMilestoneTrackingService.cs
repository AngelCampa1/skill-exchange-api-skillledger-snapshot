using SkillLedger.Core.DTOs;
using SkillLedger.Core.Enums;

namespace SkillLedger.Core.Interfaces;

/// <summary>
/// Service interface for milestone tracking and deliverable management
/// Provides project milestone creation, tracking, and payment integration
/// </summary>
public interface IMilestoneTrackingService
{
    #region Milestone Management

    /// <summary>
    /// Create a new milestone for a project
    /// </summary>
    /// <param name="request">Milestone creation request</param>
    /// <param name="createdByUserId">User creating the milestone</param>
    /// <param name="ipAddress">IP address of the request</param>
    /// <returns>Created milestone response</returns>
    Task<MilestoneResponseDto> CreateMilestoneAsync(CreateMilestoneRequestDto request, Guid createdByUserId, string? ipAddress = null);

    /// <summary>
    /// Get milestone by ID with all related data
    /// </summary>
    /// <param name="milestoneId">Milestone ID to retrieve</param>
    /// <returns>Milestone details or null if not found</returns>
    Task<MilestoneResponseDto?> GetMilestoneByIdAsync(Guid milestoneId);

    /// <summary>
    /// Update an existing milestone
    /// </summary>
    /// <param name="milestoneId">Milestone ID to update</param>
    /// <param name="request">Update request data</param>
    /// <param name="updatedByUserId">User performing the update</param>
    /// <returns>Updated milestone response or null if not found</returns>
    Task<MilestoneResponseDto?> UpdateMilestoneAsync(Guid milestoneId, UpdateMilestoneRequestDto request, Guid updatedByUserId);

    /// <summary>
    /// Delete a milestone
    /// </summary>
    /// <param name="milestoneId">Milestone ID to delete</param>
    /// <param name="deletedByUserId">User performing the deletion</param>
    /// <returns>True if deletion successful</returns>
    Task<bool> DeleteMilestoneAsync(Guid milestoneId, Guid deletedByUserId);

    /// <summary>
    /// Get milestones with filtering and pagination
    /// </summary>
    /// <param name="filter">Filter criteria</param>
    /// <returns>Paginated milestone results</returns>
    Task<PaginatedMilestonesDto> GetMilestonesAsync(MilestoneFilterDto filter, Guid? userId = null);

    /// <summary>
    /// Get project progress summary
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <returns>Project progress summary</returns>
    Task<ProjectProgressDto> GetProjectProgressAsync(Guid projectId, Guid? userId = null);

    #endregion

    #region Milestone Status Management

    /// <summary>
    /// Start work on a milestone
    /// </summary>
    /// <param name="milestoneId">Milestone ID</param>
    /// <param name="userId">User starting work</param>
    /// <returns>True if milestone was started successfully</returns>
    Task<bool> StartMilestoneAsync(Guid milestoneId, Guid userId);

    /// <summary>
    /// Submit milestone for review
    /// </summary>
    /// <param name="milestoneId">Milestone ID</param>
    /// <param name="userId">User submitting the milestone</param>
    /// <returns>True if submission successful</returns>
    Task<bool> SubmitMilestoneForReviewAsync(Guid milestoneId, Guid userId);

    /// <summary>
    /// Approve a milestone
    /// </summary>
    /// <param name="milestoneId">Milestone ID</param>
    /// <param name="approvedByUserId">User approving the milestone</param>
    /// <param name="reviewNotes">Optional review notes</param>
    /// <returns>True if approval successful</returns>
    Task<bool> ApproveMilestoneAsync(Guid milestoneId, Guid approvedByUserId, string? reviewNotes = null);

    /// <summary>
    /// Request revisions for a milestone
    /// </summary>
    /// <param name="milestoneId">Milestone ID</param>
    /// <param name="reviewedByUserId">User requesting revisions</param>
    /// <param name="reviewNotes">Required notes explaining revisions needed</param>
    /// <returns>True if revision request successful</returns>
    Task<bool> RequestMilestoneRevisionAsync(Guid milestoneId, Guid reviewedByUserId, string reviewNotes);

    /// <summary>
    /// Cancel a milestone
    /// </summary>
    /// <param name="milestoneId">Milestone ID</param>
    /// <param name="cancelledByUserId">User cancelling the milestone</param>
    /// <param name="reason">Optional cancellation reason</param>
    /// <returns>True if cancellation successful</returns>
    Task<bool> CancelMilestoneAsync(Guid milestoneId, Guid cancelledByUserId, string? reason = null);

    #endregion

    #region Deliverable Submission Management

    /// <summary>
    /// Create a submission for a milestone
    /// </summary>
    /// <param name="request">Submission creation request</param>
    /// <param name="submittedByUserId">User creating the submission</param>
    /// <param name="ipAddress">IP address of the request</param>
    /// <returns>Created submission response</returns>
    Task<SubmissionResponseDto> CreateSubmissionAsync(CreateSubmissionRequestDto request, Guid submittedByUserId, string? ipAddress = null);

    /// <summary>
    /// Get submission by ID
    /// </summary>
    /// <param name="submissionId">Submission ID</param>
    /// <returns>Submission details or null if not found</returns>
    Task<SubmissionResponseDto?> GetSubmissionByIdAsync(Guid submissionId, Guid? userId = null);

    /// <summary>
    /// Get all submissions for a milestone
    /// </summary>
    /// <param name="milestoneId">Milestone ID</param>
    /// <returns>List of submissions</returns>
    Task<List<SubmissionResponseDto>> GetMilestoneSubmissionsAsync(Guid milestoneId, Guid? userId = null);

    /// <summary>
    /// Review a milestone submission
    /// </summary>
    /// <param name="submissionId">Submission ID</param>
    /// <param name="request">Review request data</param>
    /// <param name="reviewedByUserId">User performing the review</param>
    /// <returns>True if review successful</returns>
    Task<bool> ReviewSubmissionAsync(Guid submissionId, ReviewSubmissionRequestDto request, Guid reviewedByUserId);

    #endregion

    #region Progress and Analytics

    /// <summary>
    /// Get overdue milestones for a user
    /// </summary>
    /// <param name="userId">User ID (optional - if null, gets all overdue)</param>
    /// <returns>List of overdue milestones</returns>
    Task<List<MilestoneResponseDto>> GetOverdueMilestonesAsync(Guid? userId = null);

    /// <summary>
    /// Get upcoming milestones due within specified days
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="daysAhead">Number of days to look ahead</param>
    /// <returns>List of upcoming milestones</returns>
    Task<List<MilestoneResponseDto>> GetUpcomingMilestonesAsync(Guid userId, int daysAhead = 7);

    #endregion

    #region Escrow Integration

    /// <summary>
    /// Link milestone to escrow milestone for payment triggers
    /// </summary>
    /// <param name="milestoneId">Milestone ID</param>
    /// <param name="escrowMilestoneId">Escrow milestone ID</param>
    /// <param name="linkedByUserId">User creating the link</param>
    /// <returns>True if linking successful</returns>
    Task<bool> LinkToEscrowMilestoneAsync(Guid milestoneId, Guid escrowMilestoneId, Guid linkedByUserId);

    /// <summary>
    /// Trigger payment release for approved milestone
    /// </summary>
    /// <param name="milestoneId">Milestone ID</param>
    /// <param name="triggeredByUserId">User triggering the payment</param>
    /// <returns>True if payment trigger successful</returns>
    Task<bool> TriggerPaymentReleaseAsync(Guid milestoneId, Guid triggeredByUserId);

    #endregion

    #region Security and Validation

    /// <summary>
    /// Validate user permissions for milestone operations
    /// </summary>
    /// <param name="milestoneId">Milestone ID</param>
    /// <param name="userId">User ID</param>
    /// <param name="operation">Operation being performed</param>
    /// <returns>True if user has permission</returns>
    Task<bool> ValidateUserPermissionsAsync(Guid milestoneId, Guid userId, string operation);

    #endregion
}
