using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using SkillLedger.Infrastructure.Services;
using SkillLedger.Tests.Infrastructure;
using System.Net;
using System.Security.Claims;
using Xunit;

namespace SkillLedger.Tests.Unit.Services;

/// <summary>
/// Unit tests for ControllerHelperService
/// Focus: IP extraction edge cases, user ID validation
/// </summary>
[UnitTest]
[CoreTest]
public class ControllerHelperServiceTests
{
    private readonly ControllerHelperService _service;

    public ControllerHelperServiceTests()
    {
        _service = new ControllerHelperService();
    }

    #region GetCurrentUserId Tests

    [Fact]
    public void GetCurrentUserId_ValidGuidClaim_ReturnsUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = _service.GetCurrentUserId(principal);

        // Assert
        result.Should().Be(userId);
    }

    [Fact]
    public void GetCurrentUserId_MissingClaim_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var claims = new List<Claim>();
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        // Act & Assert
        var act = () => _service.GetCurrentUserId(principal);
        act.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("User ID not found in token");
    }

    [Fact]
    public void GetCurrentUserId_EmptyStringClaim_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "")
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        // Act & Assert
        var act = () => _service.GetCurrentUserId(principal);
        act.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("User ID not found in token");
    }

    [Fact]
    public void GetCurrentUserId_InvalidGuidFormat_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "not-a-valid-guid")
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        // Act & Assert
        var act = () => _service.GetCurrentUserId(principal);
        act.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("User ID not found in token");
    }

    [Fact]
    public void GetCurrentUserId_NullClaim_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "testuser") // Different claim type
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        // Act & Assert
        var act = () => _service.GetCurrentUserId(principal);
        act.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("User ID not found in token");
    }

    #endregion

    #region GetClientIpAddress Tests - Spoofed Forwarded Headers

    [Fact]
    public void GetClientIpAddress_XForwardedForSingleIp_ReturnsRemoteIpAddress()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["X-Forwarded-For"] = "203.0.113.195";
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.5");

        // Act
        var result = _service.GetClientIpAddress(httpContext);

        // Assert
        result.Should().Be("10.0.0.5");
    }

    [Fact]
    public void GetClientIpAddress_XForwardedForMultipleIps_ReturnsRemoteIpAddress()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["X-Forwarded-For"] = "203.0.113.195, 70.41.3.18, 150.172.238.178";
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.5");

        // Act
        var result = _service.GetClientIpAddress(httpContext);

        // Assert
        result.Should().Be("10.0.0.5");
    }

    [Fact]
    public void GetClientIpAddress_XForwardedForWithSpaces_ReturnsRemoteIpAddress()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["X-Forwarded-For"] = " 203.0.113.195 , 70.41.3.18 ";
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.5");

        // Act
        var result = _service.GetClientIpAddress(httpContext);

        // Assert
        result.Should().Be("10.0.0.5");
    }

    [Fact]
    public void GetClientIpAddress_XForwardedForEmptyString_IgnoresXRealIp()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["X-Forwarded-For"] = "";
        httpContext.Request.Headers["X-Real-IP"] = "192.168.1.1";
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.5");

        // Act
        var result = _service.GetClientIpAddress(httpContext);

        // Assert
        result.Should().Be("10.0.0.5");
    }

    #endregion

    #region GetClientIpAddress Tests - X-Real-IP Header

    [Fact]
    public void GetClientIpAddress_XRealIpOnly_ReturnsRemoteIpAddress()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["X-Real-IP"] = "192.168.1.100";
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.5");

        // Act
        var result = _service.GetClientIpAddress(httpContext);

        // Assert
        result.Should().Be("10.0.0.5");
    }

    [Fact]
    public void GetClientIpAddress_BothXForwardedForAndXRealIp_IgnoresBothHeaders()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["X-Forwarded-For"] = "203.0.113.195";
        httpContext.Request.Headers["X-Real-IP"] = "192.168.1.100";
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.5");

        // Act
        var result = _service.GetClientIpAddress(httpContext);

        // Assert
        result.Should().Be("10.0.0.5");
    }

    #endregion

    #region GetClientIpAddress Tests - RemoteIpAddress Fallback

    [Fact]
    public void GetClientIpAddress_NoHeaders_ReturnsRemoteIpAddress()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.5");

        // Act
        var result = _service.GetClientIpAddress(httpContext);

        // Assert
        result.Should().Be("10.0.0.5");
    }

    [Fact]
    public void GetClientIpAddress_NoHeadersAndNullRemoteIp_ReturnsUnknown()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        httpContext.Connection.RemoteIpAddress = null;

        // Act
        var result = _service.GetClientIpAddress(httpContext);

        // Assert
        result.Should().Be("Unknown");
    }

    [Fact]
    public void GetClientIpAddress_IPv6Address_ReturnsIPv6String()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("2001:0db8:85a3:0000:0000:8a2e:0370:7334");

        // Act
        var result = _service.GetClientIpAddress(httpContext);

        // Assert
        result.Should().Be("2001:db8:85a3::8a2e:370:7334");
    }

    [Fact]
    public void GetClientIpAddress_LocalhostIPv4_ReturnsLoopback()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Loopback;

        // Act
        var result = _service.GetClientIpAddress(httpContext);

        // Assert
        result.Should().Be("127.0.0.1");
    }

    [Fact]
    public void GetClientIpAddress_LocalhostIPv6_ReturnsIPv6Loopback()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.IPv6Loopback;

        // Act
        var result = _service.GetClientIpAddress(httpContext);

        // Assert
        result.Should().Be("::1");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void GetClientIpAddress_XForwardedForOnlyCommas_ReturnsRemoteIpAddress()
    {
        // Arrange - Edge case: X-Forwarded-For contains only commas
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["X-Forwarded-For"] = ",,,";
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.5");

        // Act
        var result = _service.GetClientIpAddress(httpContext);

        // Assert
        result.Should().Be("10.0.0.5");
    }

    [Fact]
    public void GetClientIpAddress_XForwardedForWhitespaceOnly_ReturnsRemoteIpAddress()
    {
        // Arrange - Edge case: X-Forwarded-For contains only whitespace
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["X-Forwarded-For"] = "   ";
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.5");

        // Act
        var result = _service.GetClientIpAddress(httpContext);

        // Assert
        result.Should().Be("10.0.0.5");
    }

    [Fact]
    public void GetClientIpAddress_PrivateIPRanges_ReturnsRemoteIpAddress()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["X-Forwarded-For"] = "10.0.0.1";
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.5");

        // Act
        var result = _service.GetClientIpAddress(httpContext);

        // Assert
        result.Should().Be("10.0.0.5");
    }

    #endregion

    #region Helper Methods

    private static HttpContext CreateHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Clear();
        return httpContext;
    }

    #endregion
}
