using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;

namespace SkillLedger.Infrastructure.Services;

public class ProfileService : IProfileService
{
    private readonly SkillLedgerDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IFileStorageService _fileStorageService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ProfileService> _logger;

    // Cache TTL constants
    private static readonly TimeSpan ProfileCacheTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan PublicProfileCacheTtl = TimeSpan.FromMinutes(10);

    public ProfileService(
        SkillLedgerDbContext context,
        IAuditLogService auditLogService,
        IHttpContextAccessor httpContextAccessor,
        IFileStorageService fileStorageService,
        ICacheService cacheService,
        ILogger<ProfileService> logger)
    {
        _context = context;
        _auditLogService = auditLogService;
        _httpContextAccessor = httpContextAccessor;
        _fileStorageService = fileStorageService;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<ProfileResponseDto> CreateProfileAsync(Guid userId, CreateProfileDto createProfileDto)
    {
        try
        {
            // PERFORMANCE FIX: Use AsNoTracking for read-only existence checks
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return new ProfileResponseDto
                {
                    Success = false,
                    Message = "User not found"
                };
            }

            // Check if profile already exists
            var existingProfile = await _context.Profiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId);
            if (existingProfile != null)
            {
                return new ProfileResponseDto
                {
                    Success = false,
                    Message = "Profile already exists for this user"
                };
            }

            // Create new profile
            var profile = new Profile
            {
                UserId = userId,
                FirstName = createProfileDto.FirstName?.Trim(),
                LastName = createProfileDto.LastName?.Trim(),
                Title = createProfileDto.Title?.Trim(),
                Summary = createProfileDto.Summary?.Trim(),
                Company = createProfileDto.Company?.Trim(),
                WebsiteUrl = createProfileDto.WebsiteUrl?.Trim(),
                LinkedInUrl = createProfileDto.LinkedInUrl?.Trim(),
                GitHubUrl = createProfileDto.GitHubUrl?.Trim(),
                Location = createProfileDto.Location?.Trim(),
                TimeZone = createProfileDto.TimeZone?.Trim(),
                IsPublic = createProfileDto.IsPublic,
                Visibility = createProfileDto.Visibility,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Synchronize IsPublic with Visibility for backward compatibility
            SyncVisibilitySettings(profile);

            // Check if profile is complete
            profile.IsComplete = IsProfileComplete(profile);

            _context.Profiles.Add(profile);
            await _context.SaveChangesAsync();

            // Log the profile creation with IP and User-Agent
            var (ipAddress, userAgent) = GetRequestContext();
            await _auditLogService.LogEventAsync(
                userId,
                "PROFILE_CREATED",
                ipAddress,
                userAgent,
                true,
                $"User profile created with ID: {profile.Id}");

            var profileDto = MapToProfileDto(profile);

            // Cache the new profile
            await CacheProfileAsync(userId, profileDto);

            return new ProfileResponseDto
            {
                Success = true,
                Message = "Profile created successfully",
                Profile = profileDto
            };
        }
        catch (Exception ex)
        {
            var (ipAddress, userAgent) = GetRequestContext();
            await _auditLogService.LogEventAsync(
                userId,
                "PROFILE_CREATION_ERROR",
                ipAddress,
                userAgent,
                false,
                null,
                $"Failed to create profile: {ex.Message}");

            return new ProfileResponseDto
            {
                Success = false,
                Message = "An error occurred while creating the profile"
            };
        }
    }

    public async Task<ProfileResponseDto> UpdateProfileAsync(Guid userId, UpdateProfileDto updateProfileDto)
    {
        try
        {
            var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile == null)
            {
                return new ProfileResponseDto
                {
                    Success = false,
                    Message = "Profile not found"
                };
            }

            // Update profile fields
            profile.FirstName = updateProfileDto.FirstName?.Trim();
            profile.LastName = updateProfileDto.LastName?.Trim();
            profile.Title = updateProfileDto.Title?.Trim();
            profile.Summary = updateProfileDto.Summary?.Trim();
            profile.Company = updateProfileDto.Company?.Trim();
            profile.WebsiteUrl = updateProfileDto.WebsiteUrl?.Trim();
            profile.LinkedInUrl = updateProfileDto.LinkedInUrl?.Trim();
            profile.GitHubUrl = updateProfileDto.GitHubUrl?.Trim();
            profile.Location = updateProfileDto.Location?.Trim();
            profile.TimeZone = updateProfileDto.TimeZone?.Trim();
            profile.IsPublic = updateProfileDto.IsPublic;
            profile.Visibility = updateProfileDto.Visibility;
            profile.UpdatedAt = DateTime.UtcNow;

            // Synchronize IsPublic with Visibility for backward compatibility
            SyncVisibilitySettings(profile);

            // Check if profile is complete
            profile.IsComplete = IsProfileComplete(profile);

            await _context.SaveChangesAsync();

            // Invalidate cache
            await InvalidateProfileCacheAsync(userId);

            // Log the profile update with IP and User-Agent
            var (ipAddress, userAgent) = GetRequestContext();
            await _auditLogService.LogEventAsync(
                userId,
                "PROFILE_UPDATED",
                ipAddress,
                userAgent,
                true,
                $"User profile updated with ID: {profile.Id}");

            var profileDto = MapToProfileDto(profile);

            // Update cache with new data
            await CacheProfileAsync(userId, profileDto);

            return new ProfileResponseDto
            {
                Success = true,
                Message = "Profile updated successfully",
                Profile = profileDto
            };
        }
        catch (Exception ex)
        {
            var (ipAddress, userAgent) = GetRequestContext();
            await _auditLogService.LogEventAsync(
                userId,
                "PROFILE_UPDATE_ERROR",
                ipAddress,
                userAgent,
                false,
                null,
                $"Failed to update profile: {ex.Message}");

            return new ProfileResponseDto
            {
                Success = false,
                Message = "An error occurred while updating the profile"
            };
        }
    }

    public async Task<ProfileDto?> GetProfileByUserIdAsync(Guid userId, Guid? requestingUserId = null)
    {
        // Try to get from cache first
        var cacheKey = GetProfileCacheKey(userId);
        var cachedProfile = await _cacheService.GetAsync<ProfileDto>(cacheKey);
        if (cachedProfile != null)
        {
            _logger.LogDebug("Profile cache hit for user {UserId}", userId);
            return await ApplyPrivacyFilterAsync(cachedProfile, requestingUserId);
        }

        var profile = await _context.Profiles
            .Include(p => p.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
            return null;

        var profileDto = MapToProfileDto(profile);

        // Cache the profile
        await CacheProfileAsync(userId, profileDto);

        return await ApplyPrivacyFilterAsync(profileDto, requestingUserId);
    }

    private async Task<ProfileDto?> ApplyPrivacyFilterAsync(ProfileDto profile, Guid? requestingUserId)
    {
        if (profile == null)
            return null;

        var userId = profile.UserId;

        // Check privacy based on ProfileVisibility enum
        if (requestingUserId.HasValue && requestingUserId.Value != userId)
        {
            // Check visibility level authorization
            switch (profile.Visibility)
            {
                case ProfileVisibility.Private:
                    // Private profiles are never visible to others
                    return null;

                case ProfileVisibility.VerifiedUsersOnly:
                    // Only authenticated users can view (email verification no longer required)
                    var requestingUser = await _context.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Id == requestingUserId.Value);

                    if (requestingUser == null)
                        return null;
                    break;

                case ProfileVisibility.Internal:
                    // Only authenticated platform users can view (already checked by requestingUserId)
                    // No additional check needed
                    break;

                case ProfileVisibility.Public:
                    // Public profiles are visible to everyone
                    break;
            }
        }

        return profile;
    }

    public async Task<ProfileDto?> GetMyProfileAsync(Guid userId)
    {
        // Try to get from cache first
        var cacheKey = GetProfileCacheKey(userId);
        var cachedProfile = await _cacheService.GetAsync<ProfileDto>(cacheKey);
        if (cachedProfile != null)
        {
            _logger.LogDebug("Profile cache hit for user {UserId}", userId);
            return cachedProfile;
        }

        var profile = await _context.Profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
            return null;

        var profileDto = MapToProfileDto(profile);

        // BUG-001 FIX: Include user skills in response for profile completion check
        var userSkills = await _context.UserSkills
            .AsNoTracking()
            .Include(us => us.Skill)
            .Where(us => us.UserId == userId)
            .Select(us => new UserSkillDto
            {
                Id = us.Id,
                UserId = us.UserId,
                Skill = new SkillDto
                {
                    Id = us.Skill.Id,
                    Name = us.Skill.Name,
                    Description = us.Skill.Description,
                    Category = us.Skill.Category
                },
                Proficiency = us.Proficiency
            })
            .ToListAsync();

        profileDto.UserSkills = userSkills;

        // Cache the profile
        await CacheProfileAsync(userId, profileDto);

        return profileDto;
    }

    public async Task<ServiceResponseDto> DeleteProfileAsync(Guid userId)
    {
        try
        {
            var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile == null)
            {
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "Profile not found"
                };
            }

            _context.Profiles.Remove(profile);
            await _context.SaveChangesAsync();

            // Invalidate cache
            await InvalidateProfileCacheAsync(userId);

            // Log the profile deletion with IP and User-Agent
            var (ipAddress, userAgent) = GetRequestContext();
            await _auditLogService.LogEventAsync(
                userId,
                "PROFILE_DELETED",
                ipAddress,
                userAgent,
                true,
                $"User profile deleted with ID: {profile.Id}");

            return new ServiceResponseDto
            {
                Success = true,
                Message = "Profile deleted successfully"
            };
        }
        catch (Exception ex)
        {
            var (ipAddress, userAgent) = GetRequestContext();
            await _auditLogService.LogEventAsync(
                userId,
                "PROFILE_DELETION_ERROR",
                ipAddress,
                userAgent,
                false,
                null,
                $"Failed to delete profile: {ex.Message}");

            return new ServiceResponseDto
            {
                Success = false,
                Message = "An error occurred while deleting the profile"
            };
        }
    }

    public async Task<bool> HasCompleteProfileAsync(Guid userId)
    {
        var profile = await _context.Profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId);

        return profile?.IsComplete == true;
    }

    public async Task<ServiceResponseDto> UpdateAvatarAsync(Guid userId, string avatarUrl)
    {
        try
        {
            var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile == null)
            {
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "Profile not found"
                };
            }

            profile.AvatarUrl = avatarUrl?.Trim();
            profile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Invalidate cache
            await InvalidateProfileCacheAsync(userId);

            // Log the avatar update with IP and User-Agent
            var (ipAddress, userAgent) = GetRequestContext();
            await _auditLogService.LogEventAsync(
                userId,
                "AVATAR_UPDATED",
                ipAddress,
                userAgent,
                true,
                $"User avatar updated for profile ID: {profile.Id}");

            return new ServiceResponseDto
            {
                Success = true,
                Message = "Avatar updated successfully"
            };
        }
        catch (Exception ex)
        {
            var (ipAddress, userAgent) = GetRequestContext();
            await _auditLogService.LogEventAsync(
                userId,
                "AVATAR_UPDATE_ERROR",
                ipAddress,
                userAgent,
                false,
                null,
                $"Failed to update avatar: {ex.Message}");

            return new ServiceResponseDto
            {
                Success = false,
                Message = "An error occurred while updating the avatar"
            };
        }
    }

    public async Task<List<ProfileDto>> GetPublicProfilesAsync(string? searchTerm = null, int skip = 0, int take = 20)
    {
        // PERFORMANCE FIX: Use Select projection to only load needed fields instead of full entities
        var query = _context.Profiles
            .AsNoTracking()
            .Where(p => p.Visibility == ProfileVisibility.Public);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchTermLower = searchTerm.ToLower();
            query = query.Where(p =>
                (p.FirstName != null && p.FirstName.ToLower().Contains(searchTermLower)) ||
                (p.LastName != null && p.LastName.ToLower().Contains(searchTermLower)) ||
                (p.Title != null && p.Title.ToLower().Contains(searchTermLower)) ||
                (p.Company != null && p.Company.ToLower().Contains(searchTermLower)));
        }

        return await query
            .OrderBy(p => p.FirstName)
            .ThenBy(p => p.LastName)
            .Skip(skip)
            .Take(take)
            .Select(p => new ProfileDto
            {
                Id = p.Id,
                UserId = p.UserId,
                FirstName = p.FirstName,
                LastName = p.LastName,
                Title = p.Title,
                Summary = p.Summary,
                Company = p.Company,
                WebsiteUrl = p.WebsiteUrl,
                LinkedInUrl = p.LinkedInUrl,
                GitHubUrl = p.GitHubUrl,
                Location = p.Location,
                TimeZone = p.TimeZone,
                AvatarUrl = p.AvatarUrl,
                IsPublic = p.IsPublic,
                Visibility = p.Visibility,
                IsComplete = p.IsComplete,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync();
    }

    private static ProfileDto MapToProfileDto(Profile profile)
    {
        return new ProfileDto
        {
            Id = profile.Id,
            UserId = profile.UserId,
            FirstName = profile.FirstName,
            LastName = profile.LastName,
            Title = profile.Title,
            Summary = profile.Summary,
            Company = profile.Company,
            WebsiteUrl = profile.WebsiteUrl,
            LinkedInUrl = profile.LinkedInUrl,
            GitHubUrl = profile.GitHubUrl,
            Location = profile.Location,
            TimeZone = profile.TimeZone,
            AvatarUrl = profile.AvatarUrl,
            IsPublic = profile.IsPublic,
            Visibility = profile.Visibility,
            IsComplete = profile.IsComplete,
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt
        };
    }

    private static bool IsProfileComplete(Profile profile)
    {
        // A profile is considered complete if it has at least first name, last name, and title
        return !string.IsNullOrWhiteSpace(profile.FirstName) &&
               !string.IsNullOrWhiteSpace(profile.LastName) &&
               !string.IsNullOrWhiteSpace(profile.Title);
    }

    private (string ipAddress, string userAgent) GetRequestContext()
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext == null)
            return ("unknown", "unknown");

        var ipAddress = TrustedClientIpResolver.GetClientIpAddress(httpContext, "unknown");

        // Extract User-Agent
        var userAgent = httpContext.Request.Headers["User-Agent"].FirstOrDefault() ?? "unknown";

        return (ipAddress, userAgent);
    }

    public async Task<AvatarUploadResponse> SaveAvatarAsync(Guid userId, IFormFile file)
    {
        try
        {
            var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile == null)
            {
                return new AvatarUploadResponse
                {
                    Success = false,
                    Error = "Profile not found"
                };
            }

            // Validate file
            if (file == null || file.Length == 0)
            {
                return new AvatarUploadResponse
                {
                    Success = false,
                    Error = "No file provided"
                };
            }

            // Validate file size (5MB max)
            const long maxFileSize = 5 * 1024 * 1024;
            if (file.Length > maxFileSize)
            {
                return new AvatarUploadResponse
                {
                    Success = false,
                    Error = "File size must be less than 5MB"
                };
            }

            // Validate file type
            var allowedContentTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" };
            if (!allowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
            {
                return new AvatarUploadResponse
                {
                    Success = false,
                    Error = "Only JPEG, PNG, and WebP images are allowed"
                };
            }

            // Delete old avatar if exists
            if (!string.IsNullOrEmpty(profile.AvatarUrl))
            {
                await _fileStorageService.DeleteFileAsync(profile.AvatarUrl);
            }

            // Generate unique filename
            var fileExtension = Path.GetExtension(file.FileName);
            var safeFileName = $"avatar_{userId:N}_{DateTime.UtcNow:yyyyMMddHHmmss}{fileExtension}";

            // Upload file
            var uploadRequest = new FileStorageUploadRequest
            {
                FileName = safeFileName,
                FileStream = file.OpenReadStream(),
                ContentType = file.ContentType,
                FileSize = file.Length,
                ContainerPath = "profile-photos",
                Metadata = new Dictionary<string, string>
                {
                    { "userId", userId.ToString() },
                    { "uploadedAt", DateTime.UtcNow.ToString("O") },
                    { "originalFileName", file.FileName }
                }
            };

            var uploadResult = await _fileStorageService.UploadFileAsync(uploadRequest);

            if (!uploadResult.Success)
            {
                _logger.LogError("Failed to upload avatar for user {UserId}: {Error}", userId, uploadResult.ErrorMessage);
                return new AvatarUploadResponse
                {
                    Success = false,
                    Error = uploadResult.ErrorMessage ?? "Failed to upload file"
                };
            }

            // Update profile with new avatar URL
            profile.AvatarUrl = uploadResult.FilePath;
            profile.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Log the avatar update
            var (ipAddress, userAgent) = GetRequestContext();
            await _auditLogService.LogEventAsync(
                userId,
                "AVATAR_UPLOADED",
                ipAddress,
                userAgent,
                true,
                $"Avatar uploaded: {uploadResult.FilePath}");

            // BUG-HIGH-012 FIX: Malware scanning not yet implemented
            // Future enhancement: Integrate with Azure Defender or ClamAV for file scanning
            _logger.LogWarning("Malware scanning not yet integrated for avatar upload. User: {UserId}", userId);

            return new AvatarUploadResponse
            {
                Success = true,
                FileUrl = $"/uploads/{uploadResult.FilePath}",
                FileId = uploadResult.FilePath,
                ModerationStatus = "approved" // For now, auto-approve. In production, integrate content moderation
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving avatar for user {UserId}", userId);
            var (ipAddress, userAgent) = GetRequestContext();
            await _auditLogService.LogEventAsync(
                userId,
                "AVATAR_UPLOAD_ERROR",
                ipAddress,
                userAgent,
                false,
                null,
                $"Failed to save avatar: {ex.Message}");

            return new AvatarUploadResponse
            {
                Success = false,
                Error = "An error occurred while saving the avatar"
            };
        }
    }

    public async Task<ServiceResponseDto> DeleteAvatarAsync(Guid userId)
    {
        try
        {
            var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile == null)
            {
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "Profile not found"
                };
            }

            // Delete file from storage if exists
            if (!string.IsNullOrEmpty(profile.AvatarUrl))
            {
                var deleted = await _fileStorageService.DeleteFileAsync(profile.AvatarUrl);
                if (!deleted)
                {
                    _logger.LogWarning("Avatar file not found or could not be deleted: {AvatarUrl}", profile.AvatarUrl);
                }
            }

            // Clear avatar URL from profile
            profile.AvatarUrl = null;
            profile.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Log the avatar deletion
            var (ipAddress, userAgent) = GetRequestContext();
            await _auditLogService.LogEventAsync(
                userId,
                "AVATAR_DELETED",
                ipAddress,
                userAgent,
                true,
                "User avatar deleted");

            return new ServiceResponseDto
            {
                Success = true,
                Message = "Avatar deleted successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting avatar for user {UserId}", userId);
            var (ipAddress, userAgent) = GetRequestContext();
            await _auditLogService.LogEventAsync(
                userId,
                "AVATAR_DELETE_ERROR",
                ipAddress,
                userAgent,
                false,
                null,
                $"Failed to delete avatar: {ex.Message}");

            return new ServiceResponseDto
            {
                Success = false,
                Message = "An error occurred while deleting the avatar"
            };
        }
    }

    // Cache helper methods
    private static string GetProfileCacheKey(Guid userId) => $"profile:user:{userId}";

    private async Task CacheProfileAsync(Guid userId, ProfileDto profileDto)
    {
        try
        {
            var cacheKey = GetProfileCacheKey(userId);
            await _cacheService.SetAsync(cacheKey, profileDto, ProfileCacheTtl);
            _logger.LogDebug("Cached profile for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cache profile for user {UserId}", userId);
        }
    }

    private async Task InvalidateProfileCacheAsync(Guid userId)
    {
        try
        {
            var cacheKey = GetProfileCacheKey(userId);
            await _cacheService.RemoveAsync(cacheKey);
            _logger.LogDebug("Invalidated profile cache for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate profile cache for user {UserId}", userId);
        }
    }

    /// <summary>
    /// Synchronize the legacy IsPublic property with the Visibility enum for backward compatibility
    /// </summary>
    private static void SyncVisibilitySettings(Profile profile)
    {
        // If IsPublic is true, ensure Visibility is set to Public
        // If IsPublic is false, ensure Visibility is set to Private
        if (profile.IsPublic && profile.Visibility != ProfileVisibility.Public)
        {
            profile.Visibility = ProfileVisibility.Public;
        }
        else if (!profile.IsPublic && profile.Visibility == ProfileVisibility.Public)
        {
            profile.Visibility = ProfileVisibility.Private;
        }

        // Also sync the other direction: if Visibility is Public, ensure IsPublic is true
        if (profile.Visibility == ProfileVisibility.Public)
        {
            profile.IsPublic = true;
        }
        else if (profile.Visibility == ProfileVisibility.Private)
        {
            profile.IsPublic = false;
        }
    }
}
