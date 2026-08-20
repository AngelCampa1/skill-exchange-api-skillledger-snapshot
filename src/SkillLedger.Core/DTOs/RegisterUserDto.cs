using System.ComponentModel.DataAnnotations;
using SkillLedger.Core.Validators;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Core.Entities;

namespace SkillLedger.Core.DTOs;

public class RegisterUserDto
{
    /// <summary>
    /// User's email address (must be unique)
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [StrictEmailAddress(ErrorMessage = "Invalid email format")]
    [MaxLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
    public required string Email { get; set; }

    /// <summary>
    /// User's password (must meet complexity requirements)
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [MinLength(12, ErrorMessage = "Password must be at least 12 characters long")]
    [MaxLength(128, ErrorMessage = "Password cannot exceed 128 characters")]
    public required string Password { get; set; }

    /// <summary>
    /// Password confirmation (must match password)
    /// </summary>
    [Required(ErrorMessage = "Password confirmation is required")]
    [Compare("Password", ErrorMessage = "Password and confirmation do not match")]
    public required string ConfirmPassword { get; set; }

    /// <summary>
    /// User's first name
    /// </summary>
    [Required(ErrorMessage = "First name is required")]
    [MinLength(1, ErrorMessage = "First name is required")]
    [MaxLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
    public required string FirstName { get; set; }

    /// <summary>
    /// User's last name
    /// </summary>
    [Required(ErrorMessage = "Last name is required")]
    [MinLength(1, ErrorMessage = "Last name is required")]
    [MaxLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
    public required string LastName { get; set; }

    /// <summary>
    /// Whether the user accepts the terms and conditions
    /// </summary>
    [Required(ErrorMessage = "Accepting terms and conditions is required")]
    public required bool AcceptedTerms { get; set; }
}

public class RegisterUserResponseDto
{
    /// <summary>
    /// ID of the newly created user
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// User's email address
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// Message indicating next steps
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Whether the registration was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// User profile information for immediate frontend use (if auto-login successful)
    /// </summary>
    public UserProfileDto? User { get; set; }
}

public class SendPhoneVerificationDto
{
    /// <summary>
    /// Phone number to send verification code to (E.164 format)
    /// </summary>
    [Required(ErrorMessage = "Phone number is required")]
    [Phone(ErrorMessage = "Invalid phone number format")]
    [MaxLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
    public required string PhoneNumber { get; set; }
}

public class VerifyPhoneDto
{
    /// <summary>
    /// Phone number being verified (E.164 format)
    /// </summary>
    [Required(ErrorMessage = "Phone number is required")]
    [Phone(ErrorMessage = "Invalid phone number format")]
    [MaxLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
    public required string PhoneNumber { get; set; }

    /// <summary>
    /// 6-digit verification code received via SMS
    /// </summary>
    [Required(ErrorMessage = "Verification code is required")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Verification code must be 6 digits")]
    public required string VerificationCode { get; set; }
}

public class PhoneVerificationResponseDto
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Response message
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// When the verification code expires (if sending)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}

// JWT Authentication DTOs

public class LoginRequestDto
{
    /// <summary>
    /// User's email address
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [StrictEmailAddress(ErrorMessage = "Invalid email format")]
    [MaxLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
    public required string Email { get; set; }

    /// <summary>
    /// User's password
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [MaxLength(128, ErrorMessage = "Password cannot exceed 128 characters")]
    public required string Password { get; set; }

    /// <summary>
    /// Whether to create a long-lived refresh token (Remember Me)
    /// </summary>
    public bool RememberMe { get; set; } = false;
}

public class LoginResponseDto
{
    /// <summary>
    /// Whether the login was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// User profile information (if successful)
    /// </summary>
    public UserProfileDto? User { get; set; }

    /// <summary>
    /// Login result message
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Whether account is locked out
    /// </summary>
    public bool IsLockedOut { get; set; } = false;
}

public class LogoutResponseDto
{
    /// <summary>
    /// Whether the logout was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Logout result message
    /// </summary>
    public required string Message { get; set; }
}

public class UserProfileDto
{
    /// <summary>
    /// User's unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User's email address
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// User's username
    /// </summary>
    public required string UserName { get; set; }

    /// <summary>
    /// User's first name (E2E-015 FIX: Added for display purposes)
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// User's last name (E2E-015 FIX: Added for display purposes)
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Whether the user's email has been verified
    /// </summary>
    public bool EmailVerified { get; set; }

    /// <summary>
    /// Whether the user's phone number has been verified
    /// </summary>
    public bool PhoneVerified { get; set; }

    /// <summary>
    /// Whether the user has completed tax compliance setup
    /// </summary>
    public bool TaxCompliant { get; set; }

    /// <summary>
    /// User's current status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// User's roles (for authorization)
    /// </summary>
    public List<string> Roles { get; set; } = new();

    /// <summary>
    /// User's effective permissions (from all roles)
    /// </summary>
    public List<string> Permissions { get; set; } = new();
}

// Password Reset DTOs

public class ForgotPasswordRequestDto
{
    /// <summary>
    /// Email address to send password reset instructions to
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [StrictEmailAddress(ErrorMessage = "Invalid email format")]
    [MaxLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
    public required string Email { get; set; }
}

public class ForgotPasswordResponseDto
{
    /// <summary>
    /// Whether the request was processed successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Response message (generic to prevent email enumeration)
    /// </summary>
    public required string Message { get; set; }
}

public class ResetPasswordRequestDto
{
    /// <summary>
    /// Password reset token from email
    /// </summary>
    [Required(ErrorMessage = "Reset token is required")]
    public required string Token { get; set; }

    /// <summary>
    /// New password (must meet complexity requirements)
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [MinLength(12, ErrorMessage = "Password must be at least 12 characters long")]
    [MaxLength(128, ErrorMessage = "Password cannot exceed 128 characters")]
    public required string NewPassword { get; set; }

    /// <summary>
    /// Password confirmation (must match new password)
    /// </summary>
    [Required(ErrorMessage = "Password confirmation is required")]
    [Compare("NewPassword", ErrorMessage = "Password and confirmation do not match")]
    public required string ConfirmPassword { get; set; }
}

public class ResetPasswordResponseDto
{
    /// <summary>
    /// Whether the password reset was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Response message
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Whether the token was invalid or expired
    /// </summary>
    public bool TokenExpired { get; set; } = false;
}

public class ServiceResponseDto
{
    /// <summary>
    /// Whether the service operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Response message
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Additional data (optional)
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// Error details (for debugging)
    /// </summary>
    public string? ErrorDetails { get; set; }
}

// RBAC DTOs

public class PermissionDto
{
    /// <summary>
    /// Unique identifier for the permission
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Permission name/key
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Human-readable description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Permission category/module
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Whether the permission is active
    /// </summary>
    public bool IsActive { get; set; }
}

public class CreateRoleDto
{
    /// <summary>
    /// Role name (unique)
    /// </summary>
    [Required(ErrorMessage = "Role name is required")]
    [MaxLength(256, ErrorMessage = "Role name cannot exceed 256 characters")]
    public required string Name { get; set; }

    /// <summary>
    /// Role description
    /// </summary>
    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// Role priority level
    /// </summary>
    [Range(0, 100, ErrorMessage = "Priority must be between 0 and 100")]
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Permission IDs to assign to this role
    /// </summary>
    public List<Guid> PermissionIds { get; set; } = new();
}

public class UpdateRoleDto
{
    /// <summary>
    /// Role description
    /// </summary>
    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// Role priority level
    /// </summary>
    [Range(0, 100, ErrorMessage = "Priority must be between 0 and 100")]
    public int Priority { get; set; }

    /// <summary>
    /// Whether the role is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Permission IDs to assign to this role (replaces existing permissions)
    /// </summary>
    public List<Guid> PermissionIds { get; set; } = new();
}

public class RoleDto
{
    /// <summary>
    /// Unique identifier for the role
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Role name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Role description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this is a system role
    /// </summary>
    public bool IsSystemRole { get; set; }

    /// <summary>
    /// Whether the role is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Role priority level
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// When the role was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the role was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Number of users assigned to this role
    /// </summary>
    public int UserCount { get; set; }
}

public class RoleWithPermissionsDto : RoleDto
{
    /// <summary>
    /// Permissions assigned to this role
    /// </summary>
    public List<PermissionDto> Permissions { get; set; } = new();
}

public class AssignRoleDto
{
    /// <summary>
    /// User ID to assign the role to
    /// </summary>
    [Required(ErrorMessage = "User ID is required")]
    public Guid UserId { get; set; }

    /// <summary>
    /// Role name to assign
    /// </summary>
    [Required(ErrorMessage = "Role name is required")]
    public required string RoleName { get; set; }
}

public class AssignPermissionDto
{
    /// <summary>
    /// Role name to assign the permission to
    /// </summary>
    [Required(ErrorMessage = "Role name is required")]
    public required string RoleName { get; set; }

    /// <summary>
    /// Permission name to assign
    /// </summary>
    [Required(ErrorMessage = "Permission name is required")]
    public required string PermissionName { get; set; }
}

public class UserWithRolesDto
{
    /// <summary>
    /// User's unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User's email address
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// User's username
    /// </summary>
    public required string UserName { get; set; }

    /// <summary>
    /// User's current status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// User's assigned roles
    /// </summary>
    public List<RoleDto> Roles { get; set; } = new();

    /// <summary>
    /// User's effective permissions (from all roles)
    /// </summary>
    public List<PermissionDto> Permissions { get; set; } = new();
}

// Profile Management DTOs

public class CreateProfileDto
{
    /// <summary>
    /// User's first name
    /// </summary>
    [MaxLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
    public string? FirstName { get; set; }

    /// <summary>
    /// User's last name
    /// </summary>
    [MaxLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
    public string? LastName { get; set; }

    /// <summary>
    /// Professional title or role
    /// </summary>
    [MaxLength(100, ErrorMessage = "Title cannot exceed 100 characters")]
    public string? Title { get; set; }

    /// <summary>
    /// Short professional summary/bio
    /// </summary>
    [MaxLength(500, ErrorMessage = "Summary cannot exceed 500 characters")]
    public string? Summary { get; set; }

    /// <summary>
    /// Company or organization name
    /// </summary>
    [MaxLength(100, ErrorMessage = "Company name cannot exceed 100 characters")]
    public string? Company { get; set; }

    /// <summary>
    /// Professional website URL (HTTPS only)
    /// </summary>
    [HttpsUrl]
    [MaxLength(255, ErrorMessage = "Website URL cannot exceed 255 characters")]
    public string? WebsiteUrl { get; set; }

    /// <summary>
    /// LinkedIn profile URL (HTTPS only)
    /// </summary>
    [HttpsUrl]
    [MaxLength(255, ErrorMessage = "LinkedIn URL cannot exceed 255 characters")]
    public string? LinkedInUrl { get; set; }

    /// <summary>
    /// GitHub profile URL (HTTPS only)
    /// </summary>
    [HttpsUrl]
    [MaxLength(255, ErrorMessage = "GitHub URL cannot exceed 255 characters")]
    public string? GitHubUrl { get; set; }

    /// <summary>
    /// Twitter profile URL (HTTPS only)
    /// </summary>
    [HttpsUrl]
    [MaxLength(255, ErrorMessage = "Twitter URL cannot exceed 255 characters")]
    public string? TwitterUrl { get; set; }

    /// <summary>
    /// Location (city, state/province, country)
    /// </summary>
    [MaxLength(100, ErrorMessage = "Location cannot exceed 100 characters")]
    public string? Location { get; set; }

    /// <summary>
    /// Time zone identifier (e.g., "America/New_York")
    /// </summary>
    [MaxLength(50, ErrorMessage = "Time zone cannot exceed 50 characters")]
    public string? TimeZone { get; set; }

    /// <summary>
    /// Whether the profile should be visible to other users (legacy field)
    /// </summary>
    public bool IsPublic { get; set; } = false;

    /// <summary>
    /// Profile visibility level
    /// </summary>
    public ProfileVisibility Visibility { get; set; } = ProfileVisibility.Public;
}

public class UpdateProfileDto
{
    /// <summary>
    /// User's first name
    /// </summary>
    [MaxLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
    public string? FirstName { get; set; }

    /// <summary>
    /// User's last name
    /// </summary>
    [MaxLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
    public string? LastName { get; set; }

    /// <summary>
    /// Professional title or role
    /// </summary>
    [MaxLength(100, ErrorMessage = "Title cannot exceed 100 characters")]
    public string? Title { get; set; }

    /// <summary>
    /// Short professional summary/bio
    /// </summary>
    [MaxLength(500, ErrorMessage = "Summary cannot exceed 500 characters")]
    public string? Summary { get; set; }

    /// <summary>
    /// Company or organization name
    /// </summary>
    [MaxLength(100, ErrorMessage = "Company name cannot exceed 100 characters")]
    public string? Company { get; set; }

    /// <summary>
    /// Professional website URL (HTTPS only)
    /// </summary>
    [HttpsUrl]
    [MaxLength(255, ErrorMessage = "Website URL cannot exceed 255 characters")]
    public string? WebsiteUrl { get; set; }

    /// <summary>
    /// LinkedIn profile URL (HTTPS only)
    /// </summary>
    [HttpsUrl]
    [MaxLength(255, ErrorMessage = "LinkedIn URL cannot exceed 255 characters")]
    public string? LinkedInUrl { get; set; }

    /// <summary>
    /// GitHub profile URL (HTTPS only)
    /// </summary>
    [HttpsUrl]
    [MaxLength(255, ErrorMessage = "GitHub URL cannot exceed 255 characters")]
    public string? GitHubUrl { get; set; }

    /// <summary>
    /// Twitter profile URL (HTTPS only)
    /// </summary>
    [HttpsUrl]
    [MaxLength(255, ErrorMessage = "Twitter URL cannot exceed 255 characters")]
    public string? TwitterUrl { get; set; }

    /// <summary>
    /// Location (city, state/province, country)
    /// </summary>
    [MaxLength(100, ErrorMessage = "Location cannot exceed 100 characters")]
    public string? Location { get; set; }

    /// <summary>
    /// Time zone identifier (e.g., "America/New_York")
    /// </summary>
    [MaxLength(50, ErrorMessage = "Time zone cannot exceed 50 characters")]
    public string? TimeZone { get; set; }

    /// <summary>
    /// Whether the profile should be visible to other users (legacy field)
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// Profile visibility level
    /// </summary>
    public ProfileVisibility Visibility { get; set; } = ProfileVisibility.Public;
}

public class ProfileDto
{
    /// <summary>
    /// Profile unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Associated user ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// User's first name
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// User's last name
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Full name (computed from first and last name)
    /// </summary>
    public string? FullName => string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName)
        ? null
        : $"{FirstName?.Trim()} {LastName?.Trim()}".Trim();

    /// <summary>
    /// Professional title or role
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Short professional summary/bio
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// Company or organization name
    /// </summary>
    public string? Company { get; set; }

    /// <summary>
    /// Professional website URL
    /// </summary>
    public string? WebsiteUrl { get; set; }

    /// <summary>
    /// LinkedIn profile URL
    /// </summary>
    public string? LinkedInUrl { get; set; }

    /// <summary>
    /// GitHub profile URL
    /// </summary>
    public string? GitHubUrl { get; set; }

    /// <summary>
    /// Location (city, state/province, country)
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Time zone identifier
    /// </summary>
    public string? TimeZone { get; set; }

    /// <summary>
    /// Profile avatar/photo URL
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// Whether the profile is visible to other users (legacy field)
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// Profile visibility level
    /// </summary>
    public ProfileVisibility Visibility { get; set; }

    /// <summary>
    /// Whether the profile is complete (all required fields filled)
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// When the profile was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the profile was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// User's skills (populated by service for profile completion check)
    /// </summary>
    public List<UserSkillDto>? UserSkills { get; set; }
}

public class AvatarUploadResponse
{
    public bool Success { get; set; }
    public string? FileUrl { get; set; }
    public string? FileId { get; set; }
    public string? Error { get; set; }
    public string? ModerationStatus { get; set; }
}

public class ProfileResponseDto
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Response message
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Profile data (if successful)
    /// </summary>
    public ProfileDto? Profile { get; set; }
}

// Skill Management DTOs

public class CreateSkillDto
{
    /// <summary>
    /// Name of the skill
    /// </summary>
    [Required(ErrorMessage = "Skill name is required")]
    [MaxLength(100, ErrorMessage = "Skill name cannot exceed 100 characters")]
    public required string Name { get; set; }

    /// <summary>
    /// Optional description of what this skill entails
    /// </summary>
    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// Category this skill belongs to (e.g., "Programming", "Design", "Marketing")
    /// </summary>
    [Required(ErrorMessage = "Skill category is required")]
    [MaxLength(50, ErrorMessage = "Category cannot exceed 50 characters")]
    public required string Category { get; set; }
}

public class UpdateSkillDto
{
    /// <summary>
    /// Name of the skill
    /// </summary>
    [MaxLength(100, ErrorMessage = "Skill name cannot exceed 100 characters")]
    public string? Name { get; set; }

    /// <summary>
    /// Optional description of what this skill entails
    /// </summary>
    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// Category this skill belongs to
    /// </summary>
    [MaxLength(50, ErrorMessage = "Category cannot exceed 50 characters")]
    public string? Category { get; set; }

    /// <summary>
    /// Whether this skill is active and available for use
    /// </summary>
    public bool IsActive { get; set; } = true;
}

public class SkillDto
{
    /// <summary>
    /// Unique identifier for the skill
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Name of the skill
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Optional description of what this skill entails
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Category this skill belongs to
    /// </summary>
    public required string Category { get; set; }

    /// <summary>
    /// Whether this skill is pre-approved and managed by the system
    /// </summary>
    public bool IsSystemManaged { get; set; }

    /// <summary>
    /// Whether this skill is active and available for use
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// When the skill was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the skill was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Number of users who have this skill
    /// </summary>
    public int UserCount { get; set; } = 0;

    /// <summary>
    /// Number of endorsements for this skill across all users
    /// </summary>
    public int EndorsementCount { get; set; } = 0;
}

public class AddUserSkillDto
{
    /// <summary>
    /// Associated skill ID
    /// </summary>
    [Required(ErrorMessage = "Skill ID is required")]
    public Guid SkillId { get; set; }

    /// <summary>
    /// User's proficiency level with this skill
    /// </summary>
    [Required(ErrorMessage = "Proficiency level is required")]
    public SkillProficiency Proficiency { get; set; } = SkillProficiency.Beginner;

    /// <summary>
    /// Years of experience with this skill
    /// </summary>
    [Range(0, 100, ErrorMessage = "Years of experience must be between 0 and 100")]
    public int YearsOfExperience { get; set; } = 0;

    /// <summary>
    /// User's self-assessment notes about their experience with this skill
    /// </summary>
    [MaxLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string? Notes { get; set; }

    /// <summary>
    /// Whether this skill is featured prominently on the user's profile
    /// </summary>
    public bool IsFeatured { get; set; } = false;

    /// <summary>
    /// Whether this skill is visible to other users
    /// </summary>
    public bool IsVisible { get; set; } = true;
}

public class UpdateUserSkillDto
{
    /// <summary>
    /// User's proficiency level with this skill
    /// </summary>
    public SkillProficiency? Proficiency { get; set; }

    /// <summary>
    /// Years of experience with this skill
    /// </summary>
    [Range(0, 100, ErrorMessage = "Years of experience must be between 0 and 100")]
    public int? YearsOfExperience { get; set; }

    /// <summary>
    /// User's self-assessment notes about their experience with this skill
    /// </summary>
    [MaxLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string? Notes { get; set; }

    /// <summary>
    /// Whether this skill is featured prominently on the user's profile
    /// </summary>
    public bool? IsFeatured { get; set; }

    /// <summary>
    /// Whether this skill is visible to other users
    /// </summary>
    public bool? IsVisible { get; set; }
}

public class UserSkillDto
{
    /// <summary>
    /// Unique identifier for the user skill
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Associated user ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Associated skill information
    /// </summary>
    public required SkillDto Skill { get; set; }

    /// <summary>
    /// User's proficiency level with this skill
    /// </summary>
    public SkillProficiency Proficiency { get; set; }

    /// <summary>
    /// Human-readable proficiency level
    /// </summary>
    public string ProficiencyDisplay => Proficiency.ToString();

    /// <summary>
    /// Years of experience with this skill
    /// </summary>
    public int YearsOfExperience { get; set; }

    /// <summary>
    /// User's self-assessment notes about their experience with this skill
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Whether this skill is featured prominently on the user's profile
    /// </summary>
    public bool IsFeatured { get; set; }

    /// <summary>
    /// Whether this skill is visible to other users
    /// </summary>
    public bool IsVisible { get; set; }

    /// <summary>
    /// When the user added this skill
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the user last updated this skill
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Number of endorsements received for this skill
    /// </summary>
    public int EndorsementCount { get; set; } = 0;

    /// <summary>
    /// Endorsements received for this skill (if included)
    /// </summary>
    public List<SkillEndorsementDto> Endorsements { get; set; } = new();
}

public class CreateSkillEndorsementDto
{
    /// <summary>
    /// Associated user skill ID - the skill being endorsed
    /// </summary>
    [Required(ErrorMessage = "User skill ID is required")]
    public Guid UserSkillId { get; set; }

    /// <summary>
    /// Optional comment or note about the endorsement
    /// </summary>
    [MaxLength(500, ErrorMessage = "Comment cannot exceed 500 characters")]
    public string? Comment { get; set; }

    /// <summary>
    /// Review text for the endorsement (alias for Comment for backward compatibility)
    /// </summary>
    [MaxLength(500, ErrorMessage = "Review text cannot exceed 500 characters")]
    public string? ReviewText
    {
        get => Comment;
        set => Comment = value;
    }

    /// <summary>
    /// Whether this endorsement is visible to other users
    /// </summary>
    public bool IsVisible { get; set; } = true;
}

public class SkillEndorsementDto
{
    /// <summary>
    /// Unique identifier for the skill endorsement
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Associated user skill ID
    /// </summary>
    public Guid UserSkillId { get; set; }

    /// <summary>
    /// User who gave the endorsement
    /// </summary>
    public required UserSummaryDto EndorsedByUser { get; set; }

    /// <summary>
    /// Optional comment or note about the endorsement
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// Review text for the endorsement (alias for Comment for backward compatibility)
    /// </summary>
    public string? ReviewText
    {
        get => Comment;
        set => Comment = value;
    }

    /// <summary>
    /// Whether this endorsement is visible to other users
    /// </summary>
    public bool IsVisible { get; set; }

    /// <summary>
    /// When the endorsement was given
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

// Experience Management DTOs

[ExperienceDateRange]
public class CreateExperienceDto
{
    /// <summary>
    /// Type of experience (Work, Education, Project, Volunteer, etc.)
    /// </summary>
    [Required(ErrorMessage = "Experience type is required")]
    public ExperienceType Type { get; set; } = ExperienceType.Work;

    /// <summary>
    /// Job title, degree, project role, etc.
    /// </summary>
    [Required(ErrorMessage = "Title is required")]
    [MaxLength(100, ErrorMessage = "Title cannot exceed 100 characters")]
    public required string Title { get; set; }

    /// <summary>
    /// Company, school, organization, or project name
    /// </summary>
    [Required(ErrorMessage = "Organization is required")]
    [MaxLength(100, ErrorMessage = "Organization cannot exceed 100 characters")]
    public required string Organization { get; set; }

    /// <summary>
    /// Location where this experience took place
    /// </summary>
    [MaxLength(100, ErrorMessage = "Location cannot exceed 100 characters")]
    public string? Location { get; set; }

    /// <summary>
    /// Detailed description of the experience, responsibilities, achievements
    /// </summary>
    [MaxLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// Start date of the experience
    /// </summary>
    [Required(ErrorMessage = "Start date is required")]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date of the experience (null if current/ongoing)
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Whether this experience is currently ongoing
    /// </summary>
    public bool IsCurrent { get; set; } = false;

    /// <summary>
    /// Whether this experience is visible on the user's public profile
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Whether this experience is featured prominently on the user's profile
    /// </summary>
    public bool IsFeatured { get; set; } = false;

    /// <summary>
    /// Skills used in this experience
    /// </summary>
    public List<Guid> SkillIds { get; set; } = new();
}

[ExperienceDateRange]
public class UpdateExperienceDto
{
    /// <summary>
    /// Type of experience (Work, Education, Project, Volunteer, etc.)
    /// </summary>
    public ExperienceType? Type { get; set; }

    /// <summary>
    /// Job title, degree, project role, etc.
    /// </summary>
    [MaxLength(100, ErrorMessage = "Title cannot exceed 100 characters")]
    public string? Title { get; set; }

    /// <summary>
    /// Company, school, organization, or project name
    /// </summary>
    [MaxLength(100, ErrorMessage = "Organization cannot exceed 100 characters")]
    public string? Organization { get; set; }

    /// <summary>
    /// Location where this experience took place
    /// </summary>
    [MaxLength(100, ErrorMessage = "Location cannot exceed 100 characters")]
    public string? Location { get; set; }

    /// <summary>
    /// Detailed description of the experience, responsibilities, achievements
    /// </summary>
    [MaxLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// Start date of the experience
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date of the experience (null if current/ongoing)
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Whether this experience is currently ongoing
    /// </summary>
    public bool? IsCurrent { get; set; }

    /// <summary>
    /// Whether this experience is visible on the user's public profile
    /// </summary>
    public bool? IsVisible { get; set; }

    /// <summary>
    /// Whether this experience is featured prominently on the user's profile
    /// </summary>
    public bool? IsFeatured { get; set; }

    /// <summary>
    /// Display order for this experience in lists
    /// </summary>
    public int? DisplayOrder { get; set; }

    /// <summary>
    /// Skills used in this experience (replaces existing skills)
    /// </summary>
    public List<Guid>? SkillIds { get; set; }
}

public class ExperienceDto
{
    /// <summary>
    /// Unique identifier for the experience
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Associated user ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Type of experience
    /// </summary>
    public ExperienceType Type { get; set; }

    /// <summary>
    /// Human-readable experience type
    /// </summary>
    public string TypeDisplay => Type.ToString();

    /// <summary>
    /// Job title, degree, project role, etc.
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Company, school, organization, or project name
    /// </summary>
    public required string Organization { get; set; }

    /// <summary>
    /// Location where this experience took place
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Detailed description of the experience, responsibilities, achievements
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Start date of the experience
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date of the experience (null if current/ongoing)
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Whether this experience is currently ongoing
    /// </summary>
    public bool IsCurrent { get; set; }

    /// <summary>
    /// Whether this experience is visible on the user's public profile
    /// </summary>
    public bool IsVisible { get; set; }

    /// <summary>
    /// Whether this experience is featured prominently on the user's profile
    /// </summary>
    public bool IsFeatured { get; set; }

    /// <summary>
    /// Display order for this experience in lists
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// When the experience entry was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the experience entry was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Skills used in this experience
    /// </summary>
    public List<SkillDto> Skills { get; set; } = new();

    /// <summary>
    /// Duration in months (calculated from start and end dates)
    /// </summary>
    public int DurationInMonths
    {
        get
        {
            var endDate = EndDate ?? DateTime.UtcNow;
            var startDate = StartDate;
            return ((endDate.Year - startDate.Year) * 12) + endDate.Month - startDate.Month;
        }
    }

    /// <summary>
    /// Formatted duration string (e.g., "2 years 3 months")
    /// </summary>
    public string DurationDisplay
    {
        get
        {
            var months = DurationInMonths;
            if (months == 0) return "Less than a month";

            var years = months / 12;
            var remainingMonths = months % 12;

            if (years == 0) return $"{months} month{(months == 1 ? "" : "s")}";
            if (remainingMonths == 0) return $"{years} year{(years == 1 ? "" : "s")}";

            return $"{years} year{(years == 1 ? "" : "s")} {remainingMonths} month{(remainingMonths == 1 ? "" : "s")}";
        }
    }
}

public class UserSummaryDto
{
    /// <summary>
    /// User's unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User's display name (from profile if available, otherwise email)
    /// </summary>
    public required string DisplayName { get; set; }

    /// <summary>
    /// User's email address
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// User's username
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// User's first name (from profile)
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// User's last name (from profile)
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// User's professional title
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// User's company
    /// </summary>
    public string? Company { get; set; }

    /// <summary>
    /// User's location
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// User's avatar URL
    /// </summary>
    public string? AvatarUrl { get; set; }
}

public class SkillSearchDto
{
    /// <summary>
    /// Search query for skill name or description
    /// </summary>
    public string? Query { get; set; }

    /// <summary>
    /// Filter by skill category
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Whether to include only active skills
    /// </summary>
    public bool ActiveOnly { get; set; } = true;

    /// <summary>
    /// Whether to include only system-managed skills
    /// </summary>
    public bool? SystemManagedOnly { get; set; }

    /// <summary>
    /// Number of skills to skip (for pagination)
    /// </summary>
    public int Skip { get; set; } = 0;

    /// <summary>
    /// Number of skills to take (for pagination)
    /// </summary>
    [Range(1, 100, ErrorMessage = "Take must be between 1 and 100")]
    public int Take { get; set; } = 20;
}

public class UserSkillSearchDto
{
    /// <summary>
    /// User ID to search skills for
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Search query for skill name or notes
    /// </summary>
    public string? Query { get; set; }

    /// <summary>
    /// Filter by skill category
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Filter by proficiency level
    /// </summary>
    public SkillProficiency? Proficiency { get; set; }

    /// <summary>
    /// Whether to include only featured skills
    /// </summary>
    public bool? FeaturedOnly { get; set; }

    /// <summary>
    /// Whether to include only visible skills
    /// </summary>
    public bool VisibleOnly { get; set; } = true;

    /// <summary>
    /// Whether to include endorsements
    /// </summary>
    public bool IncludeEndorsements { get; set; } = false;

    /// <summary>
    /// Number of skills to skip (for pagination)
    /// </summary>
    public int Skip { get; set; } = 0;

    /// <summary>
    /// Number of skills to take (for pagination)
    /// </summary>
    [Range(1, 100, ErrorMessage = "Take must be between 1 and 100")]
    public int Take { get; set; } = 20;
}

public class ExperienceSearchDto
{
    /// <summary>
    /// User ID to search experiences for
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Search query for title, organization, or description
    /// </summary>
    public string? Query { get; set; }

    /// <summary>
    /// Filter by experience type
    /// </summary>
    public ExperienceType? Type { get; set; }

    /// <summary>
    /// Whether to include only current experiences
    /// </summary>
    public bool? CurrentOnly { get; set; }

    /// <summary>
    /// Whether to include only featured experiences
    /// </summary>
    public bool? FeaturedOnly { get; set; }

    /// <summary>
    /// Whether to include only visible experiences
    /// </summary>
    public bool VisibleOnly { get; set; } = true;

    /// <summary>
    /// Whether to include skills used in experiences
    /// </summary>
    public bool IncludeSkills { get; set; } = false;

    /// <summary>
    /// Number of experiences to skip (for pagination)
    /// </summary>
    public int Skip { get; set; } = 0;

    /// <summary>
    /// Number of experiences to take (for pagination)
    /// </summary>
    [Range(1, 100, ErrorMessage = "Take must be between 1 and 100")]
    public int Take { get; set; } = 20;
}

public class SkillCategoryDto
{
    /// <summary>
    /// Category name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Number of skills in this category
    /// </summary>
    public int SkillCount { get; set; }

    /// <summary>
    /// Number of users with skills in this category
    /// </summary>
    public int UserCount { get; set; }
}

// Project Management DTOs

public class CreateProjectDto
{
    /// <summary>
    /// Project title (max 100 characters)
    /// </summary>
    [Required(ErrorMessage = "Project title is required")]
    [MaxLength(100, ErrorMessage = "Title cannot exceed 100 characters")]
    public required string Title { get; set; }

    /// <summary>
    /// Rich text description with XSS protection (max 5000 characters)
    /// </summary>
    [Required(ErrorMessage = "Project description is required")]
    [MaxLength(5000, ErrorMessage = "Description cannot exceed 5000 characters")]
    public required string Description { get; set; }

    /// <summary>
    /// Credit budget for the project (50-50000 credits)
    /// BUG-002 FIX: Updated range to match enterprise limits in ProjectService validation
    /// </summary>
    [Required(ErrorMessage = "Credit budget is required")]
    [Range(50, 50000, ErrorMessage = "Credit budget must be between 50 and 50,000 credits")]
    public int CreditBudget { get; set; }

    /// <summary>
    /// When the project work should start
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// When the project should be completed (must be in future)
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Project deliverables (min 1, max 10 items)
    /// </summary>
    [Required(ErrorMessage = "At least one deliverable is required")]
    [MinLength(1, ErrorMessage = "At least one deliverable is required")]
    [MaxLength(10, ErrorMessage = "Cannot exceed 10 deliverables")]
    public List<CreateProjectDeliverableDto> Deliverables { get; set; } = new();

    /// <summary>
    /// Required skills for the project (min 1, max 5 skills)
    /// </summary>
    [Required(ErrorMessage = "At least one skill is required")]
    [MinLength(1, ErrorMessage = "At least one skill is required")]
    [MaxLength(5, ErrorMessage = "Cannot exceed 5 skills")]
    public List<CreateProjectSkillDto> RequiredSkills { get; set; } = new();
}

public class UpdateProjectDto
{
    /// <summary>
    /// Project title (max 100 characters)
    /// </summary>
    [MaxLength(100, ErrorMessage = "Title cannot exceed 100 characters")]
    public string? Title { get; set; }

    /// <summary>
    /// Rich text description with XSS protection (max 5000 characters)
    /// </summary>
    [MaxLength(5000, ErrorMessage = "Description cannot exceed 5000 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// Credit budget for the project (50-5000 credits)
    /// </summary>
    [Range(50, 5000, ErrorMessage = "Credit budget must be between 50 and 5000 credits")]
    public int? CreditBudget { get; set; }

    /// <summary>
    /// When the project work should start
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// When the project should be completed (must be in future)
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Project deliverables (min 1, max 10 items) - replaces existing deliverables
    /// </summary>
    [MinLength(1, ErrorMessage = "At least one deliverable is required")]
    [MaxLength(10, ErrorMessage = "Cannot exceed 10 deliverables")]
    public List<CreateProjectDeliverableDto>? Deliverables { get; set; }

    /// <summary>
    /// Required skills for the project (min 1, max 5 skills) - replaces existing skills
    /// </summary>
    [MinLength(1, ErrorMessage = "At least one skill is required")]
    [MaxLength(5, ErrorMessage = "Cannot exceed 5 skills")]
    public List<CreateProjectSkillDto>? RequiredSkills { get; set; }
}

public class CreateProjectDeliverableDto
{
    /// <summary>
    /// Description of what needs to be delivered (max 500 characters)
    /// </summary>
    [Required(ErrorMessage = "Deliverable description is required")]
    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public required string Description { get; set; }

    /// <summary>
    /// Order index for displaying deliverables in sequence
    /// </summary>
    [Range(0, 100, ErrorMessage = "Order index must be between 0 and 100")]
    public int OrderIndex { get; set; } = 0;

    /// <summary>
    /// Whether this deliverable is required for project completion
    /// </summary>
    public bool IsRequired { get; set; } = true;
}

public class CreateProjectSkillDto
{
    /// <summary>
    /// Associated skill ID
    /// </summary>
    [Required(ErrorMessage = "Skill ID is required")]
    public Guid SkillId { get; set; }

    /// <summary>
    /// Required proficiency level for this skill (1-5 scale)
    /// </summary>
    [Required(ErrorMessage = "Proficiency level is required")]
    [Range(1, 5, ErrorMessage = "Proficiency level must be between 1 and 5")]
    public int ProficiencyRequired { get; set; } = 3;

    /// <summary>
    /// Weight/importance of this skill for the project (1-5 scale)
    /// </summary>
    [Range(1, 5, ErrorMessage = "Weight must be between 1 and 5")]
    public int Weight { get; set; } = 3;
}

public class ProjectDto
{
    /// <summary>
    /// Unique identifier for the project
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the client who posted the project
    /// </summary>
    public Guid ClientId { get; set; }

    /// <summary>
    /// Client information
    /// </summary>
    public required UserSummaryDto Client { get; set; }

    /// <summary>
    /// Project title
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Rich text description
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Current project status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Credit budget for the project
    /// </summary>
    public int CreditBudget { get; set; }

    /// <summary>
    /// When the project work should start
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// When the project should be completed
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Content moderation status
    /// </summary>
    public string ModerationStatus { get; set; } = string.Empty;

    /// <summary>
    /// Optional moderation notes from review process
    /// </summary>
    public string? ModerationNotes { get; set; }

    /// <summary>
    /// When the project was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the project was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Project deliverables
    /// </summary>
    public List<ProjectDeliverableDto> Deliverables { get; set; } = new();

    /// <summary>
    /// Required skills for the project
    /// </summary>
    public List<ProjectSkillDto> RequiredSkills { get; set; } = new();

    /// <summary>
    /// Whether the project has a valid timeline
    /// </summary>
    public bool HasValidTimeline { get; set; }

    /// <summary>
    /// Whether the project is editable
    /// </summary>
    public bool IsEditable { get; set; }

    /// <summary>
    /// Whether the project can be published
    /// </summary>
    public bool CanBePublished { get; set; }

    /// <summary>
    /// Duration in days (calculated from start and end dates)
    /// </summary>
    public int? DurationInDays
    {
        get
        {
            if (!StartDate.HasValue || !EndDate.HasValue) return null;
            return (EndDate.Value - StartDate.Value).Days;
        }
    }

    /// <summary>
    /// Formatted duration string (e.g., "2 weeks", "3 months")
    /// </summary>
    public string? DurationDisplay
    {
        get
        {
            var days = DurationInDays;
            if (!days.HasValue) return null;

            if (days.Value <= 7) return $"{days.Value} day{(days.Value == 1 ? "" : "s")}";
            if (days.Value <= 30) return $"{days.Value / 7} week{(days.Value / 7 == 1 ? "" : "s")}";
            if (days.Value <= 365) return $"{days.Value / 30} month{(days.Value / 30 == 1 ? "" : "s")}";

            return $"{days.Value / 365} year{(days.Value / 365 == 1 ? "" : "s")}";
        }
    }
}

public class ProjectDeliverableDto
{
    /// <summary>
    /// Unique identifier for the deliverable
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the project this deliverable belongs to
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Description of what needs to be delivered
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Order index for displaying deliverables in sequence
    /// </summary>
    public int OrderIndex { get; set; }

    /// <summary>
    /// Whether this deliverable is required for project completion
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Whether this deliverable has been completed
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// When this deliverable was completed (if applicable)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// When the deliverable was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

public class ProjectSkillDto
{
    /// <summary>
    /// Reference to the project
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Skill information
    /// </summary>
    public required SkillDto Skill { get; set; }

    /// <summary>
    /// Required proficiency level for this skill
    /// </summary>
    public int ProficiencyRequired { get; set; }

    /// <summary>
    /// Human-readable proficiency level
    /// </summary>
    public string ProficiencyDisplay
    {
        get
        {
            return ProficiencyRequired switch
            {
                1 => "Beginner",
                2 => "Novice",
                3 => "Intermediate",
                4 => "Advanced",
                5 => "Expert",
                _ => "Unknown"
            };
        }
    }

    /// <summary>
    /// Weight/importance of this skill for the project
    /// </summary>
    public int Weight { get; set; }

    /// <summary>
    /// Human-readable weight description
    /// </summary>
    public string WeightDisplay
    {
        get
        {
            return Weight switch
            {
                1 => "Low Priority",
                2 => "Nice to Have",
                3 => "Important",
                4 => "High Priority",
                5 => "Critical",
                _ => "Unknown"
            };
        }
    }

    /// <summary>
    /// When this skill requirement was added to the project
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

public class ProjectResponseDto
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Response message
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Project data (if successful)
    /// </summary>
    public ProjectDto? Project { get; set; }
}

public class ProjectSearchDto
{
    /// <summary>
    /// Search query for title or description
    /// </summary>
    public string? Query { get; set; }

    /// <summary>
    /// Filter by project status
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Filter by required skills (skill IDs)
    /// </summary>
    public List<Guid>? SkillIds { get; set; }

    /// <summary>
    /// Filter by required skills (skill names) - alternative to SkillIds for frontend convenience.
    /// Backend will convert names to IDs. Accepts comma-separated list of skill names.
    /// </summary>
    public string? SkillNames { get; set; }

    /// <summary>
    /// Filter by credit budget range - minimum
    /// </summary>
    [Range(50, 5000, ErrorMessage = "Minimum budget must be between 50 and 5000")]
    public int? MinBudget { get; set; }

    /// <summary>
    /// Filter by credit budget range - maximum
    /// </summary>
    [Range(50, 5000, ErrorMessage = "Maximum budget must be between 50 and 5000")]
    public int? MaxBudget { get; set; }

    /// <summary>
    /// Filter by client ID
    /// </summary>
    public Guid? ClientId { get; set; }

    /// <summary>
    /// Whether to include only published projects
    /// </summary>
    public bool PublishedOnly { get; set; } = true;

    /// <summary>
    /// Sort order (CreatedDate, Budget, EndDate, etc.)
    /// </summary>
    public string SortBy { get; set; } = "CreatedAt";

    /// <summary>
    /// Sort direction (asc, desc)
    /// </summary>
    public string SortDirection { get; set; } = "desc";

    /// <summary>
    /// Number of projects to skip (for pagination)
    /// </summary>
    public int Skip { get; set; } = 0;

    /// <summary>
    /// Number of projects to take (for pagination)
    /// </summary>
    [Range(1, 100, ErrorMessage = "Take must be between 1 and 100")]
    public int Take { get; set; } = 20;
}

/// <summary>
/// Advanced project discovery search DTO with enhanced filtering, geolocation, and recommendation support
/// </summary>
public class AdvancedProjectSearchDto
{
    /// <summary>
    /// Full-text search query for title, description, and deliverables
    /// </summary>
    public string? Query { get; set; }

    /// <summary>
    /// Filter by project status(es) - multiple values supported
    /// </summary>
    public List<string>? Status { get; set; }

    /// <summary>
    /// Filter by required skills with skill matching strategies
    /// </summary>
    public List<Guid>? SkillIds { get; set; }

    /// <summary>
    /// Skill matching strategy: "Any" (has any of the skills) or "All" (has all skills)
    /// </summary>
    public SkillMatchStrategy SkillMatch { get; set; } = SkillMatchStrategy.Any;

    /// <summary>
    /// Filter by credit budget range - minimum (optional, no validation to allow flexible search)
    /// </summary>
    public int? MinBudget { get; set; }

    /// <summary>
    /// Filter by credit budget range - maximum (optional, no validation to allow flexible search)
    /// </summary>
    public int? MaxBudget { get; set; }

    /// <summary>
    /// Filter by project duration range - minimum days (optional, no validation to allow flexible search)
    /// </summary>
    public int? MinDurationDays { get; set; }

    /// <summary>
    /// Filter by project duration range - maximum days (optional, no validation to allow flexible search)
    /// </summary>
    public int? MaxDurationDays { get; set; }

    /// <summary>
    /// Filter by project creation date range - from
    /// </summary>
    public DateTime? CreatedFrom { get; set; }

    /// <summary>
    /// Filter by project creation date range - to
    /// </summary>
    public DateTime? CreatedTo { get; set; }

    /// <summary>
    /// Filter by project start date range - from
    /// </summary>
    public DateTime? StartDateFrom { get; set; }

    /// <summary>
    /// Filter by project start date range - to
    /// </summary>
    public DateTime? StartDateTo { get; set; }

    /// <summary>
    /// Filter by project end date range - from
    /// </summary>
    public DateTime? EndDateFrom { get; set; }

    /// <summary>
    /// Filter by project end date range - to
    /// </summary>
    public DateTime? EndDateTo { get; set; }

    /// <summary>
    /// Filter by client location (city, state, country)
    /// </summary>
    public string? ClientLocation { get; set; }

    /// <summary>
    /// Geolocation search center point - latitude
    /// </summary>
    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
    public double? Latitude { get; set; }

    /// <summary>
    /// Geolocation search center point - longitude
    /// </summary>
    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
    public double? Longitude { get; set; }

    /// <summary>
    /// Search radius in kilometers for geolocation filtering
    /// </summary>
    [Range(1, 10000, ErrorMessage = "Radius must be between 1 and 10000 km")]
    public int? RadiusKm { get; set; }

    /// <summary>
    /// Filter by time zone compatibility with client
    /// </summary>
    public string? TimeZone { get; set; }

    /// <summary>
    /// Filter by number of deliverables - minimum
    /// </summary>
    [Range(1, 10, ErrorMessage = "Minimum deliverables must be between 1 and 10")]
    public int? MinDeliverables { get; set; }

    /// <summary>
    /// Filter by number of deliverables - maximum
    /// </summary>
    [Range(1, 10, ErrorMessage = "Maximum deliverables must be between 1 and 10")]
    public int? MaxDeliverables { get; set; }

    /// <summary>
    /// Whether to include only projects with remote work option
    /// </summary>
    public bool? RemoteWorkOnly { get; set; }

    /// <summary>
    /// Filter by client ID
    /// </summary>
    public Guid? ClientId { get; set; }

    /// <summary>
    /// Exclude projects from specific clients
    /// </summary>
    public List<Guid>? ExcludeClients { get; set; }

    /// <summary>
    /// Whether to include only published and approved projects
    /// </summary>
    public bool PublishedOnly { get; set; } = true;

    /// <summary>
    /// Advanced sorting options with multiple sort criteria
    /// </summary>
    public List<SortCriteria>? SortBy { get; set; }

    /// <summary>
    /// Default sort if no custom sort criteria provided
    /// </summary>
    public string DefaultSort { get; set; } = "relevance";

    /// <summary>
    /// Enable intelligent recommendations based on user profile
    /// </summary>
    public bool EnableRecommendations { get; set; } = false;

    /// <summary>
    /// User ID for personalized recommendations (if EnableRecommendations is true)
    /// </summary>
    public Guid? RecommendationUserId { get; set; }

    /// <summary>
    /// Include projects that are similar to specified project IDs
    /// </summary>
    public List<Guid>? SimilarToProjects { get; set; }

    /// <summary>
    /// Boost projects from clients with high reputation scores
    /// </summary>
    public bool BoostHighReputationClients { get; set; } = false;

    /// <summary>
    /// Search result caching strategy
    /// </summary>
    public SearchCacheStrategy CacheStrategy { get; set; } = SearchCacheStrategy.Standard;

    /// <summary>
    /// Number of projects to skip (for pagination)
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Skip must be 0 or greater")]
    public int Skip { get; set; } = 0;

    /// <summary>
    /// Number of projects to take (for pagination)
    /// </summary>
    [Range(1, 100, ErrorMessage = "Take must be between 1 and 100")]
    public int Take { get; set; } = 20;

    /// <summary>
    /// Include search result aggregations (faceted search data)
    /// </summary>
    public bool IncludeAggregations { get; set; } = false;

    /// <summary>
    /// Include search query explanation for debugging
    /// </summary>
    public bool IncludeExplanation { get; set; } = false;
}

/// <summary>
/// Skill matching strategy for project search
/// </summary>
public enum SkillMatchStrategy
{
    /// <summary>
    /// Project must have ANY of the specified skills
    /// </summary>
    Any = 1,

    /// <summary>
    /// Project must have ALL of the specified skills
    /// </summary>
    All = 2
}

/// <summary>
/// Search result caching strategy
/// </summary>
public enum SearchCacheStrategy
{
    /// <summary>
    /// No caching
    /// </summary>
    None = 0,

    /// <summary>
    /// Standard caching (5 minutes)
    /// </summary>
    Standard = 1,

    /// <summary>
    /// Extended caching (15 minutes)
    /// </summary>
    Extended = 2,

    /// <summary>
    /// Long-term caching (1 hour) for static queries
    /// </summary>
    LongTerm = 3
}

/// <summary>
/// Sort criteria for advanced project search
/// </summary>
public class SortCriteria
{
    /// <summary>
    /// Field to sort by (relevance, created, budget, endDate, etc.)
    /// </summary>
    [Required(ErrorMessage = "Sort field is required")]
    public required string Field { get; set; }

    /// <summary>
    /// Sort direction (asc, desc)
    /// </summary>
    public string Direction { get; set; } = "desc";

    /// <summary>
    /// Sort weight/priority (higher numbers have more influence)
    /// </summary>
    [Range(1, 10, ErrorMessage = "Sort weight must be between 1 and 10")]
    public int Weight { get; set; } = 1;
}

public class ProjectSummaryDto
{
    /// <summary>
    /// Unique identifier for the project
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Project title
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Short description (first 200 characters)
    /// </summary>
    public required string ShortDescription { get; set; }

    /// <summary>
    /// Client information
    /// </summary>
    public required UserSummaryDto Client { get; set; }

    /// <summary>
    /// Current project status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Credit budget for the project
    /// </summary>
    public int CreditBudget { get; set; }

    /// <summary>
    /// When the project was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Project end date
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Number of deliverables
    /// </summary>
    public int DeliverableCount { get; set; }

    /// <summary>
    /// Required skills summary
    /// </summary>
    public List<string> RequiredSkillNames { get; set; } = new();

    /// <summary>
    /// Formatted duration string
    /// </summary>
    public string? DurationDisplay { get; set; }
}

public class SaveDraftProjectDto
{
    /// <summary>
    /// Project title (optional for draft)
    /// </summary>
    [MaxLength(100, ErrorMessage = "Title cannot exceed 100 characters")]
    public string? Title { get; set; }

    /// <summary>
    /// Rich text description (optional for draft)
    /// </summary>
    [MaxLength(5000, ErrorMessage = "Description cannot exceed 5000 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// Credit budget for the project (optional for draft)
    /// </summary>
    [Range(50, 5000, ErrorMessage = "Credit budget must be between 50 and 5000 credits")]
    public int? CreditBudget { get; set; }

    /// <summary>
    /// When the project work should start (optional for draft)
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// When the project should be completed (optional for draft)
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Project deliverables (optional for draft)
    /// </summary>
    [MaxLength(10, ErrorMessage = "Cannot exceed 10 deliverables")]
    public List<CreateProjectDeliverableDto>? Deliverables { get; set; }

    /// <summary>
    /// Required skills for the project (optional for draft)
    /// </summary>
    [MaxLength(5, ErrorMessage = "Cannot exceed 5 skills")]
    public List<CreateProjectSkillDto>? RequiredSkills { get; set; }
}

/// <summary>
/// Advanced search results with aggregations and metadata
/// </summary>
public class AdvancedProjectSearchResultDto
{
    /// <summary>
    /// Matching projects
    /// </summary>
    public List<ProjectSummaryDto> Projects { get; set; } = new();

    /// <summary>
    /// Total number of projects matching the search criteria
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int CurrentPage { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Whether there are more results available
    /// </summary>
    public bool HasNextPage { get; set; }

    /// <summary>
    /// Whether there are previous results available
    /// </summary>
    public bool HasPreviousPage { get; set; }

    /// <summary>
    /// Search aggregations for faceted search
    /// </summary>
    public SearchAggregationsDto? Aggregations { get; set; }

    /// <summary>
    /// Recommended/similar projects based on search context
    /// </summary>
    public List<ProjectSummaryDto> RecommendedProjects { get; set; } = new();

    /// <summary>
    /// Search execution metadata
    /// </summary>
    public SearchMetadataDto Metadata { get; set; } = new();

    /// <summary>
    /// Search query explanation (if requested)
    /// </summary>
    public string? QueryExplanation { get; set; }
}

/// <summary>
/// Search aggregations for faceted search functionality
/// </summary>
public class SearchAggregationsDto
{
    /// <summary>
    /// Skills distribution in search results
    /// </summary>
    public List<FacetDto> Skills { get; set; } = new();

    /// <summary>
    /// Budget ranges distribution
    /// </summary>
    public List<FacetDto> BudgetRanges { get; set; } = new();

    /// <summary>
    /// Project duration distribution
    /// </summary>
    public List<FacetDto> DurationRanges { get; set; } = new();

    /// <summary>
    /// Project status distribution
    /// </summary>
    public List<FacetDto> Status { get; set; } = new();

    /// <summary>
    /// Client location distribution
    /// </summary>
    public List<FacetDto> Locations { get; set; } = new();

    /// <summary>
    /// Deliverable count distribution
    /// </summary>
    public List<FacetDto> DeliverableCounts { get; set; } = new();

    /// <summary>
    /// Time zone distribution
    /// </summary>
    public List<FacetDto> TimeZones { get; set; } = new();

    /// <summary>
    /// Creation date ranges
    /// </summary>
    public List<FacetDto> CreationDateRanges { get; set; } = new();
}

/// <summary>
/// Search facet for drill-down filtering
/// </summary>
public class FacetDto
{
    /// <summary>
    /// Facet key/value
    /// </summary>
    public required string Key { get; set; }

    /// <summary>
    /// Human-readable display value
    /// </summary>
    public required string DisplayValue { get; set; }

    /// <summary>
    /// Number of items in this facet
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Whether this facet is currently selected in the search
    /// </summary>
    public bool IsSelected { get; set; }
}

/// <summary>
/// Search execution metadata
/// </summary>
public class SearchMetadataDto
{
    /// <summary>
    /// Search execution time in milliseconds
    /// </summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>
    /// Whether results were served from cache
    /// </summary>
    public bool FromCache { get; set; }

    /// <summary>
    /// Cache key used (if applicable)
    /// </summary>
    public string? CacheKey { get; set; }

    /// <summary>
    /// Search index version used
    /// </summary>
    public string? IndexVersion { get; set; }

    /// <summary>
    /// Applied filters summary
    /// </summary>
    public List<string> AppliedFilters { get; set; } = new();

    /// <summary>
    /// Search warnings or suggestions
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Saved search configuration
/// </summary>
public class SavedSearchDto
{
    /// <summary>
    /// Unique identifier for the saved search
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User who saved this search
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Human-readable name for the saved search
    /// </summary>
    [Required(ErrorMessage = "Search name is required")]
    [MaxLength(100, ErrorMessage = "Search name cannot exceed 100 characters")]
    public required string Name { get; set; }

    /// <summary>
    /// Optional description of what this search is for
    /// </summary>
    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// Serialized search criteria
    /// </summary>
    public required string SearchCriteria { get; set; }

    /// <summary>
    /// Whether to enable email notifications for new matching projects
    /// </summary>
    public bool NotificationsEnabled { get; set; } = false;

    /// <summary>
    /// Notification frequency (immediate, daily, weekly)
    /// </summary>
    public NotificationFrequency NotificationFrequency { get; set; } = NotificationFrequency.Daily;

    /// <summary>
    /// When this search was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this search was last used
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Number of times this search has been executed
    /// </summary>
    public int UsageCount { get; set; }

    /// <summary>
    /// Whether this search is active
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Create saved search request
/// </summary>
public class CreateSavedSearchDto
{
    /// <summary>
    /// Human-readable name for the saved search
    /// </summary>
    [Required(ErrorMessage = "Search name is required")]
    [MaxLength(100, ErrorMessage = "Search name cannot exceed 100 characters")]
    public required string Name { get; set; }

    /// <summary>
    /// Optional description of what this search is for
    /// </summary>
    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// Search criteria to save
    /// </summary>
    [Required(ErrorMessage = "Search criteria is required")]
    public required AdvancedProjectSearchDto SearchCriteria { get; set; }

    /// <summary>
    /// Whether to enable email notifications for new matching projects
    /// </summary>
    public bool NotificationsEnabled { get; set; } = false;

    /// <summary>
    /// Notification frequency (immediate, daily, weekly)
    /// </summary>
    public NotificationFrequency NotificationFrequency { get; set; } = NotificationFrequency.Daily;
}

/// <summary>
/// DTO for updating a saved search
/// </summary>
public class UpdateSavedSearchDto
{
    /// <summary>
    /// Human-readable name for the saved search
    /// </summary>
    [Required(ErrorMessage = "Search name is required")]
    [MaxLength(100, ErrorMessage = "Search name cannot exceed 100 characters")]
    public required string Name { get; set; }

    /// <summary>
    /// Optional description of what this search is for
    /// </summary>
    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// Search criteria to save
    /// </summary>
    [Required(ErrorMessage = "Search criteria is required")]
    public required AdvancedProjectSearchDto SearchCriteria { get; set; }

    /// <summary>
    /// Whether to enable email notifications for new matching projects
    /// </summary>
    public bool NotificationsEnabled { get; set; } = false;

    /// <summary>
    /// Notification frequency (immediate, daily, weekly)
    /// </summary>
    public NotificationFrequency NotificationFrequency { get; set; } = NotificationFrequency.Daily;
}

// Additional DTOs for US-2.2.1 Advanced Project Discovery

/// <summary>
/// Trending search term data
/// </summary>
public class TrendingSearchTermDto
{
    /// <summary>
    /// Search term that is trending
    /// </summary>
    public required string Term { get; set; }

    /// <summary>
    /// Number of times this term was searched
    /// </summary>
    public int SearchCount { get; set; }

    /// <summary>
    /// Percentage change from previous period
    /// </summary>
    public double ChangePercentage { get; set; }

    /// <summary>
    /// Rank in trending list
    /// </summary>
    public int Rank { get; set; }
}

/// <summary>
/// DTO for different skill match modes
/// </summary>
public enum SkillMatchMode
{
    /// <summary>
    /// Project must have ANY of the specified skills
    /// </summary>
    Any = 1,

    /// <summary>
    /// Project must have ALL of the specified skills  
    /// </summary>
    All = 2
}

/// <summary>
/// DTO for different project sort options
/// </summary>
public enum ProjectSortBy
{
    /// <summary>
    /// Sort by search relevance (default for searches with query)
    /// </summary>
    Relevance = 1,

    /// <summary>
    /// Sort by creation date, newest first
    /// </summary>
    Newest = 2,

    /// <summary>
    /// Sort by creation date, oldest first
    /// </summary>
    Oldest = 3,

    /// <summary>
    /// Sort by budget, highest first
    /// </summary>
    BudgetHighToLow = 4,

    /// <summary>
    /// Sort by budget, lowest first
    /// </summary>
    BudgetLowToHigh = 5,

    /// <summary>
    /// Sort by deadline, most urgent first
    /// </summary>
    Deadline = 6,

    /// <summary>
    /// Sort by start date, earliest first
    /// </summary>
    StartDate = 7,

    /// <summary>
    /// Sort alphabetically by title
    /// </summary>
    Title = 8
}

/// <summary>
/// Project search result with pagination and metadata
/// </summary>
public class ProjectSearchResultDto
{
    /// <summary>
    /// List of matching projects
    /// </summary>
    public List<ProjectDto> Projects { get; set; } = new();

    /// <summary>
    /// Total count of projects matching the search criteria
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int CurrentPage { get; set; }

    /// <summary>
    /// Number of projects per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNextPage { get; set; }

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPreviousPage { get; set; }

    /// <summary>
    /// Search execution time in milliseconds
    /// </summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>
    /// Search relevance score (optional, for relevance-based searches)
    /// </summary>
    public double? SearchScore { get; set; }
}

// Project Application System DTOs (US-2.3.1)

/// <summary>
/// DTO for creating a new project application
/// </summary>
public class CreateProjectApplicationDto
{
    /// <summary>
    /// ID of the project to apply for
    /// </summary>
    [Required(ErrorMessage = "Project ID is required")]
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Cover letter with application pitch
    /// </summary>
    [Required(ErrorMessage = "Cover letter is required")]
    [MaxLength(2000, ErrorMessage = "Cover letter cannot exceed 2000 characters")]
    [MinLength(100, ErrorMessage = "Cover letter must be at least 100 characters")]
    public required string CoverLetter { get; set; }

    /// <summary>
    /// Proposed timeline in days to completion
    /// </summary>
    [Range(1, 365, ErrorMessage = "Timeline must be between 1 and 365 days")]
    public int? ProposedTimeline { get; set; }

    /// <summary>
    /// Whether the provider is available to start immediately
    /// </summary>
    public bool IsAvailableImmediately { get; set; } = false;

    /// <summary>
    /// Proposed budget in credits (optional override)
    /// </summary>
    [Range(50, 5000, ErrorMessage = "Budget must be between 50 and 5000 credits")]
    public int? ProposedBudget { get; set; }

    /// <summary>
    /// Timeline commitment and availability declaration
    /// </summary>
    [MaxLength(500, ErrorMessage = "Availability details cannot exceed 500 characters")]
    public string? AvailabilityDetails { get; set; }

    /// <summary>
    /// Portfolio attachment metadata (file uploads handled separately)
    /// </summary>
    public List<CreateApplicationAttachmentDto>? Attachments { get; set; }
}

/// <summary>
/// DTO for portfolio attachment metadata
/// </summary>
public class CreateApplicationAttachmentDto
{
    /// <summary>
    /// Original filename
    /// </summary>
    [Required(ErrorMessage = "Filename is required")]
    [MaxLength(255, ErrorMessage = "Filename cannot exceed 255 characters")]
    public required string FileName { get; set; }

    /// <summary>
    /// File content type
    /// </summary>
    [Required(ErrorMessage = "Content type is required")]
    [MaxLength(100, ErrorMessage = "Content type cannot exceed 100 characters")]
    public required string ContentType { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    [Range(1, 10 * 1024 * 1024, ErrorMessage = "File size must be between 1 byte and 10MB")]
    public long FileSize { get; set; }

    /// <summary>
    /// Storage URL for the uploaded file
    /// </summary>
    [Required(ErrorMessage = "Storage URL is required")]
    [MaxLength(500, ErrorMessage = "Storage URL cannot exceed 500 characters")]
    public required string StorageUrl { get; set; }

    /// <summary>
    /// Optional description of the portfolio item
    /// </summary>
    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }
}

/// <summary>
/// DTO for project application response
/// </summary>
public class ProjectApplicationDto
{
    /// <summary>
    /// Application ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Project information
    /// </summary>
    public required ProjectSummaryDto Project { get; set; }

    /// <summary>
    /// Provider information
    /// </summary>
    public required UserSummaryDto Provider { get; set; }

    /// <summary>
    /// Cover letter content
    /// </summary>
    public required string CoverLetter { get; set; }

    /// <summary>
    /// Proposed timeline in days
    /// </summary>
    public int? ProposedTimeline { get; set; }

    /// <summary>
    /// Automatic skill match score
    /// </summary>
    public decimal? SkillMatchScore { get; set; }

    /// <summary>
    /// Current application status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// When the application was submitted
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the application was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// When the application was reviewed (if applicable)
    /// </summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>
    /// Client feedback on application
    /// </summary>
    public string? ClientFeedback { get; set; }

    /// <summary>
    /// Whether provider is available immediately
    /// </summary>
    public bool IsAvailableImmediately { get; set; }

    /// <summary>
    /// Proposed budget override
    /// </summary>
    public int? ProposedBudget { get; set; }

    /// <summary>
    /// Availability details
    /// </summary>
    public string? AvailabilityDetails { get; set; }

    /// <summary>
    /// Portfolio attachments
    /// </summary>
    public List<ApplicationAttachmentDto> Attachments { get; set; } = new();

    /// <summary>
    /// Days since application was submitted
    /// </summary>
    public int DaysSinceSubmitted { get; set; }

    /// <summary>
    /// Whether application can be withdrawn
    /// </summary>
    public bool CanBeWithdrawn { get; set; }
}

/// <summary>
/// DTO for application attachment
/// </summary>
public class ApplicationAttachmentDto
{
    /// <summary>
    /// Attachment ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Original filename
    /// </summary>
    public required string FileName { get; set; }

    /// <summary>
    /// File content type
    /// </summary>
    public required string ContentType { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Download/view URL
    /// </summary>
    public required string Url { get; set; }

    /// <summary>
    /// Portfolio item description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether file is safe to download
    /// </summary>
    public bool IsSafe { get; set; }

    /// <summary>
    /// Upload timestamp
    /// </summary>
    public DateTime UploadedAt { get; set; }
}

/// <summary>
/// DTO for updating application status (by client)
/// </summary>
public class UpdateApplicationStatusDto
{
    /// <summary>
    /// New application status
    /// </summary>
    [Required(ErrorMessage = "Status is required")]
    public required string Status { get; set; }

    /// <summary>
    /// Optional feedback from client
    /// </summary>
    [MaxLength(1000, ErrorMessage = "Feedback cannot exceed 1000 characters")]
    public string? ClientFeedback { get; set; }
}

/// <summary>
/// DTO for application search and filtering
/// </summary>
public class ApplicationSearchDto
{
    /// <summary>
    /// Filter by project ID
    /// </summary>
    public Guid? ProjectId { get; set; }

    /// <summary>
    /// Filter by provider ID
    /// </summary>
    public Guid? ProviderId { get; set; }

    /// <summary>
    /// Filter by application status
    /// </summary>
    public List<string>? Status { get; set; }

    /// <summary>
    /// Filter by minimum skill match score
    /// </summary>
    [Range(0.0, 1.0, ErrorMessage = "Min score must be between 0.0 and 1.0")]
    public decimal? MinSkillMatchScore { get; set; }

    /// <summary>
    /// Filter by submission date range - from
    /// </summary>
    public DateTime? SubmittedFrom { get; set; }

    /// <summary>
    /// Filter by submission date range - to
    /// </summary>
    public DateTime? SubmittedTo { get; set; }

    /// <summary>
    /// Filter by providers available immediately
    /// </summary>
    public bool? AvailableImmediately { get; set; }

    /// <summary>
    /// Sort by field (SubmittedAt, SkillMatch, Status)
    /// </summary>
    public string SortBy { get; set; } = "SubmittedAt";

    /// <summary>
    /// Sort direction (asc, desc)
    /// </summary>
    public string SortDirection { get; set; } = "desc";

    /// <summary>
    /// Number of applications to skip
    /// </summary>
    public int Skip { get; set; } = 0;

    /// <summary>
    /// Number of applications to take
    /// </summary>
    [Range(1, 100, ErrorMessage = "Take must be between 1 and 100")]
    public int Take { get; set; } = 20;
}

/// <summary>
/// DTO for application search results
/// </summary>
public class ApplicationSearchResultDto
{
    /// <summary>
    /// Matching applications
    /// </summary>
    public List<ProjectApplicationDto> Applications { get; set; } = new();

    /// <summary>
    /// Total count of matching applications
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int CurrentPage { get; set; }

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total pages
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Whether there are more pages
    /// </summary>
    public bool HasNextPage { get; set; }

    /// <summary>
    /// Whether there are previous pages
    /// </summary>
    public bool HasPreviousPage { get; set; }
}

/// <summary>
/// DTO for application statistics
/// </summary>
public class ApplicationStatisticsDto
{
    /// <summary>
    /// Total applications submitted
    /// </summary>
    public int TotalApplications { get; set; }

    /// <summary>
    /// Applications by status
    /// </summary>
    public Dictionary<string, int> ApplicationsByStatus { get; set; } = new();

    /// <summary>
    /// Average skill match score
    /// </summary>
    public decimal? AverageSkillMatchScore { get; set; }

    /// <summary>
    /// Applications submitted this month
    /// </summary>
    public int ApplicationsThisMonth { get; set; }

    /// <summary>
    /// Success rate (accepted / total submitted)
    /// </summary>
    public decimal SuccessRate { get; set; }

    /// <summary>
    /// Average response time in days
    /// </summary>
    public double? AverageResponseTimeDays { get; set; }
}

// Provider Selection DTOs (US-2.4.1)

/// <summary>
/// DTO for creating a provider selection
/// </summary>
public class CreateProviderSelectionDto
{
    /// <summary>
    /// Project ID for which provider is being selected
    /// </summary>
    [Required(ErrorMessage = "Project ID is required")]
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Selected provider's user ID
    /// </summary>
    [Required(ErrorMessage = "Provider ID is required")]
    public Guid SelectedProviderId { get; set; }

    /// <summary>
    /// Selected application ID
    /// </summary>
    [Required(ErrorMessage = "Application ID is required")]
    public Guid SelectedApplicationId { get; set; }

    /// <summary>
    /// Reason for selecting this provider
    /// </summary>
    [Required(ErrorMessage = "Selection reason is required")]
    [MaxLength(1000, ErrorMessage = "Selection reason cannot exceed 1000 characters")]
    [MinLength(50, ErrorMessage = "Selection reason must be at least 50 characters")]
    public required string SelectionReason { get; set; }

    /// <summary>
    /// Contract terms agreed upon
    /// </summary>
    [MaxLength(5000, ErrorMessage = "Contract terms cannot exceed 5000 characters")]
    public string? ContractTerms { get; set; }

    /// <summary>
    /// Escrow amount in credits
    /// </summary>
    [Required(ErrorMessage = "Escrow amount is required")]
    [Range(50, 5000, ErrorMessage = "Escrow amount must be between 50 and 5000 credits")]
    public int EscrowAmount { get; set; }

    /// <summary>
    /// Expected project start date
    /// </summary>
    public DateTime? ExpectedStartDate { get; set; }

    /// <summary>
    /// Expected project completion date
    /// </summary>
    public DateTime? ExpectedCompletionDate { get; set; }

    /// <summary>
    /// Notes from contract negotiation
    /// </summary>
    [MaxLength(2000, ErrorMessage = "Negotiation notes cannot exceed 2000 characters")]
    public string? NegotiationNotes { get; set; }
}

/// <summary>
/// DTO for provider selection response
/// </summary>
public class ProviderSelectionDto
{
    /// <summary>
    /// Selection ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Project information
    /// </summary>
    public required ProjectSummaryDto Project { get; set; }

    /// <summary>
    /// Selected provider information
    /// </summary>
    public required UserSummaryDto SelectedProvider { get; set; }

    /// <summary>
    /// Selected application details
    /// </summary>
    public required ProjectApplicationDto SelectedApplication { get; set; }

    /// <summary>
    /// Reason for selection
    /// </summary>
    public required string SelectionReason { get; set; }

    /// <summary>
    /// Contract terms
    /// </summary>
    public string? ContractTerms { get; set; }

    /// <summary>
    /// Escrow amount in credits
    /// </summary>
    public int EscrowAmount { get; set; }

    /// <summary>
    /// When selection was made
    /// </summary>
    public DateTime SelectedAt { get; set; }

    /// <summary>
    /// Expected start date
    /// </summary>
    public DateTime? ExpectedStartDate { get; set; }

    /// <summary>
    /// Expected completion date
    /// </summary>
    public DateTime? ExpectedCompletionDate { get; set; }

    /// <summary>
    /// Current status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Negotiation notes
    /// </summary>
    public string? NegotiationNotes { get; set; }

    /// <summary>
    /// Whether escrow is funded
    /// </summary>
    public bool IsEscrowFunded { get; set; }

    /// <summary>
    /// Whether contract is signed
    /// </summary>
    public bool IsContractSigned { get; set; }

    /// <summary>
    /// Whether ready to start work
    /// </summary>
    public bool IsReadyToStart { get; set; }
}

/// <summary>
/// DTO for application comparison and ranking
/// </summary>
public class ApplicationComparisonDto
{
    /// <summary>
    /// Application details
    /// </summary>
    public required ProjectApplicationDto Application { get; set; }

    /// <summary>
    /// Calculated ranking score
    /// </summary>
    public decimal RankingScore { get; set; }

    /// <summary>
    /// Skill match percentage
    /// </summary>
    public decimal SkillMatchPercentage { get; set; }

    /// <summary>
    /// Provider reputation score
    /// </summary>
    public decimal ReputationScore { get; set; }

    /// <summary>
    /// Timeline competitiveness score
    /// </summary>
    public decimal TimelineScore { get; set; }

    /// <summary>
    /// Budget competitiveness score
    /// </summary>
    public decimal BudgetScore { get; set; }

    /// <summary>
    /// Availability score
    /// </summary>
    public decimal AvailabilityScore { get; set; }

    /// <summary>
    /// Overall recommendation level
    /// </summary>
    public RecommendationLevel RecommendationLevel { get; set; }

    /// <summary>
    /// Strengths of this application
    /// </summary>
    public List<string> Strengths { get; set; } = new();

    /// <summary>
    /// Concerns about this application
    /// </summary>
    public List<string> Concerns { get; set; } = new();

    /// <summary>
    /// Provider's past work history summary
    /// </summary>
    public ProviderHistorySummaryDto? ProviderHistory { get; set; }
}

/// <summary>
/// Recommendation levels for provider selection
/// </summary>
public enum RecommendationLevel
{
    /// <summary>
    /// Not recommended
    /// </summary>
    NotRecommended = 1,

    /// <summary>
    /// Consider with caution
    /// </summary>
    ConsiderWithCaution = 2,

    /// <summary>
    /// Good candidate
    /// </summary>
    GoodCandidate = 3,

    /// <summary>
    /// Highly recommended
    /// </summary>
    HighlyRecommended = 4,

    /// <summary>
    /// Top choice
    /// </summary>
    TopChoice = 5
}

/// <summary>
/// DTO for provider history summary
/// </summary>
public class ProviderHistorySummaryDto
{
    /// <summary>
    /// Number of projects completed
    /// </summary>
    public int ProjectsCompleted { get; set; }

    /// <summary>
    /// Average rating received
    /// </summary>
    public decimal AverageRating { get; set; }

    /// <summary>
    /// On-time delivery rate
    /// </summary>
    public decimal OnTimeDeliveryRate { get; set; }

    /// <summary>
    /// Client satisfaction score
    /// </summary>
    public decimal ClientSatisfactionScore { get; set; }

    /// <summary>
    /// Recent projects relevant to current selection
    /// </summary>
    public List<string> RelevantProjects { get; set; } = new();

    /// <summary>
    /// Total credits earned
    /// </summary>
    public int TotalCreditsEarned { get; set; }

    /// <summary>
    /// Member since date
    /// </summary>
    public DateTime MemberSince { get; set; }
}

/// <summary>
/// DTO for selection dashboard view
/// </summary>
public class SelectionDashboardDto
{
    /// <summary>
    /// Project information
    /// </summary>
    public required ProjectDto Project { get; set; }

    /// <summary>
    /// All applications with ranking
    /// </summary>
    public List<ApplicationComparisonDto> RankedApplications { get; set; } = new();

    /// <summary>
    /// Top recommended applications
    /// </summary>
    public List<ApplicationComparisonDto> TopRecommendations { get; set; } = new();

    /// <summary>
    /// Applications requiring manual review
    /// </summary>
    public List<ApplicationComparisonDto> RequireReview { get; set; } = new();

    /// <summary>
    /// Selection statistics
    /// </summary>
    public SelectionStatisticsDto Statistics { get; set; } = new();

    /// <summary>
    /// Selection deadline
    /// </summary>
    public DateTime? SelectionDeadline { get; set; }

    /// <summary>
    /// Whether selection has been made
    /// </summary>
    public bool IsSelectionMade { get; set; }

    /// <summary>
    /// Current selection (if made)
    /// </summary>
    public ProviderSelectionDto? CurrentSelection { get; set; }
}

/// <summary>
/// DTO for selection statistics
/// </summary>
public class SelectionStatisticsDto
{
    /// <summary>
    /// Total applications received
    /// </summary>
    public int TotalApplications { get; set; }

    /// <summary>
    /// Applications by status
    /// </summary>
    public Dictionary<string, int> ApplicationsByStatus { get; set; } = new();

    /// <summary>
    /// Average skill match score
    /// </summary>
    public decimal AverageSkillMatchScore { get; set; }

    /// <summary>
    /// Budget range of applications
    /// </summary>
    public BudgetRangeDto BudgetRange { get; set; } = new();

    /// <summary>
    /// Timeline range of applications
    /// </summary>
    public TimelineRangeDto TimelineRange { get; set; } = new();

    /// <summary>
    /// Top skills represented
    /// </summary>
    public List<string> TopSkills { get; set; } = new();

    /// <summary>
    /// Provider experience levels distribution
    /// </summary>
    public Dictionary<string, int> ExperienceLevels { get; set; } = new();
}

/// <summary>
/// DTO for budget range
/// </summary>
public class BudgetRangeDto
{
    /// <summary>
    /// Minimum budget proposed
    /// </summary>
    public int MinBudget { get; set; }

    /// <summary>
    /// Maximum budget proposed
    /// </summary>
    public int MaxBudget { get; set; }

    /// <summary>
    /// Average budget proposed
    /// </summary>
    public decimal AverageBudget { get; set; }
}

/// <summary>
/// DTO for timeline range
/// </summary>
public class TimelineRangeDto
{
    /// <summary>
    /// Minimum timeline in days
    /// </summary>
    public int MinTimelineDays { get; set; }

    /// <summary>
    /// Maximum timeline in days
    /// </summary>
    public int MaxTimelineDays { get; set; }

    /// <summary>
    /// Average timeline in days
    /// </summary>
    public decimal AverageTimelineDays { get; set; }
}

/// <summary>
/// DTO for selection search and filtering
/// </summary>
public class ProviderSelectionSearchDto
{
    /// <summary>
    /// Filter by client ID
    /// </summary>
    public Guid? ClientId { get; set; }

    /// <summary>
    /// Filter by provider ID
    /// </summary>
    public Guid? ProviderId { get; set; }

    /// <summary>
    /// Filter by selection status
    /// </summary>
    public List<string>? Status { get; set; }

    /// <summary>
    /// Filter by selection date range - from
    /// </summary>
    public DateTime? SelectedFrom { get; set; }

    /// <summary>
    /// Filter by selection date range - to
    /// </summary>
    public DateTime? SelectedTo { get; set; }

    /// <summary>
    /// Filter by escrow amount range - minimum
    /// </summary>
    [Range(50, 5000, ErrorMessage = "Min escrow must be between 50 and 5000")]
    public int? MinEscrowAmount { get; set; }

    /// <summary>
    /// Filter by escrow amount range - maximum
    /// </summary>
    [Range(50, 5000, ErrorMessage = "Max escrow must be between 50 and 5000")]
    public int? MaxEscrowAmount { get; set; }

    /// <summary>
    /// Filter by contract signed status
    /// </summary>
    public bool? IsContractSigned { get; set; }

    /// <summary>
    /// Filter by escrow funded status
    /// </summary>
    public bool? IsEscrowFunded { get; set; }

    /// <summary>
    /// Sort by field
    /// </summary>
    public string SortBy { get; set; } = "SelectedAt";

    /// <summary>
    /// Sort direction
    /// </summary>
    public string SortDirection { get; set; } = "desc";

    /// <summary>
    /// Number of results to skip
    /// </summary>
    public int Skip { get; set; } = 0;

    /// <summary>
    /// Number of results to take
    /// </summary>
    [Range(1, 100, ErrorMessage = "Take must be between 1 and 100")]
    public int Take { get; set; } = 20;
}
