namespace SkillLedger.Api.Attributes;

/// <summary>
/// DEPRECATED: This attribute is being phased out.
///
/// SECURITY WARNING: SkillLedger uses COOKIE-BASED authentication, NOT JWT.
/// Using this attribute on cookie-authenticated endpoints creates CSRF vulnerabilities.
///
/// Only use for:
/// 1. Public endpoints (no authentication required)
/// 2. Webhook endpoints with signature validation
/// 3. Testing endpoints (disabled in production)
///
/// DO NOT USE for cookie-authenticated endpoints - they MUST have CSRF protection.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public class IgnoreAntiforgeryTokenAttribute : Microsoft.AspNetCore.Mvc.IgnoreAntiforgeryTokenAttribute
{
}
