using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace SkillLedger.Tests.Infrastructure;

/// <summary>
/// Test authentication handler that accepts any request with test claims
/// This allows integration tests to authenticate as specific users without cookies or JWT tokens
/// </summary>
public class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string AuthenticationScheme = "TestScheme";

    public TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check if test claims are present in the request headers
        if (!Context.Request.Headers.ContainsKey("X-Test-UserId"))
        {
            // No test authentication - return no result (not a failure, just not authenticated)
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var userId = Context.Request.Headers["X-Test-UserId"].ToString();
        var email = Context.Request.Headers["X-Test-Email"].ToString();
        var roles = Context.Request.Headers["X-Test-Roles"].ToString();
        var permissions = Context.Request.Headers["X-Test-Permissions"].ToString();

        // Build claims from test headers
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, email),
            new Claim("sub", userId) // JWT standard claim
        };

        // Add roles if provided
        if (!string.IsNullOrEmpty(roles))
        {
            foreach (var role in roles.Split(','))
            {
                claims.Add(new Claim(ClaimTypes.Role, role.Trim()));
            }
        }

        // Add permissions if provided
        if (!string.IsNullOrEmpty(permissions))
        {
            foreach (var permission in permissions.Split(','))
            {
                claims.Add(new Claim("permission", permission.Trim()));
            }
        }

        var identity = new ClaimsIdentity(claims, AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        // In test environment, return 401 Unauthorized when authentication is required but not provided
        Context.Response.StatusCode = 401;
        return Task.CompletedTask;
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        // In test environment, return 403 Forbidden instead of throwing
        Context.Response.StatusCode = 403;
        return Task.CompletedTask;
    }
}
