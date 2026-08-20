using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;
using System.Security.Claims;

namespace SkillLedger.Infrastructure.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly SkillLedgerDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IAuditLogService _auditLogService;
    private readonly Core.Interfaces.IAuthorizationService _rbacAuthorizationService;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        SkillLedgerDbContext context,
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IAuditLogService auditLogService,
        Core.Interfaces.IAuthorizationService rbacAuthorizationService,
        ILogger<AuthenticationService> logger)
    {
        _context = context;
        _userManager = userManager;
        _signInManager = signInManager;
        _auditLogService = auditLogService;
        _rbacAuthorizationService = rbacAuthorizationService;
        _logger = logger;
    }

    public async Task<LoginResponseDto> AuthenticateAsync(LoginRequestDto loginRequest, string? ipAddress = null, string? userAgent = null)
    {
        try
        {
            _logger.LogInformation("Authentication attempt for email: {Email}", loginRequest.Email);

            // Find user by email
            var user = await _userManager.FindByEmailAsync(loginRequest.Email);
            if (user == null)
            {
                await LogFailedAttempt("USER_LOGIN_FAILED", "User not found", null, ipAddress, userAgent, loginRequest.Email);
                return CreateFailedLoginResponse("Invalid email or password.");
            }

            // Check if account is locked
            if (await _userManager.IsLockedOutAsync(user))
            {
                var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
                await LogFailedAttempt("USER_LOGIN_FAILED", "Account locked", user.Id, ipAddress, userAgent, loginRequest.Email);

                return new LoginResponseDto
                {
                    Success = false,
                    Message = $"Account is locked until {lockoutEnd?.ToString("yyyy-MM-dd HH:mm:ss")} UTC due to too many failed attempts.",
                    IsLockedOut = true
                };
            }

            // Verify password and sign in using cookie authentication
            var result = await _signInManager.PasswordSignInAsync(
                user,
                loginRequest.Password,
                isPersistent: loginRequest.RememberMe,
                lockoutOnFailure: true);

            if (!result.Succeeded)
            {
                await IncrementFailedLoginAttempts(user);

                if (result.IsLockedOut)
                {
                    await LogFailedAttempt("USER_LOGIN_FAILED", "Account locked after failed attempts", user.Id, ipAddress, userAgent, loginRequest.Email);
                    return new LoginResponseDto
                    {
                        Success = false,
                        Message = "Account has been locked due to multiple failed login attempts. Please try again later.",
                        IsLockedOut = true
                    };
                }

                await LogFailedAttempt("USER_LOGIN_FAILED", "Invalid password", user.Id, ipAddress, userAgent, loginRequest.Email);
                return CreateFailedLoginResponse("Invalid email or password.");
            }

            // Successful authentication - reset failed attempts
            await ResetFailedLoginAttemptsAsync(user.Id);

            // Create user profile
            var userProfile = await CreateUserProfileDto(user);

            await _auditLogService.LogEventAsync(
                user.Id,
                "USER_LOGIN_SUCCESS",
                ipAddress ?? "unknown",
                userAgent,
                true,
                "User successfully authenticated with cookie"
            );

            _logger.LogInformation("Successful cookie authentication for user: {UserId}", user.Id);

            return new LoginResponseDto
            {
                Success = true,
                User = userProfile,
                Message = "Login successful"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication failed for email: {Email}", loginRequest.Email);
            await LogFailedAttempt("USER_LOGIN_ERROR", $"Authentication error: {ex.Message}", null, ipAddress, userAgent, loginRequest.Email);
            return CreateFailedLoginResponse("An error occurred during authentication. Please try again.");
        }
    }

    public async Task<LogoutResponseDto> LogoutAsync(Guid userId, string? ipAddress = null)
    {
        try
        {
            _logger.LogInformation("Logout attempt for user: {UserId}", userId);

            // Sign out from cookie authentication
            await _signInManager.SignOutAsync();

            await _auditLogService.LogEventAsync(
                userId,
                "USER_LOGOUT",
                ipAddress ?? "unknown",
                null,
                true,
                "User logged out successfully"
            );

            return new LogoutResponseDto
            {
                Success = true,
                Message = "Logged out successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout failed for user: {UserId}", userId);
            return new LogoutResponseDto
            {
                Success = false,
                Message = "Logout failed. Please try again."
            };
        }
    }

    public async Task<LogoutResponseDto> LogoutFromAllDevicesAsync(Guid userId, string? ipAddress = null)
    {
        try
        {
            _logger.LogInformation("Logout from all devices for user: {UserId}", userId);

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return new LogoutResponseDto
                {
                    Success = false,
                    Message = "User not found."
                };
            }

            await _userManager.UpdateSecurityStampAsync(user);
            await _signInManager.SignOutAsync();

            await _auditLogService.LogEventAsync(
                userId,
                "USER_LOGOUT_ALL_DEVICES",
                ipAddress ?? "unknown",
                null,
                true,
                "User logged out from all devices"
            );

            return new LogoutResponseDto
            {
                Success = true,
                Message = "Successfully logged out from all devices."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout from all devices failed for user: {UserId}", userId);
            return new LogoutResponseDto
            {
                Success = false,
                Message = "Failed to logout from all devices. Please try again."
            };
        }
    }

    public async Task<bool> IsAccountLockedAsync(Guid userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return false;
            }

            return await _userManager.IsLockedOutAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check account lock status for user: {UserId}", userId);
            return false;
        }
    }

    public async Task ResetFailedLoginAttemptsAsync(Guid userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return;
            }

            await _userManager.ResetAccessFailedCountAsync(user);

            // Update our custom failed attempts counter
            user.FailedLoginAttempts = 0;
            await _context.SaveChangesAsync();

            _logger.LogDebug("Reset failed login attempts for user: {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset failed login attempts for user: {UserId}", userId);
        }
    }

    public async Task<UserProfileDto?> GetCurrentUserFromContextAsync(Guid userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", userId);
                return null;
            }

            return await CreateUserProfileDto(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current user from context: {UserId}", userId);
            return null;
        }
    }

    #region Private Helper Methods

    private async Task<UserProfileDto> CreateUserProfileDto(User user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var permissions = await _rbacAuthorizationService.GetUserPermissionsAsync(user.Id);

        return new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email!,
            UserName = user.UserName!,
            FirstName = user.FirstName,  // E2E-015 FIX: Include first name for display
            LastName = user.LastName,    // E2E-015 FIX: Include last name for display
            EmailVerified = user.EmailConfirmed,  // E2E-006 FIX: Map EmailConfirmed to EmailVerified
            TaxCompliant = user.TaxCompliant,
            Status = user.Status.ToString(),
            Roles = roles.ToList(),
            Permissions = permissions.ToList()
        };
    }

    private async Task IncrementFailedLoginAttempts(User user)
    {
        try
        {
            user.FailedLoginAttempts++;
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to increment failed login attempts for user: {UserId}", user.Id);
        }
    }

    private async Task LogFailedAttempt(string eventType, string message, Guid? userId, string? ipAddress, string? userAgent, string? email = null)
    {
        try
        {
            var fullMessage = string.IsNullOrEmpty(email) ? message : $"{message} for email: {email}";
            await _auditLogService.LogEventAsync(userId, eventType, ipAddress ?? "unknown", userAgent, false, fullMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log audit event: {EventType}", eventType);
        }
    }

    private LoginResponseDto CreateFailedLoginResponse(string message)
    {
        return new LoginResponseDto
        {
            Success = false,
            Message = message,
            IsLockedOut = false
        };
    }

    #endregion
}
