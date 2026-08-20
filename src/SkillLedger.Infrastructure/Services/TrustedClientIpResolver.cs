using Microsoft.AspNetCore.Http;

namespace SkillLedger.Infrastructure.Services;

/// <summary>
/// Resolves the client IP after ASP.NET Core ForwardedHeadersMiddleware has applied
/// the configured trusted proxy rules.
/// </summary>
public static class TrustedClientIpResolver
{
    public static string GetClientIpAddress(HttpContext httpContext, string unknownValue = "Unknown")
    {
        return httpContext.Connection.RemoteIpAddress?.ToString() ?? unknownValue;
    }
}
