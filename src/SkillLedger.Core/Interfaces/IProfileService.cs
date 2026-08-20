using SkillLedger.Core.DTOs;

namespace SkillLedger.Core.Interfaces;

public interface IProfileService
{
    /// <summary>
    /// Creates a new profile for a user
    /// </summary>
    /// <param name="userId">The user ID to create profile for</param>
    /// <param name="createProfileDto">Profile creation data</param>
    /// <returns>Profile response with created profile data</returns>
    Task<ProfileResponseDto> CreateProfileAsync(Guid userId, CreateProfileDto createProfileDto);

    /// <summary>
    /// Updates an existing user profile
    /// </summary>
    /// <param name="userId">The user ID owning the profile</param>
    /// <param name="updateProfileDto">Profile update data</param>
    /// <returns>Profile response with updated profile data</returns>
    Task<ProfileResponseDto> UpdateProfileAsync(Guid userId, UpdateProfileDto updateProfileDto);

    /// <summary>
    /// Gets a user's profile by user ID
    /// </summary>
    /// <param name="userId">The user ID to get profile for</param>
    /// <param name="requestingUserId">The user ID making the request (for privacy checks)</param>
    /// <returns>Profile data if found and accessible</returns>
    Task<ProfileDto?> GetProfileByUserIdAsync(Guid userId, Guid? requestingUserId = null);

    /// <summary>
    /// Gets the current user's own profile
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>Profile data if found</returns>
    Task<ProfileDto?> GetMyProfileAsync(Guid userId);

    /// <summary>
    /// Deletes a user's profile
    /// </summary>
    /// <param name="userId">The user ID owning the profile</param>
    /// <returns>Service response indicating success or failure</returns>
    Task<ServiceResponseDto> DeleteProfileAsync(Guid userId);

    /// <summary>
    /// Checks if a user has a complete profile
    /// </summary>
    /// <param name="userId">The user ID to check</param>
    /// <returns>True if profile exists and is complete</returns>
    Task<bool> HasCompleteProfileAsync(Guid userId);

    /// <summary>
    /// Updates profile avatar URL
    /// </summary>
    /// <param name="userId">The user ID owning the profile</param>
    /// <param name="avatarUrl">New avatar URL</param>
    /// <returns>Service response indicating success or failure</returns>
    Task<ServiceResponseDto> UpdateAvatarAsync(Guid userId, string avatarUrl);

    /// <summary>
    /// Gets public profiles for search/discovery
    /// </summary>
    /// <param name="searchTerm">Optional search term to filter by name, title, or company</param>
    /// <param name="skip">Number of profiles to skip (for pagination)</param>
    /// <param name="take">Number of profiles to take (for pagination)</param>
    /// <returns>List of public profile data</returns>
    Task<List<ProfileDto>> GetPublicProfilesAsync(string? searchTerm = null, int skip = 0, int take = 20);
}
