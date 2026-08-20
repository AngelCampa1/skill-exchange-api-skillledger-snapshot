using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Core.Validators;
using SkillLedger.Infrastructure.Data;
using System.Security.Cryptography;
using System.Text.Json;
using AuditActions = SkillLedger.Core.Constants.AuditActions;

namespace SkillLedger.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly SkillLedgerDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly IAuditLogService _auditLogService;
    private readonly IEmailService _emailService;
    private readonly ISequencerClient _sequencerClient;
    private readonly ICreditWalletService _creditWalletService;
    private readonly ILogger<UserService> _logger;

    public UserService(
        SkillLedgerDbContext context,
        UserManager<User> userManager,
        IAuditLogService auditLogService,
        IEmailService emailService,
        ISequencerClient sequencerClient,
        ICreditWalletService creditWalletService,
        ILogger<UserService> logger)
    {
        _context = context;
        _userManager = userManager;
        _auditLogService = auditLogService;
        _emailService = emailService;
        _sequencerClient = sequencerClient;
        _creditWalletService = creditWalletService;
        _logger = logger;
    }

    public async Task<RegisterUserResponseDto> RegisterUserAsync(RegisterUserDto registerDto, string ipAddress, string? userAgent = null)
    {
        try
        {
            _logger.LogInformation("Starting user registration for email: {Email}", registerDto.Email);

            // Let ASP.NET Identity handle password validation to ensure consistency
            // This prevents mismatch between custom validation and Identity's built-in validation

            // Check if user already exists
            // BUG-004 FIX: Return proper error when email already exists
            var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
            if (existingUser != null)
            {
                // Log the failed attempt
                await _auditLogService.LogEventAsync(
                    existingUser.Id,
                    AuditActions.USER_REGISTRATION,
                    ipAddress,
                    userAgent,
                    false,
                    JsonSerializer.Serialize(new { Email = registerDto.Email }),
                    "Email already exists"
                );

                _logger.LogWarning("Registration attempt with existing email: {Email} from IP: {IPAddress}", registerDto.Email, ipAddress);

                // BUG-001 FIX: Return generic error to prevent email enumeration attacks
                // The audit log still records the actual reason internally for security monitoring
                return new RegisterUserResponseDto
                {
                    UserId = Guid.Empty,
                    Email = registerDto.Email,
                    Success = false,
                    Message = "Registration could not be completed. Please verify your information and try again."
                };
            }

            // Create new user
            var user = new User
            {
                UserName = registerDto.Email,
                Email = registerDto.Email,
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                CreatedFromIP = ipAddress,
                UpdatedAt = DateTime.UtcNow,
                UpdatedFromIP = ipAddress
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                await _auditLogService.LogEventAsync(
                    null,
                    AuditActions.USER_REGISTRATION,
                    ipAddress,
                    userAgent,
                    false,
                    JsonSerializer.Serialize(new { Email = registerDto.Email, Errors = result.Errors }),
                    errors
                );

                _logger.LogError("Failed to create user: {Errors}", errors);
                return new RegisterUserResponseDto
                {
                    UserId = Guid.Empty,
                    Email = registerDto.Email,
                    Success = false,
                    Message = errors // Return the actual ASP.NET Identity validation errors
                };
            }

            // Keep email unconfirmed until the owner proves access to the mailbox.
            // Verified-email gates elsewhere must not be bypassed by registration alone.
            user.EmailConfirmed = false;
            await _userManager.UpdateAsync(user);

            // E2E-007 FIX: Create credit wallet with starting credits for new user
            try
            {
                await _creditWalletService.CreateWalletAsync(user.Id);
                _logger.LogInformation("Credit wallet created for user {UserId}", user.Id);
            }
            catch (Exception walletEx)
            {
                _logger.LogCritical(walletEx, "Failed to create credit wallet for user {UserId} — manual wallet creation required", user.Id);
            }

            // Send welcome email (fire and forget - don't block registration on email delivery)
            try
            {
                var userName = $"{registerDto.FirstName} {registerDto.LastName}".Trim();
                if (string.IsNullOrEmpty(userName)) userName = registerDto.Email;

                var emailSent = await _emailService.SendWelcomeEmailAsync(registerDto.Email, userName);
                if (emailSent)
                {
                    _logger.LogInformation("Welcome email sent successfully to {Email}", registerDto.Email);
                }
                else
                {
                    _logger.LogWarning("Failed to send welcome email to {Email} - email service returned false", registerDto.Email);
                }
            }
            catch (Exception emailEx)
            {
                // Log but don't fail registration if email fails
                _logger.LogError(emailEx, "Failed to send welcome email to {Email}", registerDto.Email);
            }

            await EnrollSignupSequencesAsync(registerDto);

            // Log successful registration
            await _auditLogService.LogEventAsync(
                user.Id,
                AuditActions.USER_REGISTRATION,
                ipAddress,
                userAgent,
                true,
                JsonSerializer.Serialize(new { Email = registerDto.Email, UserId = user.Id })
            );

            _logger.LogInformation("User registered successfully: {UserId}", user.Id);

            return new RegisterUserResponseDto
            {
                UserId = user.Id,
                Email = registerDto.Email,
                Success = true,
                Message = "Registration successful! You can now log in and start using the platform."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration for email: {Email}", registerDto.Email);

            await _auditLogService.LogEventAsync(
                null,
                AuditActions.USER_REGISTRATION,
                ipAddress,
                userAgent,
                false,
                JsonSerializer.Serialize(new { Email = registerDto.Email }),
                ex.Message
            );

            return new RegisterUserResponseDto
            {
                UserId = Guid.Empty,
                Email = registerDto.Email,
                Success = false,
                Message = "An error occurred during registration. Please try again."
            };
        }
    }

    private async Task EnrollSignupSequencesAsync(RegisterUserDto registerDto)
    {
        var properties = new Dictionary<string, object?>
        {
            ["first_name"] = registerDto.FirstName,
            ["last_name"] = registerDto.LastName,
            ["product"] = "skillledger"
        };

        foreach (var sequenceSlug in new[] { "skillledger-fulfillment-welcome", "skillledger-nurture-value-1" })
        {
            try
            {
                await _sequencerClient.EnrollAsync(
                    registerDto.Email,
                    sequenceSlug,
                    "skillledger_signup",
                    properties);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Sequencer enrollment failed for {Email} into {SequenceSlug}", registerDto.Email, sequenceSlug);
            }
        }
    }

    public async Task<bool> IsEmailAvailableAsync(string email)
    {
        // Always return true to prevent email enumeration attacks
        // The actual check is done during registration
        await Task.CompletedTask;
        return true;
    }

    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _userManager.FindByEmailAsync(email);
    }

    public async Task<bool> UpdateEmailVerificationStatusAsync(Guid userId, bool isEmailVerified, string ipAddress)
    {
        try
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Attempt to verify non-existent user: {UserId}", userId);
                return false;
            }

            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedFromIP = ipAddress;

            // Email verification is no longer required - user is always Active
            user.Status = UserStatus.Active;

            await _context.SaveChangesAsync();

            await _auditLogService.LogEventAsync(
                userId,
                AuditActions.USER_PROFILE_UPDATE,
                ipAddress,
                null,
                true,
                JsonSerializer.Serialize(new { UserId = userId, EmailVerified = isEmailVerified })
            );

            _logger.LogInformation("Email verification status updated for user: {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating email verification status for user: {UserId}", userId);

            await _auditLogService.LogEventAsync(
                userId,
                AuditActions.USER_PROFILE_UPDATE,
                ipAddress,
                null,
                false,
                JsonSerializer.Serialize(new { UserId = userId, EmailVerified = isEmailVerified }),
                ex.Message
            );

            return false;
        }
    }

    public async Task<ServiceResponseDto> UpdatePasswordAsync(Guid userId, string newPassword)
    {
        try
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Attempt to update password for non-existent user: {UserId}", userId);
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "User not found."
                };
            }

            // Validate password using static password validator
            var passwordValidation = PasswordValidator.ValidatePassword(newPassword);
            if (!passwordValidation.IsValid)
            {
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "Password does not meet security requirements.",
                    ErrorDetails = string.Join(", ", passwordValidation.Errors)
                };
            }

            // Use Identity's built-in password management to properly handle hashing and security stamp
            var result = await _userManager.RemovePasswordAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Failed to remove existing password for user {UserId}: {Errors}", userId, errors);
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "Failed to update password.",
                    ErrorDetails = errors
                };
            }

            result = await _userManager.AddPasswordAsync(user, newPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Failed to add new password for user {UserId}: {Errors}", userId, errors);
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "Failed to update password.",
                    ErrorDetails = errors
                };
            }

            // Update user metadata
            user.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation("Password updated successfully for user: {UserId}", userId);

            return new ServiceResponseDto
            {
                Success = true,
                Message = "Password updated successfully."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating password for user: {UserId}", userId);

            return new ServiceResponseDto
            {
                Success = false,
                Message = "An error occurred while updating the password.",
                ErrorDetails = ex.Message
            };
        }
    }
}
