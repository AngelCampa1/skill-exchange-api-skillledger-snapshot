using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;

namespace SkillLedger.Core.Interfaces;

/// <summary>
/// Service for managing user badges and verification
/// </summary>
public interface IBadgeService
{
    /// <summary>
    /// Check which badges a user is eligible to earn
    /// </summary>
    /// <param name="userId">User ID to check</param>
    /// <returns>List of eligible badges</returns>
    Task<List<BadgeProgressDto>> CheckBadgeEligibilityAsync(Guid userId);

    /// <summary>
    /// Get current progress towards all badges for a user
    /// </summary>
    /// <param name="userId">User ID to check</param>
    /// <returns>List of badge progress information</returns>
    Task<List<BadgeProgressDto>> GetBadgeProgressAsync(Guid userId);

    /// <summary>
    /// Award a badge to a user
    /// </summary>
    /// <param name="userId">User ID to award badge to</param>
    /// <param name="badgeType">Type of badge to award</param>
    /// <param name="evidence">Evidence/proof for badge earning</param>
    /// <param name="awardedBy">User ID of who awarded the badge (optional for automatic)</param>
    /// <returns>The awarded badge</returns>
    Task<UserBadge> AwardBadgeAsync(Guid userId, string badgeType, Dictionary<string, object>? evidence = null, Guid? awardedBy = null);

    /// <summary>
    /// Revoke a badge from a user
    /// </summary>
    /// <param name="badgeId">Badge ID to revoke</param>
    /// <param name="reason">Reason for revocation</param>
    /// <param name="revokedBy">User ID of who revoked the badge</param>
    Task RevokeBadgeAsync(Guid badgeId, string reason, Guid revokedBy);

    /// <summary>
    /// Get all badges for a user
    /// </summary>
    /// <param name="userId">User ID to get badges for</param>
    /// <param name="includeExpired">Whether to include expired badges</param>
    /// <returns>List of user badges</returns>
    Task<List<UserBadge>> GetUserBadgesAsync(Guid userId, bool includeExpired = false);

    /// <summary>
    /// Submit a verification request for manual badge verification
    /// </summary>
    /// <param name="userId">User ID requesting verification</param>
    /// <param name="badgeType">Type of badge to verify</param>
    /// <param name="evidence">Evidence submitted for verification</param>
    /// <returns>The verification request</returns>
    Task<VerificationRequest> SubmitVerificationRequestAsync(Guid userId, string badgeType, Dictionary<string, object> evidence);

    /// <summary>
    /// Process a verification request (approve/reject)
    /// </summary>
    /// <param name="requestId">Verification request ID</param>
    /// <param name="approved">Whether the request is approved</param>
    /// <param name="reviewNotes">Notes from the reviewer</param>
    /// <param name="reviewedBy">User ID of the reviewer</param>
    Task ProcessVerificationRequestAsync(Guid requestId, bool approved, string? reviewNotes, Guid reviewedBy);

    /// <summary>
    /// Get pending verification requests
    /// </summary>
    /// <param name="badgeType">Optional filter by badge type</param>
    /// <returns>List of pending verification requests</returns>
    Task<List<VerificationRequest>> GetPendingVerificationRequestsAsync(string? badgeType = null);

    /// <summary>
    /// Process automatic badge evaluation for all users
    /// </summary>
    /// <returns>Number of badges awarded</returns>
    Task<int> ProcessAutomaticBadgeEvaluationAsync();

    /// <summary>
    /// Process automatic badge evaluation for a specific user
    /// </summary>
    /// <param name="userId">User ID to evaluate</param>
    /// <returns>Number of badges awarded</returns>
    Task<int> ProcessAutomaticBadgeEvaluationAsync(Guid userId);

    /// <summary>
    /// Check for expired badges and handle expiration
    /// </summary>
    /// <returns>Number of badges expired</returns>
    Task<int> ProcessBadgeExpirationAsync();
}