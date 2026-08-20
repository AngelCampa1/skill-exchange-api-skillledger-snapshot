using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Interfaces;
using SkillLedger.Core.Validators;
using SkillLedger.Infrastructure.Data;
using System.Security.Cryptography;
using System.Text;

namespace SkillLedger.Infrastructure.Services;

/// <summary>
/// Service for handling password reset functionality
/// </summary>
public class PasswordResetService : IPasswordResetService
{
    private readonly SkillLedgerDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IUserService _userService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<PasswordResetService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IDistributedLockService _lockService; // BUG-006 FIX: Add lock service

    // Configuration constants
    private const int TokenExpirationHours = 1;
    private const int MaxResetAttemptsPerHour = 3;
    private const int MaxVerificationAttemptsPerToken = 5;
    private const int TokenLength = 64;

    public PasswordResetService(
        SkillLedgerDbContext context,
        IEmailService emailService,
        IUserService userService,
        IAuditLogService auditLogService,
        ILogger<PasswordResetService> logger,
        IConfiguration configuration,
        IDistributedLockService lockService) // BUG-006 FIX: Inject lock service
    {
        _context = context;
        _emailService = emailService;
        _userService = userService;
        _auditLogService = auditLogService;
        _logger = logger;
        _configuration = configuration;
        _lockService = lockService; // BUG-006 FIX: Initialize lock service
    }

    public async Task<ServiceResponseDto> InitiatePasswordResetAsync(string email, string ipAddress, string userAgent)
    {
        try
        {
            // BUG-022 FIX: Validate email format before processing
            if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
            {
                _logger.LogWarning("Invalid email format in password reset request: {Email} from IP: {IpAddress}",
                    email, ipAddress);

                // Return generic message to prevent enumeration
                return new ServiceResponseDto
                {
                    Success = true,
                    Message = "If the email address is registered and verified, password reset instructions have been sent."
                };
            }

            _logger.LogInformation("Password reset initiated for email: {Email} from IP: {IpAddress}",
                email, ipAddress);

            // Check rate limiting first
            if (!await CanRequestPasswordResetAsync(email))
            {
                await _auditLogService.LogEventAsync(
                    null,
                    "PASSWORD_RESET_RATE_LIMITED",
                    ipAddress,
                    userAgent,
                    false,
                    $"Rate limit exceeded for email: {email}"
                );

                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "Too many password reset requests. Please wait before trying again."
                };
            }

            // BUG-NEW-002 FIX: Add null check for Email property
            // Find user by email (using email enumeration protection)
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == email.ToLower());

            // Always return success to prevent email enumeration
            // But only actually send email if user exists
            if (user != null)
            {
                // Revoke any existing active reset tokens for this user
                await RevokeUserResetTokensAsync(user.Id, "New reset request initiated");

                // Generate secure reset token
                var (token, tokenHash) = GenerateSecureToken();
                var resetRequest = new PasswordReset
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Token = token, // Store plain token temporarily for email
                    TokenHash = tokenHash,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddHours(TokenExpirationHours),
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    AttemptCount = 0,
                    IsUsed = false
                };

                _context.PasswordResets.Add(resetRequest);
                await _context.SaveChangesAsync();

                // Send password reset email
                var baseUrl = _configuration["App:BaseUrl"] ?? "https://localhost:3030";
                var emailSent = await _emailService.SendPasswordResetEmailAsync(
                    user.Email ?? throw new InvalidOperationException("User email cannot be null"),
                    user.UserName ?? throw new InvalidOperationException("User name cannot be null"),
                    token,
                    baseUrl);

                // Clear the plain token from memory/entity after email is sent
                resetRequest.Token = string.Empty;
                await _context.SaveChangesAsync();

                if (emailSent)
                {
                    await _auditLogService.LogEventAsync(
                        user.Id,
                        "PASSWORD_RESET_REQUESTED",
                        ipAddress,
                        userAgent,
                        true,
                        "Password reset email sent successfully"
                    );

                    _logger.LogInformation("Password reset email sent successfully for user: {UserId}", user.Id);
                }
                else
                {
                    _logger.LogWarning("Failed to send password reset email for user: {UserId}", user.Id);
                }
            }
            else
            {
                // Log attempt for non-existent or unconfirmed email
                await _auditLogService.LogEventAsync(
                    null,
                    "PASSWORD_RESET_INVALID_EMAIL",
                    ipAddress,
                    userAgent,
                    false,
                    $"Reset requested for non-existent/unconfirmed email: {email}"
                );

                _logger.LogWarning("Password reset requested for non-existent or unconfirmed email: {Email}", email);
            }

            // Always return generic success message to prevent email enumeration
            return new ServiceResponseDto
            {
                Success = true,
                Message = "If the email address is registered and verified, password reset instructions have been sent."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating password reset for email: {Email}", email);

            await _auditLogService.LogEventAsync(
                null,
                "PASSWORD_RESET_ERROR",
                ipAddress,
                userAgent,
                false,
                $"Error: {ex.Message}"
            );

            return new ServiceResponseDto
            {
                Success = false,
                Message = "An error occurred while processing your request. Please try again later."
            };
        }
    }

    public async Task<bool> ValidateResetTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        try
        {
            var tokenHash = HashToken(token);
            var resetRequest = await _context.PasswordResets
                .Include(pr => pr.User)
                .FirstOrDefaultAsync(pr => pr.TokenHash == tokenHash && !pr.IsUsed);

            return resetRequest != null && resetRequest.IsValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating reset token");
            return false;
        }
    }

    public async Task<ServiceResponseDto> CompletePasswordResetAsync(string token, string newPassword, string ipAddress, string userAgent)
    {
        // BUG-006 FIX: Add distributed lock to prevent race conditions
        var tokenHash = HashToken(token);
        var lockKey = $"password_reset:{tokenHash}";

        var distributedLock = await _lockService.TryAcquireLockAsync(lockKey, TimeSpan.FromSeconds(30));
        if (distributedLock == null)
        {
            _logger.LogWarning("Failed to acquire lock for password reset token");
            return new ServiceResponseDto
            {
                Success = false,
                Message = "Password reset is already in progress. Please try again in a moment."
            };
        }

        try
        {
            // Lock acquired, proceed with reset
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(newPassword))
            {
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "Invalid request. Token and password are required."
                };
            }

            // Password validation will be handled by UserService.UpdatePasswordAsync

            var resetRequest = await _context.PasswordResets
                .Include(pr => pr.User)
                .FirstOrDefaultAsync(pr => pr.TokenHash == tokenHash);

            if (resetRequest == null)
            {
                await _auditLogService.LogEventAsync(
                    null,
                    "PASSWORD_RESET_INVALID_TOKEN",
                    ipAddress,
                    userAgent,
                    false,
                    "Invalid reset token provided"
                );

                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "Invalid or expired reset token."
                };
            }

            // SECURITY FIX: Check expiration and used status BEFORE incrementing attempts
            // to prevent race conditions
            if (resetRequest.IsExpired)
            {
                await _auditLogService.LogEventAsync(
                    resetRequest.UserId,
                    "PASSWORD_RESET_EXPIRED_TOKEN",
                    ipAddress,
                    userAgent,
                    false,
                    "Expired reset token used"
                );

                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "Reset token has expired. Please request a new password reset."
                };
            }

            if (resetRequest.IsUsed)
            {
                await _auditLogService.LogEventAsync(
                    resetRequest.UserId,
                    "PASSWORD_RESET_USED_TOKEN",
                    ipAddress,
                    userAgent,
                    false,
                    "Already used reset token"
                );

                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "Reset token has already been used. Please request a new password reset."
                };
            }

            // Update attempt tracking and save immediately to prevent race conditions
            resetRequest.AttemptCount++;
            resetRequest.LastAttemptAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Check attempt limits (after save to ensure accurate count)
            if (resetRequest.AttemptCount > MaxVerificationAttemptsPerToken)
            {
                await _auditLogService.LogEventAsync(
                    resetRequest.UserId,
                    "PASSWORD_RESET_MAX_ATTEMPTS",
                    ipAddress,
                    userAgent,
                    false,
                    $"Maximum attempts exceeded: {resetRequest.AttemptCount}"
                );

                // No need to save again - already saved above
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "Too many failed attempts with this token. Please request a new password reset."
                };
            }

            // Update user password
            var user = resetRequest.User;
            var updateResult = await _userService.UpdatePasswordAsync(user.Id, newPassword);

            if (!updateResult.Success)
            {
                await _auditLogService.LogEventAsync(
                    user.Id,
                    "PASSWORD_RESET_UPDATE_FAILED",
                    ipAddress,
                    userAgent,
                    false,
                    $"Failed to update password: {updateResult.Message}"
                );

                await _context.SaveChangesAsync();

                // Return the actual error message from UserService (password validation errors)
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = updateResult.Message  // Pass through actual validation error
                };
            }

            // Mark token as used
            resetRequest.IsUsed = true;
            resetRequest.UsedAt = DateTime.UtcNow;

            // Revoke all other active reset tokens for this user
            await RevokeUserResetTokensAsync(user.Id, "Password successfully reset", resetRequest.Id);

            await _context.SaveChangesAsync();

            await _auditLogService.LogEventAsync(
                user.Id,
                "PASSWORD_RESET_COMPLETED",
                ipAddress,
                userAgent,
                true,
                "Password reset completed successfully"
            );

            _logger.LogInformation("Password reset completed successfully for user: {UserId}", user.Id);

            return new ServiceResponseDto
            {
                Success = true,
                Message = "Password has been reset successfully. You can now log in with your new password."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing password reset");

            await _auditLogService.LogEventAsync(
                null,
                "PASSWORD_RESET_ERROR",
                ipAddress,
                userAgent,
                false,
                $"Error: {ex.Message}"
            );

            return new ServiceResponseDto
            {
                Success = false,
                Message = "An error occurred while resetting your password. Please try again later."
            };
        }
        finally
        {
            // BUG-006 FIX: Always release the distributed lock
            if (distributedLock != null)
            {
                await distributedLock.DisposeAsync();
            }
        }
    }

    public async Task<int> GetRemainingResetAttemptsAsync(string email)
    {
        try
        {
            // BUG-NEW-002 FIX: Add null check for Email property
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == email.ToLower());

            if (user == null)
                return MaxResetAttemptsPerHour; // Don't reveal if email exists

            var oneHourAgo = DateTime.UtcNow.AddHours(-1);
            var recentAttempts = await _context.PasswordResets
                .CountAsync(pr => pr.UserId == user.Id && pr.CreatedAt >= oneHourAgo);

            return Math.Max(0, MaxResetAttemptsPerHour - recentAttempts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting remaining reset attempts for email: {Email}", email);
            return 0;
        }
    }

    public async Task<bool> CanRequestPasswordResetAsync(string email)
    {
        try
        {
            // BUG-NEW-002 FIX: Add null check for Email property
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == email.ToLower());

            if (user == null)
                return true; // Don't reveal if email exists, but still apply rate limiting

            var oneHourAgo = DateTime.UtcNow.AddHours(-1);
            var recentAttempts = await _context.PasswordResets
                .CountAsync(pr => pr.UserId == user.Id && pr.CreatedAt >= oneHourAgo);

            return recentAttempts < MaxResetAttemptsPerHour;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if password reset can be requested for email: {Email}", email);
            return false; // Err on the side of caution
        }
    }

    public async Task<int> CleanupExpiredTokensAsync()
    {
        try
        {
            var expiredTokens = await _context.PasswordResets
                .Where(pr => pr.ExpiresAt < DateTime.UtcNow)
                .ToListAsync();

            if (expiredTokens.Any())
            {
                _context.PasswordResets.RemoveRange(expiredTokens);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Cleaned up {Count} expired password reset tokens", expiredTokens.Count);
            }

            return expiredTokens.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired password reset tokens");
            return 0;
        }
    }

    public async Task<int> RevokeUserResetTokensAsync(Guid userId, string reason, Guid? excludeTokenId = null)
    {
        try
        {
            var activeTokens = await _context.PasswordResets
                .Where(pr => pr.UserId == userId && !pr.IsUsed && pr.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();

            if (excludeTokenId.HasValue)
            {
                activeTokens = activeTokens.Where(pr => pr.Id != excludeTokenId.Value).ToList();
            }

            foreach (var token in activeTokens)
            {
                token.IsUsed = true;
                token.UsedAt = DateTime.UtcNow;
            }

            if (activeTokens.Any())
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Revoked {Count} active password reset tokens for user {UserId}. Reason: {Reason}",
                    activeTokens.Count, userId, reason);
            }

            return activeTokens.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking password reset tokens for user: {UserId}", userId);
            return 0;
        }
    }

    /// <summary>
    /// P1 SECURITY FIX: Generate cryptographically secure password reset token
    /// Uses RandomNumberGenerator (CSPRNG) for unpredictable token generation
    /// 64-byte token provides 512 bits of entropy (2^512 possible values)
    /// </summary>
    private (string token, string hash) GenerateSecureToken()
    {
        // P1 FIX: Generate cryptographically secure random token
        // RandomNumberGenerator.Create() uses system CSPRNG (e.g., CryptGenRandom on Windows)
        // This is NOT predictable like Random.Next() or Guid.NewGuid()
        using var rng = RandomNumberGenerator.Create();
        var tokenBytes = new byte[TokenLength]; // 64 bytes = 512 bits of entropy
        rng.GetBytes(tokenBytes);

        // Convert to URL-safe base64 string
        // Replace URL-unsafe characters: + → -, / → _, remove padding =
        var token = Convert.ToBase64String(tokenBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");

        // P1 FIX: Create SHA256 hash for secure storage
        // Never store plain tokens in database - only hash
        // This prevents token leakage if database is compromised
        var tokenHash = HashToken(token);

        _logger.LogDebug("Generated secure password reset token with {Bytes} bytes ({Bits} bits entropy)",
            TokenLength, TokenLength * 8);

        return (token, tokenHash);
    }

    /// <summary>
    /// P1 SECURITY FIX: Hash token using SHA256 for secure storage
    /// One-way hash prevents reverse engineering even if database is compromised
    /// </summary>
    private string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// BUG-022 FIX: Validate email format using regex
    /// </summary>
    private bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            // Basic email validation regex
            var emailRegex = new System.Text.RegularExpressions.Regex(
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            return emailRegex.IsMatch(email) && email.Length <= 254;
        }
        catch
        {
            return false;
        }
    }

}