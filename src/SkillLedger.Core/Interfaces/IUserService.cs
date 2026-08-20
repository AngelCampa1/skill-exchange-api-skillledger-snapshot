using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;

namespace SkillLedger.Core.Interfaces;

public interface IUserService
{
    /// <summary>
    /// Registers a new user with secure password hashing
    /// </summary>
    /// <param name="registerDto">Registration details</param>
    /// <param name="ipAddress">IP address of the registration request</param>
    /// <param name="userAgent">User agent from the request</param>
    /// <returns>Registration result</returns>
    Task<RegisterUserResponseDto> RegisterUserAsync(RegisterUserDto registerDto, string ipAddress, string? userAgent = null);

    /// <summary>
    /// Checks if an email address is already registered (with enumeration protection)
    /// </summary>
    /// <param name="email">Email address to check</param>
    /// <returns>True if available (always returns true to prevent enumeration)</returns>
    Task<bool> IsEmailAvailableAsync(string email);

    /// <summary>
    /// Gets a user by ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>User entity or null</returns>
    Task<User?> GetUserByIdAsync(Guid userId);

    /// <summary>
    /// Gets a user by email address
    /// </summary>
    /// <param name="email">Email address</param>
    /// <returns>User entity or null</returns>
    Task<User?> GetUserByEmailAsync(string email);

    /// <summary>
    /// Updates user's email confirmed status (legacy method - email verification no longer required)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="isEmailVerified">Whether email is confirmed</param>
    /// <param name="ipAddress">IP address</param>
    /// <returns>True if successful</returns>
    Task<bool> UpdateEmailVerificationStatusAsync(Guid userId, bool isEmailVerified, string ipAddress);

    /// <summary>
    /// Updates a user's password with secure hashing
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="newPassword">New password to set</param>
    /// <returns>Service response indicating success or failure</returns>
    Task<ServiceResponseDto> UpdatePasswordAsync(Guid userId, string newPassword);
}