using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace SkillLedger.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Cookie-based authentication only
[EnableRateLimiting("DefaultPolicy")]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;
    private readonly ILogger<ProfileController> _logger;
    private readonly IFileStorageService _fileStorageService;
    private readonly IAuditLogService _auditLogService;

    public ProfileController(
        IProfileService profileService,
        ILogger<ProfileController> logger,
        IFileStorageService fileStorageService,
        IAuditLogService auditLogService)
    {
        _profileService = profileService;
        _logger = logger;
        _fileStorageService = fileStorageService;
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// Creates a profile for the current user
    /// </summary>
    /// <param name="createProfileDto">Profile creation data</param>
    /// <returns>Profile creation response</returns>
    [HttpPost]
    [EnableRateLimiting("ProfileCreationPolicy")]
    public async Task<ActionResult<ProfileResponseDto>> CreateProfile([FromBody] CreateProfileDto createProfileDto)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new ProfileResponseDto
            {
                Success = false,
                Message = "User not authenticated"
            });
        }

        var result = await _profileService.CreateProfileAsync(userId.Value, createProfileDto);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetMyProfile), result);
    }

    /// <summary>
    /// Updates the current user's profile
    /// </summary>
    /// <param name="updateProfileDto">Profile update data</param>
    /// <returns>Profile update response</returns>
    [HttpPut]
    [EnableRateLimiting("ProfileUpdatePolicy")]
    public async Task<ActionResult<ProfileResponseDto>> UpdateProfile([FromBody] UpdateProfileDto updateProfileDto)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new ProfileResponseDto
            {
                Success = false,
                Message = "User not authenticated"
            });
        }

        var result = await _profileService.UpdateProfileAsync(userId.Value, updateProfileDto);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets the current user's profile
    /// </summary>
    /// <returns>Current user's profile data</returns>
    [HttpGet("me")]
    public async Task<ActionResult<ProfileDto>> GetMyProfile()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var profile = await _profileService.GetMyProfileAsync(userId.Value);
        if (profile == null)
        {
            return NotFound(new { message = "Profile not found" });
        }

        return Ok(profile);
    }

    /// <summary>
    /// Gets a user's profile by user ID (respects privacy settings)
    /// </summary>
    /// <param name="userId">The user ID to get profile for</param>
    /// <returns>User's profile data if accessible</returns>
    [HttpGet("user/{userId:guid}")]
    public async Task<ActionResult<ProfileDto>> GetUserProfile(Guid userId)
    {
        var requestingUserId = GetCurrentUserId();

        var profile = await _profileService.GetProfileByUserIdAsync(userId, requestingUserId);
        if (profile == null)
        {
            return NotFound(new { message = "Profile not found or not accessible" });
        }

        return Ok(profile);
    }

    /// <summary>
    /// Deletes the current user's profile
    /// </summary>
    /// <returns>Profile deletion response</returns>
    [HttpDelete]
    [EnableRateLimiting("ProfileDeletionPolicy")]
    public async Task<ActionResult<ServiceResponseDto>> DeleteProfile()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new ServiceResponseDto
            {
                Success = false,
                Message = "User not authenticated"
            });
        }

        var result = await _profileService.DeleteProfileAsync(userId.Value);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Updates the current user's profile avatar
    /// </summary>
    /// <param name="avatarRequest">Avatar URL data</param>
    /// <returns>Avatar update response</returns>
    [HttpPut("avatar")]
    [EnableRateLimiting("ProfileUpdatePolicy")]
    public async Task<ActionResult<ServiceResponseDto>> UpdateAvatar([FromBody] UpdateAvatarDto avatarRequest)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new ServiceResponseDto
            {
                Success = false,
                Message = "User not authenticated"
            });
        }

        var result = await _profileService.UpdateAvatarAsync(userId.Value, avatarRequest.AvatarUrl);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Uploads a profile avatar photo
    /// </summary>
    /// <param name="file">The image file to upload</param>
    /// <returns>Upload response with avatar URL</returns>
    [HttpPost("avatar/upload")]
    [EnableRateLimiting("ProfileUpdatePolicy")]
    [RequestSizeLimit(5_242_880)] // 5MB limit
    public async Task<ActionResult<AvatarUploadResponseDto>> UploadAvatar(IFormFile file)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new ServiceResponseDto
            {
                Success = false,
                Message = "User not authenticated"
            });
        }

        // Validate file presence
        if (file == null || file.Length == 0)
        {
            return BadRequest(new AvatarUploadResponseDto
            {
                Success = false,
                Error = "No file provided"
            });
        }

        // Validate file size (5MB max)
        const long maxFileSize = 5 * 1024 * 1024;
        if (file.Length > maxFileSize)
        {
            return BadRequest(new AvatarUploadResponseDto
            {
                Success = false,
                Error = "File size must be less than 5MB"
            });
        }

        // Validate file type
        var allowedContentTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" };
        if (!allowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
        {
            return BadRequest(new AvatarUploadResponseDto
            {
                Success = false,
                Error = "Only JPEG, PNG, and WebP images are allowed"
            });
        }

        try
        {
            // Get profile to ensure it exists
            var profile = await _profileService.GetMyProfileAsync(userId.Value);
            if (profile == null)
            {
                return BadRequest(new AvatarUploadResponseDto
                {
                    Success = false,
                    Error = "Profile not found"
                });
            }

            // Delete old avatar if exists
            if (!string.IsNullOrEmpty(profile.AvatarUrl))
            {
                await _fileStorageService.DeleteFileAsync(profile.AvatarUrl);
            }

            // Generate unique filename
            var fileExtension = Path.GetExtension(file.FileName);
            var safeFileName = $"avatar_{userId.Value:N}_{DateTime.UtcNow:yyyyMMddHHmmss}{fileExtension}";

            // Upload file
            // SECURITY FIX: Wrap stream in using to ensure proper disposal and prevent resource leaks
            using var fileStream = file.OpenReadStream();
            var uploadRequest = new FileStorageUploadRequest
            {
                FileName = safeFileName,
                FileStream = fileStream,
                ContentType = file.ContentType,
                FileSize = file.Length,
                ContainerPath = "profile-photos",
                Metadata = new Dictionary<string, string>
                {
                    { "userId", userId.Value.ToString() },
                    { "uploadedAt", DateTime.UtcNow.ToString("O") },
                    { "originalFileName", file.FileName }
                }
            };

            var uploadResult = await _fileStorageService.UploadFileAsync(uploadRequest);

            if (!uploadResult.Success)
            {
                _logger.LogError("Failed to upload avatar for user {UserId}: {Error}", userId, uploadResult.ErrorMessage);
                return BadRequest(new AvatarUploadResponseDto
                {
                    Success = false,
                    Error = uploadResult.ErrorMessage ?? "Failed to upload file"
                });
            }

            // Ensure FilePath is not null after successful upload
            if (string.IsNullOrEmpty(uploadResult.FilePath))
            {
                _logger.LogError("Upload succeeded but FilePath is null for user {UserId}", userId);
                return StatusCode(500, new AvatarUploadResponseDto
                {
                    Success = false,
                    Error = "Upload succeeded but file path is missing"
                });
            }

            // Update profile with new avatar URL
            var updateResult = await _profileService.UpdateAvatarAsync(userId.Value, uploadResult.FilePath);

            if (!updateResult.Success)
            {
                // Clean up uploaded file if profile update fails
                await _fileStorageService.DeleteFileAsync(uploadResult.FilePath);
                return BadRequest(new AvatarUploadResponseDto
                {
                    Success = false,
                    Error = updateResult.Message
                });
            }

            // Log the avatar update
            await _auditLogService.LogEventAsync(
                userId.Value,
                "AVATAR_UPLOADED",
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                HttpContext.Request.Headers["User-Agent"].FirstOrDefault() ?? "unknown",
                true,
                $"Avatar uploaded: {uploadResult.FilePath}");

            // BUG-HIGH-012 FIX: Malware scanning not yet implemented
            // Future enhancement: Integrate with Azure Defender or ClamAV for file scanning
            _logger.LogWarning("Malware scanning not yet integrated for avatar upload. User: {UserId}", userId);

            return Ok(new AvatarUploadResponseDto
            {
                Success = true,
                FileUrl = $"/uploads/{uploadResult.FilePath}",
                FileId = uploadResult.FilePath,
                ModerationStatus = "approved" // For now, auto-approve. In production, integrate content moderation
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading avatar for user {UserId}", userId);
            await _auditLogService.LogEventAsync(
                userId.Value,
                "AVATAR_UPLOAD_ERROR",
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                HttpContext.Request.Headers["User-Agent"].FirstOrDefault() ?? "unknown",
                false,
                null,
                $"Failed to save avatar: {ex.Message}");

            return StatusCode(500, new AvatarUploadResponseDto
            {
                Success = false,
                Error = "An error occurred while uploading the file"
            });
        }
    }

    /// <summary>
    /// Deletes the current user's profile avatar
    /// </summary>
    /// <returns>Deletion response</returns>
    [HttpDelete("avatar")]
    [EnableRateLimiting("ProfileUpdatePolicy")]
    public async Task<ActionResult<ServiceResponseDto>> DeleteAvatar()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new ServiceResponseDto
            {
                Success = false,
                Message = "User not authenticated"
            });
        }

        try
        {
            // Get profile to ensure it exists
            var profile = await _profileService.GetMyProfileAsync(userId.Value);
            if (profile == null)
            {
                return BadRequest(new ServiceResponseDto
                {
                    Success = false,
                    Message = "Profile not found"
                });
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
            var updateResult = await _profileService.UpdateAvatarAsync(userId.Value, string.Empty);

            if (!updateResult.Success)
            {
                return BadRequest(updateResult);
            }

            // Log the avatar deletion
            await _auditLogService.LogEventAsync(
                userId.Value,
                "AVATAR_DELETED",
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                HttpContext.Request.Headers["User-Agent"].FirstOrDefault() ?? "unknown",
                true,
                "User avatar deleted");

            return Ok(new ServiceResponseDto
            {
                Success = true,
                Message = "Avatar deleted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting avatar for user {UserId}", userId);
            await _auditLogService.LogEventAsync(
                userId.Value,
                "AVATAR_DELETE_ERROR",
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                HttpContext.Request.Headers["User-Agent"].FirstOrDefault() ?? "unknown",
                false,
                null,
                $"Failed to delete avatar: {ex.Message}");

            return StatusCode(500, new ServiceResponseDto
            {
                Success = false,
                Message = "An error occurred while deleting the avatar"
            });
        }
    }

    /// <summary>
    /// Gets public profiles for search/discovery
    /// </summary>
    /// <param name="searchTerm">Optional search term to filter profiles</param>
    /// <param name="skip">Number of profiles to skip (pagination)</param>
    /// <param name="take">Number of profiles to take (pagination, max 50)</param>
    /// <returns>List of public profiles</returns>
    [HttpGet("public")]
    [AllowAnonymous]
    [EnableRateLimiting("PublicProfileSearchPolicy")]
    public async Task<ActionResult<List<ProfileDto>>> GetPublicProfiles(
        [FromQuery] string? searchTerm = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20)
    {
        // Validate pagination parameters
        if (skip < 0)
            skip = 0;

        if (take <= 0 || take > 50)
            take = 20;

        var profiles = await _profileService.GetPublicProfilesAsync(searchTerm, skip, take);
        return Ok(profiles);
    }

    /// <summary>
    /// Checks if the current user has a complete profile
    /// </summary>
    /// <returns>Boolean indicating if profile is complete</returns>
    [HttpGet("complete")]
    public async Task<ActionResult<bool>> IsProfileComplete()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var isComplete = await _profileService.HasCompleteProfileAsync(userId.Value);
        return Ok(isComplete);
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        return null;
    }
}

public class UpdateAvatarDto
{
    /// <summary>
    /// Avatar URL
    /// </summary>
    [Url(ErrorMessage = "Invalid avatar URL format")]
    [MaxLength(500, ErrorMessage = "Avatar URL cannot exceed 500 characters")]
    public required string AvatarUrl { get; set; }
}

public class AvatarUploadResponseDto
{
    /// <summary>
    /// Success status
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Uploaded avatar URL
    /// </summary>
    public string? FileUrl { get; set; }

    /// <summary>
    /// File ID (path)
    /// </summary>
    public string? FileId { get; set; }

    /// <summary>
    /// Error message if upload failed
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Moderation status
    /// </summary>
    public string? ModerationStatus { get; set; }
}