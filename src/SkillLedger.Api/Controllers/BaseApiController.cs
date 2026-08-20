using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SkillLedger.Api.Middleware; // For GetCorrelationId extension method
using SkillLedger.Infrastructure.Services;

namespace SkillLedger.Api.Controllers;

/// <summary>
/// Base controller with common functionality for all API controllers
/// Includes correlation ID support, user context, and standardized responses
/// </summary>
[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// BUG-041 FIX: Get the correlation ID for the current request
    /// </summary>
    protected string? CorrelationId => HttpContext.GetCorrelationId();

    /// <summary>
    /// BUG-HIGH-005 FIX: Get the current user's ID from claims with proper null handling
    /// </summary>
    /// <returns>User ID from claims</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when user ID claim is missing or invalid</exception>
    protected Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }

    /// <summary>
    /// BUG-HIGH-005 FIX: Safely attempts to extract user ID without throwing exceptions
    /// Useful for optional/anonymous endpoints
    /// </summary>
    /// <returns>User ID if found and valid, null otherwise</returns>
    protected Guid? TryGetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)
            ? null
            : userId;
    }

    /// <summary>
    /// Get the current user's email from claims
    /// </summary>
    protected string? GetCurrentUserEmail()
    {
        return User.FindFirst(ClaimTypes.Email)?.Value;
    }

    /// <summary>
    /// Get the client's IP address (hashed for privacy protection)
    /// </summary>
    protected string GetClientIPAddress()
    {
        var ipAddress = TrustedClientIpResolver.GetClientIpAddress(HttpContext, "unknown");

        // PRIVACY PROTECTION: Hash IP addresses before storing/returning
        return !string.IsNullOrEmpty(ipAddress) ? HashIpAddress(ipAddress) : "unknown";
    }

    /// <summary>
    /// Hash IP address for privacy protection while maintaining uniqueness
    /// </summary>
    private string HashIpAddress(string ipAddress)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(ipAddress + "skillLedger-salt-2024"));
        return BitConverter.ToString(hashedBytes).Replace("-", "").ToLowerInvariant()[..16];
    }

    /// <summary>
    /// BUG-042 FIX: Create a standardized error response with correlation ID
    /// </summary>
    protected ObjectResult Error(string message, int statusCode = 400)
    {
        var response = new
        {
            success = false,
            message,
            correlationId = CorrelationId,
            timestamp = DateTime.UtcNow
        };

        return StatusCode(statusCode, response);
    }

    /// <summary>
    /// BUG-042 FIX: Create a standardized success response
    /// </summary>
    protected ObjectResult Success<T>(T data, string? message = null)
    {
        var response = new
        {
            success = true,
            message = message ?? "Operation completed successfully",
            data,
            correlationId = CorrelationId,
            timestamp = DateTime.UtcNow
        };

        return Ok(response);
    }

    /// <summary>
    /// BUG-042 FIX: Create a standardized validation error response
    /// </summary>
    protected ObjectResult ValidationError(Dictionary<string, string[]> errors)
    {
        var response = new
        {
            success = false,
            message = "Validation failed",
            errors,
            correlationId = CorrelationId,
            timestamp = DateTime.UtcNow
        };

        return BadRequest(response);
    }
}

