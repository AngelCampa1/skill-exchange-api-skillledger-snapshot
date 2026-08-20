using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Moq;
using SkillLedger.Core.Constants;
using SkillLedger.Infrastructure.Services;
using Xunit;

namespace SkillLedger.Tests.Unit;

/// <summary>
/// Unit tests for SubscriptionAuthorizationHandler
/// Tests subscription authorization logic based on user claims
/// </summary>
public class SubscriptionAuthorizationHandlerTests
{
    private readonly Mock<ILogger<SubscriptionAuthorizationService>> _mockLogger;
    private readonly SubscriptionAuthorizationService _authorizationService;

    public SubscriptionAuthorizationHandlerTests()
    {
        _mockLogger = new Mock<ILogger<SubscriptionAuthorizationService>>();
        _authorizationService = new SubscriptionAuthorizationService(_mockLogger.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldSucceed_WhenUserHasActiveSubscriptionClaim()
    {
        // Arrange
        var user = CreateTestUserWithClaims(new List<(string, object)>
        {
            (SubscriptionClaims.HasActiveSubscription, "true"),
            (SubscriptionClaims.SubscriptionTierName, "Professional"),
            (SubscriptionClaims.SubscriptionStatus, "Active")
        });

        var requirement = new ActiveSubscriptionRequirement();
        var context = new AuthorizationHandlerContext(
            new List<IAuthorizationRequirement> { requirement },
            user,
            null);

        // Debug: Check claims are present
        var activeClaim = user.FindFirst(SubscriptionClaims.HasActiveSubscription)?.Value;
        var statusClaim = user.FindFirst(SubscriptionClaims.SubscriptionStatus)?.Value;
        var tierClaim = user.FindFirst(SubscriptionClaims.SubscriptionTierName)?.Value;

        Assert.Equal("true", activeClaim);
        Assert.Equal("Active", statusClaim);
        Assert.Equal("Professional", tierClaim);

        // Act
        await _authorizationService.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenUserHasNoActiveSubscriptionClaim()
    {
        // Arrange
        var user = CreateTestUserWithClaims(new List<(string, object)>
        {
            (SubscriptionClaims.HasActiveSubscription, "false")
        });

        var requirement = new ActiveSubscriptionRequirement();
        var context = new AuthorizationHandlerContext(
            new List<IAuthorizationRequirement> { requirement },
            user,
            null);

        // Act
        await _authorizationService.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_ShouldSucceed_WhenUserHasBusinessTierOrHigher()
    {
        // Arrange
        var user = CreateTestUserWithClaims(new List<(string, object)>
        {
            (SubscriptionClaims.HasActiveSubscription, "true"),
            (SubscriptionClaims.SubscriptionTierName, "Business"),
            (SubscriptionClaims.SubscriptionStatus, "Active")
        });

        var requirement = new BusinessOrHigherRequirement();
        var context = new AuthorizationHandlerContext(
            new List<IAuthorizationRequirement> { requirement },
            user,
            null);

        // Act
        await _authorizationService.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenUserHasFreeTierForBusinessRequirement()
    {
        // Arrange
        var user = CreateTestUserWithClaims(new List<(string, object)>
        {
            (SubscriptionClaims.HasActiveSubscription, "true"),
            (SubscriptionClaims.SubscriptionTierName, "Free")
        });

        var requirement = new BusinessOrHigherRequirement();
        var context = new AuthorizationHandlerContext(
            new List<IAuthorizationRequirement> { requirement },
            user,
            null);

        // Act
        await _authorizationService.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_ShouldSucceed_WhenUserHasApiAccess()
    {
        // Arrange
        var user = CreateTestUserWithClaims(new List<(string, object)>
        {
            (SubscriptionClaims.HasActiveSubscription, "true"),
            (SubscriptionClaims.HasApiAccess, "true"),
            (SubscriptionClaims.SubscriptionStatus, "Active")
        });

        var requirement = new ApiAccessRequirement();
        var context = new AuthorizationHandlerContext(
            new List<IAuthorizationRequirement> { requirement },
            user,
            null);

        // Act
        await _authorizationService.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenUserDoesNotHaveApiAccess()
    {
        // Arrange
        var user = CreateTestUserWithClaims(new List<(string, object)>
        {
            (SubscriptionClaims.HasActiveSubscription, "true"),
            (SubscriptionClaims.HasApiAccess, "false")
        });

        var requirement = new ApiAccessRequirement();
        var context = new AuthorizationHandlerContext(
            new List<IAuthorizationRequirement> { requirement },
            user,
            null);

        // Act
        await _authorizationService.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_ShouldSucceed_WhenUserIsOnTrialAndTrialIsAllowed()
    {
        // Arrange
        var user = CreateTestUserWithClaims(new List<(string, object)>
        {
            (SubscriptionClaims.HasActiveSubscription, "true"),
            (SubscriptionClaims.IsTrial, "true"),
            (SubscriptionClaims.SubscriptionStatus, "Trial")
        });

        var requirement = new ActiveOrTrialSubscriptionRequirement();
        var context = new AuthorizationHandlerContext(
            new List<IAuthorizationRequirement> { requirement },
            user,
            null);

        // Act
        await _authorizationService.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenUserIsOnTrialAndTrialIsNotAllowed()
    {
        // Arrange
        var user = CreateTestUserWithClaims(new List<(string, object)>
        {
            (SubscriptionClaims.HasActiveSubscription, "true"),
            (SubscriptionClaims.IsTrial, "true")
        });

        var requirement = new ActiveSubscriptionRequirement(); // Trial not allowed
        var context = new AuthorizationHandlerContext(
            new List<IAuthorizationRequirement> { requirement },
            user,
            null);

        // Act
        await _authorizationService.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var user = new System.Security.Claims.ClaimsPrincipal(); // Empty principal (not authenticated)
        var requirement = new ActiveSubscriptionRequirement();
        var context = new AuthorizationHandlerContext(
            new List<IAuthorizationRequirement> { requirement },
            user,
            null);

        // Act
        await _authorizationService.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_ShouldIgnoreNonSubscriptionRequirements()
    {
        // Arrange
        var user = CreateTestUserWithClaims(new List<(string, object)>
        {
            (SubscriptionClaims.HasActiveSubscription, "true")
        });

        // Create a non-subscription requirement
        var nonSubscriptionRequirement = new Mock<IAuthorizationRequirement>().Object;
        var context = new AuthorizationHandlerContext(
            new List<IAuthorizationRequirement> { nonSubscriptionRequirement },
            user,
            null);

        // Act
        await _authorizationService.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded); // Should not succeed as it's not a subscription requirement
    }

    #region Helper Methods

    private static System.Security.Claims.ClaimsPrincipal CreateTestUserWithClaims(List<(string ClaimType, object Value)> claims)
    {
        var claimList = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(System.Security.Claims.ClaimTypes.Email, "test@example.com")
        };

        foreach (var (claimType, value) in claims)
        {
            claimList.Add(new System.Security.Claims.Claim(claimType, value.ToString()));
        }

        return new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(claimList, "TestAuthentication"));
    }

    #endregion
}