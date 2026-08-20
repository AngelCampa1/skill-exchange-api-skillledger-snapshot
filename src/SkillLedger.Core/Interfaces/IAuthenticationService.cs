using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;

namespace SkillLedger.Core.Interfaces;

/// <summary>
/// Service for handling user authentication operations (Cookie-based)
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticate user with email and password using cookie authentication
    /// </summary>
    /// <param name="loginRequest">Login credentials</param>
    /// <param name="ipAddress">IP address of the login attempt</param>
    /// <param name="userAgent">User agent of the client</param>
    /// <returns>Login response if successful</returns>
    Task<LoginResponseDto> AuthenticateAsync(LoginRequestDto loginRequest, string? ipAddress = null, string? userAgent = null);

    /// <summary>
    /// Logout user by signing out of cookie authentication
    /// </summary>
    /// <param name="userId">ID of the user logging out</param>
    /// <param name="ipAddress">IP address of the logout request</param>
    /// <returns>Logout response</returns>
    Task<LogoutResponseDto> LogoutAsync(Guid userId, string? ipAddress = null);

    /// <summary>
    /// Logout user from all devices (for cookie-based auth, this is same as regular logout)
    /// </summary>
    /// <param name="userId">ID of the user to logout from all devices</param>
    /// <param name="ipAddress">IP address of the logout request</param>
    /// <returns>Logout response</returns>
    Task<LogoutResponseDto> LogoutFromAllDevicesAsync(Guid userId, string? ipAddress = null);

    /// <summary>
    /// Check if a user account is currently locked out
    /// </summary>
    /// <param name="userId">User ID to check</param>
    /// <returns>True if account is locked out</returns>
    Task<bool> IsAccountLockedAsync(Guid userId);

    /// <summary>
    /// Reset failed login attempts for a user (used after successful login)
    /// </summary>
    /// <param name="userId">User ID to reset failed attempts for</param>
    /// <returns>Task</returns>
    Task ResetFailedLoginAttemptsAsync(Guid userId);

    /// <summary>
    /// Get current user profile from authenticated HttpContext (cookie-based)
    /// </summary>
    /// <param name="userId">User ID from HttpContext.User claims</param>
    /// <returns>User profile data</returns>
    Task<UserProfileDto?> GetCurrentUserFromContextAsync(Guid userId);
}