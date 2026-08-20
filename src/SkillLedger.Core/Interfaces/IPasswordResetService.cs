using SkillLedger.Core.DTOs;

namespace SkillLedger.Core.Interfaces;

/// <summary>
/// Service for handling password reset functionality
/// </summary>
public interface IPasswordResetService
{
    /// <summary>
    /// Initiates a password reset request by sending a reset email
    /// </summary>
    /// <param name="email">Email address to send reset instructions to</param>
    /// <param name="ipAddress">IP address of the requesting client</param>
    /// <param name="userAgent">User agent of the requesting client</param>
    /// <returns>Service response indicating success or failure</returns>
    Task<ServiceResponseDto> InitiatePasswordResetAsync(string email, string ipAddress, string userAgent);

    /// <summary>
    /// Validates a password reset token
    /// </summary>
    /// <param name="token">Reset token to validate</param>
    /// <returns>True if token is valid and not expired</returns>
    Task<bool> ValidateResetTokenAsync(string token);

    /// <summary>
    /// Completes the password reset process with a new password
    /// </summary>
    /// <param name="token">Valid reset token</param>
    /// <param name="newPassword">New password to set</param>
    /// <param name="ipAddress">IP address of the requesting client</param>
    /// <param name="userAgent">User agent of the requesting client</param>
    /// <returns>Service response indicating success or failure</returns>
    Task<ServiceResponseDto> CompletePasswordResetAsync(string token, string newPassword, string ipAddress, string userAgent);

    /// <summary>
    /// Gets the remaining reset attempts for an email address
    /// </summary>
    /// <param name="email">Email address to check</param>
    /// <returns>Number of remaining attempts allowed</returns>
    Task<int> GetRemainingResetAttemptsAsync(string email);

    /// <summary>
    /// Checks if a password reset can be requested for an email
    /// </summary>
    /// <param name="email">Email address to check</param>
    /// <returns>True if reset can be requested, false if rate limited</returns>
    Task<bool> CanRequestPasswordResetAsync(string email);

    /// <summary>
    /// Cleans up expired password reset tokens
    /// </summary>
    /// <returns>Number of tokens cleaned up</returns>
    Task<int> CleanupExpiredTokensAsync();

    /// <summary>
    /// Revokes all active password reset tokens for a user
    /// </summary>
    /// <param name="userId">User ID to revoke tokens for</param>
    /// <param name="reason">Reason for revocation (for audit log)</param>
    /// <param name="excludeTokenId">Optional token ID to exclude from revocation</param>
    /// <returns>Number of tokens revoked</returns>
    Task<int> RevokeUserResetTokensAsync(Guid userId, string reason, Guid? excludeTokenId = null);
}