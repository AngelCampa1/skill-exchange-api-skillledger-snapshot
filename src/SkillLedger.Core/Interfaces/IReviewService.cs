using SkillLedger.Core.DTOs;
using SkillLedger.Core.Enums;

namespace SkillLedger.Core.Interfaces;

/// <summary>
/// Service for managing project reviews with blind review system and temporal locks
/// </summary>
public interface IReviewService
{
    /// <summary>
    /// Submit a new project review with blind review logic
    /// </summary>
    /// <param name="createDto">Review details</param>
    /// <param name="reviewerId">ID of the user submitting the review</param>
    /// <param name="ipAddress">IP address of the reviewer</param>
    /// <returns>Review response with status information</returns>
    Task<ReviewResponseDto> SubmitReviewAsync(CreateReviewDto createDto, Guid reviewerId, string ipAddress);

    /// <summary>
    /// Get reviews for a specific project (only visible reviews)
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="requesterId">ID of the user requesting the reviews</param>
    /// <returns>List of visible reviews for the project</returns>
    Task<List<ReviewDisplayDto>> GetProjectReviewsAsync(Guid projectId, Guid requesterId);

    /// <summary>
    /// Get reviews received by a specific user (public profile view)
    /// </summary>
    /// <param name="userId">User ID to get reviews for</param>
    /// <param name="requesterId">ID of the user requesting the reviews</param>
    /// <param name="reviewType">Optional filter by review type</param>
    /// <param name="page">Page number for pagination</param>
    /// <param name="pageSize">Number of reviews per page</param>
    /// <returns>Paginated list of reviews received by the user</returns>
    Task<(List<ReviewDisplayDto> Reviews, int TotalCount)> GetUserReviewsAsync(
        Guid userId,
        Guid requesterId,
        ProjectReviewType? reviewType = null,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    /// Get review summary statistics for a user
    /// </summary>
    /// <param name="userId">User ID to get summary for</param>
    /// <returns>Review summary with ratings and counts</returns>
    Task<ReviewSummaryDto?> GetUserReviewSummaryAsync(Guid userId);

    /// <summary>
    /// Get blind review status for a project and user
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="userId">User ID to check status for</param>
    /// <returns>Blind review status information</returns>
    Task<BlindReviewStatusDto?> GetBlindReviewStatusAsync(Guid projectId, Guid userId);

    /// <summary>
    /// Retract a submitted review (only allowed in SubmittedBlind status)
    /// </summary>
    /// <param name="reviewId">Review ID to retract</param>
    /// <param name="userId">ID of the user attempting to retract</param>
    /// <param name="ipAddress">IP address of the requester</param>
    /// <returns>Success status</returns>
    Task<ReviewResponseDto> RetractReviewAsync(Guid reviewId, Guid userId, string ipAddress);

    /// <summary>
    /// Add a response to a published review
    /// </summary>
    /// <param name="responseDto">Response details</param>
    /// <param name="userId">ID of the user adding the response</param>
    /// <param name="ipAddress">IP address of the responder</param>
    /// <returns>Success status</returns>
    Task<ReviewResponseDto> AddReviewResponseAsync(AddReviewResponseDto responseDto, Guid userId, string ipAddress);

    /// <summary>
    /// Update photo attachments for a review (only allowed in Pending status)
    /// </summary>
    /// <param name="reviewId">Review ID</param>
    /// <param name="photoIds">List of photo file IDs to attach</param>
    /// <param name="userId">ID of the user updating attachments</param>
    /// <returns>Success status</returns>
    Task<ReviewResponseDto> UpdateReviewPhotosAsync(Guid reviewId, List<Guid> photoIds, Guid userId);

    /// <summary>
    /// Flag a review for content moderation
    /// </summary>
    /// <param name="reviewId">Review ID to flag</param>
    /// <param name="reason">Reason for flagging</param>
    /// <param name="reporterId">ID of the user reporting the review</param>
    /// <param name="ipAddress">IP address of the reporter</param>
    /// <returns>Success status</returns>
    Task<ReviewResponseDto> FlagReviewAsync(Guid reviewId, string reason, Guid reporterId, string ipAddress);

    /// <summary>
    /// Get a specific review by ID (with permission checks)
    /// </summary>
    /// <param name="reviewId">Review ID</param>
    /// <param name="requesterId">ID of the user requesting the review</param>
    /// <returns>Review details if authorized to view</returns>
    Task<ReviewDisplayDto?> GetReviewByIdAsync(Guid reviewId, Guid requesterId);

    /// <summary>
    /// Check if a user can review another user for a specific project
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="reviewerId">ID of the potential reviewer</param>
    /// <param name="revieweeId">ID of the potential reviewee</param>
    /// <param name="reviewType">Type of review</param>
    /// <returns>True if review is allowed</returns>
    Task<bool> CanSubmitReviewAsync(Guid projectId, Guid reviewerId, Guid revieweeId, ProjectReviewType reviewType);

    /// <summary>
    /// Process the blind review system - publish reviews when both parties have submitted
    /// </summary>
    /// <param name="projectId">Project ID to check</param>
    /// <returns>True if reviews were published</returns>
    Task<bool> ProcessBlindReviewsAsync(Guid projectId);

    /// <summary>
    /// Get user reviews with filtering and pagination
    /// </summary>
    /// <param name="userId">User ID to get reviews for</param>
    /// <param name="filter">Filter options for reviews</param>
    /// <returns>Paginated reviews with statistics</returns>
    Task<PaginatedReviewsDto> GetUserReviewsAsync(Guid userId, ReviewFilterDto filter);

    /// <summary>
    /// Get project reviews with submission status
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="userId">Current user ID</param>
    /// <returns>Project reviews with submission capabilities</returns>
    Task<ProjectReviewsDto> GetProjectReviewsWithStatusAsync(Guid projectId, Guid userId);

    /// <summary>
    /// Add response to a review
    /// </summary>
    /// <param name="reviewId">Review ID</param>
    /// <param name="response">Response text</param>
    /// <param name="userId">User ID adding response</param>
    /// <param name="ipAddress">IP address</param>
    /// <returns>Response result</returns>
    Task<ReviewResponseDto> AddReviewResponseAsync(Guid reviewId, string response, Guid userId, string ipAddress);

    /// <summary>
    /// Get review statistics for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Review statistics</returns>
    Task<ReviewSummaryDto> GetReviewStatisticsAsync(Guid userId);

    /// <summary>
    /// Upload evidence files for a review
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="files">Files to upload</param>
    /// <param name="userId">User ID uploading files</param>
    /// <param name="ipAddress">IP address</param>
    /// <returns>Upload result</returns>
    Task<FileUploadResultDto> UploadReviewEvidenceAsync(Guid projectId, List<object> files, Guid userId, string ipAddress);
}