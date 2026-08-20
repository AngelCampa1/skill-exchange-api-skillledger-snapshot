using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace SkillLedger.Infrastructure.Services;

/// <summary>
/// Helper service for common controller operations
/// Centralizes GetCurrentUserId and GetClientIpAddress to eliminate duplicate code across 8 controllers
/// </summary>
public class ControllerHelperService
{
    /// <summary>
    /// Extracts the current user ID from the ClaimsPrincipal
    /// </summary>
    /// <param name="user">The ClaimsPrincipal from the controller User property</param>
    /// <returns>The user's GUID</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when user ID is not found in token</exception>
    public Guid GetCurrentUserId(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }

        return userId;
    }

    /// <summary>
    /// Extracts the client IP address after ForwardedHeadersMiddleware has applied
    /// the configured trusted proxy rules.
    /// </summary>
    /// <param name="httpContext">The HttpContext from the controller</param>
    /// <returns>The client IP address or "Unknown" if not found</returns>
    public string GetClientIpAddress(HttpContext httpContext)
    {
        return TrustedClientIpResolver.GetClientIpAddress(httpContext);
    }
}
