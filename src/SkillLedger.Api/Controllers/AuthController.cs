using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SkillLedger.Api.Attributes;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Interfaces;
using SkillLedger.Core.Validators;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using CoreAuthService = SkillLedger.Core.Interfaces.IAuthenticationService;

namespace SkillLedger.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : BaseApiController
{
    private readonly IUserService _userService;
    private readonly IPasswordResetService _passwordResetService;
    private readonly CoreAuthService _authenticationService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IUserService userService,
        IPasswordResetService passwordResetService,
        CoreAuthService authenticationService,
        IAuditLogService auditLogService,
        ILogger<AuthController> logger)
    {
        _userService = userService;
        _passwordResetService = passwordResetService;
        _authenticationService = authenticationService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user account
    /// </summary>
    /// <param name="registerDto">Registration details</param>
    /// <returns>Registration result</returns>
    [HttpPost("register")]
    [EnableRateLimiting("RegistrationPolicy")]
    [SkillLedger.Api.Attributes.IgnoreAntiforgeryToken]  // API endpoints use JWT auth instead of CSRF tokens
    [ProducesResponseType(typeof(RegisterUserResponseDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.TooManyRequests)]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto registerDto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .SelectMany(x => x.Value?.Errors ?? new Microsoft.AspNetCore.Mvc.ModelBinding.ModelErrorCollection())
                .Select(x => x.ErrorMessage)
                .ToList();

            await LogFailedAttempt("USER_REGISTRATION", $"Validation failed: {string.Join(", ", errors)}");
            return BadRequest(ModelState);
        }

        var ipAddress = GetClientIpAddress();
        var userAgent = Request.Headers.UserAgent.ToString();

        // Check for recent failed attempts from this IP
        var recentFailedAttempts = await _auditLogService.GetRecentFailedAttemptsAsync(ipAddress, 1);
        if (recentFailedAttempts >= 10) // Additional rate limiting beyond the policy
        {
            await LogFailedAttempt("RATE_LIMIT_EXCEEDED", $"Too many failed attempts: {recentFailedAttempts}");
            return StatusCode(429, new { message = "Too many failed attempts. Please try again later." });
        }

        var result = await _userService.RegisterUserAsync(registerDto, ipAddress, userAgent);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        // Automatically sign in the user after registration using cookie authentication
        if (result.UserId != Guid.Empty)
        {
            try
            {
                var loginRequest = new LoginRequestDto
                {
                    Email = registerDto.Email,
                    Password = registerDto.Password,
                    RememberMe = false
                };

                var authResult = await _authenticationService.AuthenticateAsync(loginRequest, ipAddress, userAgent);

                if (authResult.Success && authResult.User != null)
                {
                    // Update registration response with user profile
                    result.User = authResult.User;
                    result.Message = "Registration successful! You are now logged in.";

                    _logger.LogInformation("New user {Email} signed in automatically with cookie authentication", registerDto.Email);
                }
                else
                {
                    _logger.LogWarning("Failed to authenticate newly registered user: {UserId}, Error: {Error}", result.UserId, authResult.Message);
                    // Still return successful registration
                    result.Message = "Registration successful! Please log in to continue.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error authenticating newly registered user: {UserId}", result.UserId);
                // Still return successful registration
                result.Message = "Registration successful! Please log in to continue.";
            }
        }

        return Ok(result);
    }





    /// <summary>
    /// Check if email is available (returns generic response to prevent enumeration)
    /// </summary>
    /// <param name="email">Email address to check</param>
    /// <returns>Generic response to prevent email enumeration attacks</returns>
    [HttpGet("check-email")]
    [EnableRateLimiting("RegistrationPolicy")]
    [ProducesResponseType(typeof(EmailAvailabilityDto), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> CheckEmailAvailability([FromQuery][Required] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new { message = "Email address is required." });
        }

        // BUG-CRITICAL-002 FIX: Use cryptographically secure RNG for timing attack prevention
        // Random() is predictable; use RandomNumberGenerator for security-sensitive operations
        var delay = RandomNumberGenerator.GetInt32(100, 500);
        await Task.Delay(delay);

        // Always return the same response regardless of email availability to prevent enumeration
        return Ok(new EmailAvailabilityDto
        {
            Email = email,
            IsAvailable = true, // Always return true to prevent enumeration
            Message = "If this email is registered, you will receive further instructions during registration."
        });
    }

    /// <summary>
    /// Get anti-forgery token for CSRF protection
    /// </summary>
    /// <returns>Anti-forgery token</returns>
    [HttpGet("csrf-token")]
    [EnableRateLimiting("GeneralApiPolicy")] // BUG-034 FIX: Add rate limiting to prevent DoS
    [ProducesResponseType(typeof(CsrfTokenDto), (int)HttpStatusCode.OK)]
    public IActionResult GetCsrfToken()
    {
        var tokens = HttpContext.RequestServices.GetService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>();
        var tokenSet = tokens?.GetAndStoreTokens(HttpContext);

        return Ok(new CsrfTokenDto
        {
            Token = tokenSet?.RequestToken ?? string.Empty,
            HeaderName = "X-CSRF-TOKEN"
        });
    }

    /// <summary>
    /// Authenticate user using cookie-based authentication
    /// </summary>
    /// <param name="loginRequest">Login credentials</param>
    /// <returns>Login response if authentication successful</returns>
    [HttpPost("login")]
    [EnableRateLimiting("LoginPolicy")]
    [SkillLedger.Api.Attributes.IgnoreAntiforgeryToken]  // Cookie-based authentication
    [ProducesResponseType(typeof(LoginResponseDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType((int)HttpStatusCode.Locked)]
    [ProducesResponseType((int)HttpStatusCode.TooManyRequests)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto loginRequest)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .SelectMany(x => x.Value?.Errors ?? new Microsoft.AspNetCore.Mvc.ModelBinding.ModelErrorCollection())
                .Select(x => x.ErrorMessage)
                .ToList();

            await LogFailedAttempt("USER_LOGIN_VALIDATION_FAILED", $"Login validation failed: {string.Join(", ", errors)}");
            return BadRequest(ModelState);
        }

        var ipAddress = GetClientIpAddress();
        var userAgent = Request.Headers.UserAgent.ToString();

        var result = await _authenticationService.AuthenticateAsync(loginRequest, ipAddress, userAgent);

        if (!result.Success)
        {
            if (result.IsLockedOut)
            {
                return StatusCode(423, result); // 423 Locked
            }

            return Unauthorized(result);
        }

        // SignInManager already created the authentication cookie
        _logger.LogInformation("User {Email} signed in successfully with cookie authentication", loginRequest.Email);

        return Ok(result);
    }

    /// <summary>
    /// Logout user by signing out of cookie authentication
    /// </summary>
    /// <returns>Logout result</returns>
    [HttpPost("logout")]
    [Authorize] // Cookie-based authentication only
    [EnableRateLimiting("DefaultPolicy")]
    [ProducesResponseType(typeof(LogoutResponseDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized(new { message = "Invalid user session." });
        }

        var ipAddress = GetClientIpAddress();
        var result = await _authenticationService.LogoutAsync(userId, ipAddress);

        return Ok(result);
    }

    /// <summary>
    /// Logout user from all devices (cookie-based, same as regular logout)
    /// </summary>
    /// <returns>Logout result</returns>
    [HttpPost("logout-all")]
    [Authorize] // Cookie-based authentication only
    [EnableRateLimiting("DefaultPolicy")]
    [ProducesResponseType(typeof(LogoutResponseDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> LogoutFromAllDevices()
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized(new { message = "Invalid user session." });
        }

        var ipAddress = GetClientIpAddress();
        var result = await _authenticationService.LogoutFromAllDevicesAsync(userId, ipAddress);

        return Ok(result);
    }

    /// <summary>
    /// Refresh the current session (extends cookie expiration)
    /// </summary>
    /// <returns>Refresh result with success status</returns>
    [HttpPost("refresh")]
    [Authorize] // Cookie-based authentication only
    [EnableRateLimiting("TokenRefreshPolicy")]
    [ProducesResponseType(typeof(RefreshResponseDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> RefreshSession()
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized(new RefreshResponseDto { Success = false, Message = "Invalid user session." });
        }

        // For cookie-based authentication, the session is already valid if we get here
        // The [Authorize] attribute validates the cookie
        // We can optionally refresh the authentication cookie to extend its lifetime
        var ipAddress = GetClientIpAddress();
        var userAgent = Request.Headers.UserAgent.ToString();

        try
        {
            // Re-sign in the user to extend the cookie expiration
            var userProfile = await _authenticationService.GetCurrentUserFromContextAsync(userId);
            if (userProfile != null)
            {
                _logger.LogInformation("Session refreshed for user {UserId} from {IpAddress}", userId, ipAddress);
                return Ok(new RefreshResponseDto
                {
                    Success = true,
                    Message = "Session refreshed successfully.",
                    User = userProfile
                });
            }

            return Unauthorized(new RefreshResponseDto { Success = false, Message = "User not found." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing session for user {UserId}", userId);
            return Unauthorized(new RefreshResponseDto { Success = false, Message = "Failed to refresh session." });
        }
    }

    /// <summary>
    /// Get current authenticated user profile
    /// </summary>
    /// <returns>User profile information</returns>
    [HttpGet("me")]
    [Authorize] // Cookie-based authentication only
    [EnableRateLimiting("DefaultPolicy")]  // E2E-001 FIX: Use high-limit policy instead of RegistrationPolicy
    [ProducesResponseType(typeof(UserProfileDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> GetCurrentUser()
    {
        // User is now authenticated via cookie authentication (primary scheme)
        if (User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdClaim, out var userId))
            {
                var userProfile = await _authenticationService.GetCurrentUserFromContextAsync(userId);
                if (userProfile != null)
                {
                    return Ok(new { success = true, user = userProfile });
                }
                else
                {
                    _logger.LogWarning("User authenticated but profile not found: {UserId}", userId);
                    return Unauthorized(new { message = "User profile not found." });
                }
            }
            else
            {
                _logger.LogWarning("User authenticated but invalid user ID claim found");
                return Unauthorized(new { message = "Invalid user identification." });
            }
        }

        // This should not happen with [Authorize] attribute, but added for safety
        _logger.LogWarning("GetCurrentUser called but user is not authenticated");
        return Unauthorized(new { message = "Authentication required." });
    }

    /// <summary>
    /// Check authentication status without returning user data
    /// </summary>
    /// <returns>Authentication status</returns>
    [HttpGet("status")]
    [EnableRateLimiting("DefaultPolicy")]
    [ProducesResponseType(typeof(object), (int)HttpStatusCode.OK)]
    public IActionResult GetAuthenticationStatus()
    {
        var isAuthenticated = User.Identity?.IsAuthenticated == true;

        if (isAuthenticated)
        {
            var userId = GetCurrentUserId();
            var email = GetCurrentUserEmail();

            return Ok(new
            {
                isAuthenticated = true,
                user = new
                {
                    id = userId.ToString(),
                    email = email
                },
                timestamp = DateTime.UtcNow
            });
        }

        return Ok(new
        {
            isAuthenticated = false,
            user = (object?)null,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Initiate password reset by sending reset email
    /// </summary>
    /// <param name="forgotPasswordRequest">Email address to send reset instructions to</param>
    /// <returns>Generic success message to prevent email enumeration</returns>
    [HttpPost("forgot-password")]
    [EnableRateLimiting("PasswordResetPolicy")]
    [SkillLedger.Api.Attributes.IgnoreAntiforgeryToken]
    [ProducesResponseType(typeof(ForgotPasswordResponseDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.TooManyRequests)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto forgotPasswordRequest)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .SelectMany(x => x.Value?.Errors ?? new Microsoft.AspNetCore.Mvc.ModelBinding.ModelErrorCollection())
                .Select(x => x.ErrorMessage)
                .ToList();

            await LogFailedAttempt("PASSWORD_RESET_VALIDATION_FAILED", $"Validation failed: {string.Join(", ", errors)}");
            return BadRequest(ModelState);
        }

        var ipAddress = GetClientIpAddress();
        var userAgent = Request.Headers.UserAgent.ToString();

        var result = await _passwordResetService.InitiatePasswordResetAsync(
            forgotPasswordRequest.Email,
            ipAddress,
            userAgent);

        if (!result.Success)
        {
            if (result.Message.Contains("Too many"))
            {
                return StatusCode(429, new ForgotPasswordResponseDto
                {
                    Success = false,
                    Message = result.Message
                });
            }

            return BadRequest(new ForgotPasswordResponseDto
            {
                Success = false,
                Message = result.Message
            });
        }

        return Ok(new ForgotPasswordResponseDto
        {
            Success = true,
            Message = result.Message
        });
    }

    /// <summary>
    /// Validate a password reset token
    /// </summary>
    /// <param name="token">Reset token to validate</param>
    /// <returns>Token validity status</returns>
    [HttpGet("validate-reset-token")]
    [EnableRateLimiting("PasswordResetPolicy")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> ValidateResetToken([FromQuery][Required] string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return BadRequest(new { message = "Reset token is required." });
        }

        var isValid = await _passwordResetService.ValidateResetTokenAsync(token);

        return Ok(new
        {
            valid = isValid,
            message = isValid ? "Token is valid" : "Invalid or expired token"
        });
    }

    /// <summary>
    /// Complete password reset with new password
    /// </summary>
    /// <param name="resetPasswordRequest">Reset token and new password</param>
    /// <returns>Password reset result</returns>
    [HttpPost("reset-password")]
    [EnableRateLimiting("PasswordResetPolicy")]
    [SkillLedger.Api.Attributes.IgnoreAntiforgeryToken]
    [ProducesResponseType(typeof(ResetPasswordResponseDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.TooManyRequests)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto resetPasswordRequest)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .SelectMany(x => x.Value?.Errors ?? new Microsoft.AspNetCore.Mvc.ModelBinding.ModelErrorCollection())
                .Select(x => x.ErrorMessage)
                .ToList();

            await LogFailedAttempt("PASSWORD_RESET_VALIDATION_FAILED", $"Validation failed: {string.Join(", ", errors)}");
            return BadRequest(ModelState);
        }

        var ipAddress = GetClientIpAddress();
        var userAgent = Request.Headers.UserAgent.ToString();

        var result = await _passwordResetService.CompletePasswordResetAsync(
            resetPasswordRequest.Token,
            resetPasswordRequest.NewPassword,
            ipAddress,
            userAgent);

        if (!result.Success)
        {
            var isExpired = result.Message.Contains("expired") || result.Message.Contains("used");

            return BadRequest(new ResetPasswordResponseDto
            {
                Success = false,
                Message = result.Message,
                TokenExpired = isExpired
            });
        }

        return Ok(new ResetPasswordResponseDto
        {
            Success = true,
            Message = result.Message,
            TokenExpired = false
        });
    }

    /// <summary>
    /// Check remaining password reset attempts for an email
    /// </summary>
    /// <param name="email">Email address to check</param>
    /// <returns>Remaining attempts count</returns>
    [HttpGet("password-reset-attempts")]
    [EnableRateLimiting("PasswordResetPolicy")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> GetPasswordResetAttempts([FromQuery][Required] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new { message = "Email address is required." });
        }

        var remainingAttempts = await _passwordResetService.GetRemainingResetAttemptsAsync(email);
        var canRequest = await _passwordResetService.CanRequestPasswordResetAsync(email);

        return Ok(new
        {
            remainingAttempts = remainingAttempts,
            canRequestReset = canRequest
        });
    }

    private new Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    private new string? GetCurrentUserEmail()
    {
        return User.FindFirst(ClaimTypes.Email)?.Value;
    }

    private string GetClientIpAddress()
    {
        return SkillLedger.Infrastructure.Services.TrustedClientIpResolver.GetClientIpAddress(HttpContext, "unknown");
    }

    private async Task LogFailedAttempt(string action, string details)
    {
        var ipAddress = GetClientIpAddress();
        var userAgent = Request.Headers.UserAgent.ToString();

        await _auditLogService.LogEventAsync(
            null,
            action,
            ipAddress,
            userAgent,
            false,
            details
        );
    }

}

// DTOs for the endpoints

public class EmailAvailabilityDto
{
    public required string Email { get; set; }
    public bool IsAvailable { get; set; }
    public required string Message { get; set; }
}

public class CsrfTokenDto
{
    public required string Token { get; set; }
    public required string HeaderName { get; set; }
}

public class PhoneVerificationAttemptsDto
{
    public required string PhoneNumber { get; set; }
    public int RemainingAttempts { get; set; }
    public bool CanRequestNewCode { get; set; }
}

public class RefreshResponseDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public object? User { get; set; }
}

