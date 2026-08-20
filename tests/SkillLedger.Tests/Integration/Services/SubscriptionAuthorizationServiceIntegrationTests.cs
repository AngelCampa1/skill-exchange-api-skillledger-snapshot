using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.Constants;
using SkillLedger.Infrastructure.Services;
using SkillLedger.Tests.Infrastructure;
using System.Security.Claims;
using System.Text.Json;
using Xunit;

namespace SkillLedger.Tests.Integration.Services;

/// <summary>
/// Integration tests for SubscriptionAuthorizationService business logic
/// Following anti-mocking pattern: Pure business logic with no external dependencies
/// </summary>
[IntegrationTest]
public class SubscriptionAuthorizationServiceIntegrationTests
{
    private readonly SubscriptionAuthorizationService _authService;
    private readonly ILogger<SubscriptionAuthorizationService> _logger;

    public SubscriptionAuthorizationServiceIntegrationTests()
    {
        _logger = LoggerFactory
            .Create(builder => builder.AddConsole())
            .CreateLogger<SubscriptionAuthorizationService>();

        _authService = new SubscriptionAuthorizationService(_logger);
    }

    [Fact]
    public async Task HandleAsync_ActiveSubscription_ShouldSucceed()
    {
        // Arrange
        var user = CreateUserWithClaims(new Claim[]
        {
            new(SubscriptionClaims.HasActiveSubscription, "true"),
            new(SubscriptionClaims.SubscriptionStatus, "Active"),
            new(SubscriptionClaims.SubscriptionTierName, "Professional")
        });

        var requirement = new ActiveSubscriptionRequirement();
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await _authService.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_NoActiveSubscription_ShouldFail()
    {
        // Arrange
        var user = CreateUserWithClaims(new Claim[]
        {
            new(SubscriptionClaims.HasActiveSubscription, "false"),
            new(SubscriptionClaims.SubscriptionStatus, "Expired")
        });

        var requirement = new ActiveSubscriptionRequirement();
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await _authService.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_TrialSubscription_AllowTrial_ShouldSucceed()
    {
        // Arrange
        var user = CreateUserWithClaims(new Claim[]
        {
            new(SubscriptionClaims.IsTrial, "true"),
            new(SubscriptionClaims.SubscriptionStatus, "Trial"),
            new(SubscriptionClaims.HasActiveSubscription, "true")
        });

        var requirement = new ActiveOrTrialSubscriptionRequirement();
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await _authService.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_TrialSubscription_NotAllowed_ShouldFail()
    {
        // Arrange
        var user = CreateUserWithClaims(new Claim[]
        {
            new(SubscriptionClaims.IsTrial, "true"),
            new(SubscriptionClaims.SubscriptionStatus, "Trial"),
            new(SubscriptionClaims.HasActiveSubscription, "true")
        });

        var requirement = new ActiveSubscriptionRequirement(); // AllowTrial = false
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await _authService.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_BusinessTier_BusinessOrHigherRequirement_ShouldSucceed()
    {
        // Arrange
        var user = CreateUserWithClaims(new Claim[]
        {
            new(SubscriptionClaims.HasActiveSubscription, "true"),
            new(SubscriptionClaims.SubscriptionStatus, "Active"),
            new(SubscriptionClaims.SubscriptionTierName, "Business")
        });

        var requirement = new BusinessOrHigherRequirement();
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await _authService.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_EnterpriseTier_BusinessOrHigherRequirement_ShouldSucceed()
    {
        // Arrange
        var user = CreateUserWithClaims(new Claim[]
        {
            new(SubscriptionClaims.HasActiveSubscription, "true"),
            new(SubscriptionClaims.SubscriptionStatus, "Active"),
            new(SubscriptionClaims.SubscriptionTierName, "Enterprise")
        });

        var requirement = new BusinessOrHigherRequirement();
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await _authService.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_BasicTier_BusinessOrHigherRequirement_ShouldFail()
    {
        // Arrange
        var user = CreateUserWithClaims(new Claim[]
        {
            new(SubscriptionClaims.HasActiveSubscription, "true"),
            new(SubscriptionClaims.SubscriptionStatus, "Active"),
            new(SubscriptionClaims.SubscriptionTierName, "Basic")
        });

        var requirement = new BusinessOrHigherRequirement();
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await _authService.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_EnterpriseTier_EnterpriseTierRequirement_ShouldSucceed()
    {
        // Arrange
        var user = CreateUserWithClaims(new Claim[]
        {
            new(SubscriptionClaims.HasActiveSubscription, "true"),
            new(SubscriptionClaims.SubscriptionStatus, "Active"),
            new(SubscriptionClaims.SubscriptionTierName, "Enterprise")
        });

        var requirement = new EnterpriseTierRequirement();
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await _authService.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_BusinessTier_EnterpriseTierRequirement_ShouldFail()
    {
        // Arrange
        var user = CreateUserWithClaims(new Claim[]
        {
            new(SubscriptionClaims.HasActiveSubscription, "true"),
            new(SubscriptionClaims.SubscriptionStatus, "Active"),
            new(SubscriptionClaims.SubscriptionTierName, "Business")
        });

        var requirement = new EnterpriseTierRequirement();
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await _authService.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_ApiAccess_WithFeatureClaim_ShouldSucceed()
    {
        // Arrange
        var user = CreateUserWithClaims(new Claim[]
        {
            new(SubscriptionClaims.HasActiveSubscription, "true"),
            new(SubscriptionClaims.SubscriptionStatus, "Active"),
            new("has_api_access", "true")
        });

        var requirement = new ApiAccessRequirement();
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await _authService.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_ApiAccess_WithJsonFeatureArray_ShouldSucceed()
    {
        // Arrange
        var features = new[] { "api_access", "priority_support" };
        var featuresJson = JsonSerializer.Serialize(features);

        var user = CreateUserWithClaims(new Claim[]
        {
            new(SubscriptionClaims.HasActiveSubscription, "true"),
            new(SubscriptionClaims.SubscriptionStatus, "Active"),
            new(SubscriptionClaims.AvailableFeatures, featuresJson)
        });

        var requirement = new ApiAccessRequirement();
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await _authService.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_ApiAccess_WithoutFeature_ShouldFail()
    {
        // Arrange
        var user = CreateUserWithClaims(new Claim[]
        {
            new(SubscriptionClaims.HasActiveSubscription, "true"),
            new(SubscriptionClaims.SubscriptionStatus, "Active"),
            new("has_api_access", "false")
        });

        var requirement = new ApiAccessRequirement();
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await _authService.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_MultipleFeatures_AllPresent_ShouldSucceed()
    {
        // Arrange
        var features = new[] { "advanced_analytics", "priority_support", "api_access" };
        var featuresJson = JsonSerializer.Serialize(features);

        var user = CreateUserWithClaims(new Claim[]
        {
            new(SubscriptionClaims.HasActiveSubscription, "true"),
            new(SubscriptionClaims.SubscriptionStatus, "Active"),
            new(SubscriptionClaims.AvailableFeatures, featuresJson)
        });

        var requirement = new SubscriptionRequirement
        {
            RequiredFeatures = new List<string> { "advanced_analytics", "api_access" }
        };
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await _authService.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_MultipleFeatures_OneMissing_ShouldFail()
    {
        // Arrange
        var features = new[] { "advanced_analytics", "priority_support" };
        var featuresJson = JsonSerializer.Serialize(features);

        var user = CreateUserWithClaims(new Claim[]
        {
            new(SubscriptionClaims.HasActiveSubscription, "true"),
            new(SubscriptionClaims.SubscriptionStatus, "Active"),
            new(SubscriptionClaims.AvailableFeatures, featuresJson)
        });

        var requirement = new SubscriptionRequirement
        {
            RequiredFeatures = new List<string> { "advanced_analytics", "api_access" }
        };
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await _authService.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_MinActiveProjects_Sufficient_ShouldSucceed()
    {
        // Arrange
        var user = CreateUserWithClaims(new Claim[]
        {
            new(SubscriptionClaims.HasActiveSubscription, "true"),
            new(SubscriptionClaims.SubscriptionStatus, "Active"),
            new(SubscriptionClaims.MaxActiveProjects, "10")
        });

        var requirement = new SubscriptionRequirement
        {
            MinMaxActiveProjects = 5
        };
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await _authService.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_MinActiveProjects_Insufficient_ShouldFail()
    {
        // Arrange
        var user = CreateUserWithClaims(new Claim[]
        {
            new(SubscriptionClaims.HasActiveSubscription, "true"),
            new(SubscriptionClaims.SubscriptionStatus, "Active"),
            new(SubscriptionClaims.MaxActiveProjects, "3")
        });

        var requirement = new SubscriptionRequirement
        {
            MinMaxActiveProjects = 5
        };
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await _authService.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_MinTeamMembers_Sufficient_ShouldSucceed()
    {
        // Arrange
        var user = CreateUserWithClaims(new Claim[]
        {
            new(SubscriptionClaims.HasActiveSubscription, "true"),
            new(SubscriptionClaims.SubscriptionStatus, "Active"),
            new(SubscriptionClaims.MaxTeamMembers, "20")
        });

        var requirement = new TeamMemberAccessRequirement(); // MinMaxTeamMembers = 2
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await _authService.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_MinTeamMembers_Insufficient_ShouldFail()
    {
        // Arrange
        var user = CreateUserWithClaims(new Claim[]
        {
            new(SubscriptionClaims.HasActiveSubscription, "true"),
            new(SubscriptionClaims.SubscriptionStatus, "Active"),
            new(SubscriptionClaims.MaxTeamMembers, "1")
        });

        var requirement = new TeamMemberAccessRequirement(); // MinMaxTeamMembers = 2
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await _authService.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_UnlimitedProjects_ShouldSucceed()
    {
        // Arrange
        var user = CreateUserWithClaims(new Claim[]
        {
            new(SubscriptionClaims.HasActiveSubscription, "true"),
            new(SubscriptionClaims.SubscriptionStatus, "Active"),
            new(SubscriptionClaims.MaxActiveProjects, "999")
        });

        var requirement = new UnlimitedProjectsRequirement(); // MinMaxActiveProjects = 999
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await _authService.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_PrioritySupport_ShouldSucceed()
    {
        // Arrange
        var user = CreateUserWithClaims(new Claim[]
        {
            new(SubscriptionClaims.HasActiveSubscription, "true"),
            new(SubscriptionClaims.SubscriptionStatus, "Active"),
            new("has_priority_support", "true")
        });

        var requirement = new PrioritySupportRequirement();
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await _authService.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_AdvancedAnalytics_ShouldSucceed()
    {
        // Arrange
        var user = CreateUserWithClaims(new Claim[]
        {
            new(SubscriptionClaims.HasActiveSubscription, "true"),
            new(SubscriptionClaims.SubscriptionStatus, "Active"),
            new("has_advanced_analytics", "true")
        });

        var requirement = new AdvancedAnalyticsRequirement();
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await _authService.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_MultiSignature_ShouldSucceed()
    {
        // Arrange
        var user = CreateUserWithClaims(new Claim[]
        {
            new(SubscriptionClaims.HasActiveSubscription, "true"),
            new(SubscriptionClaims.SubscriptionStatus, "Active"),
            new("has_multi_signature", "true")
        });

        var requirement = new MultiSignatureRequirement();
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await _authService.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_CustomIntegrations_ShouldSucceed()
    {
        // Arrange
        var user = CreateUserWithClaims(new Claim[]
        {
            new(SubscriptionClaims.HasActiveSubscription, "true"),
            new(SubscriptionClaims.SubscriptionStatus, "Active"),
            new("has_custom_integrations", "true")
        });

        var requirement = new CustomIntegrationsRequirement();
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await _authService.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_AdvancedFraudDetection_ShouldSucceed()
    {
        // Arrange
        var user = CreateUserWithClaims(new Claim[]
        {
            new(SubscriptionClaims.HasActiveSubscription, "true"),
            new(SubscriptionClaims.SubscriptionStatus, "Active"),
            new("has_advanced_fraud_detection", "true")
        });

        var requirement = new AdvancedFraudDetectionRequirement();
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await _authService.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_SubscriptionStatusNone_ShouldFail()
    {
        // Arrange
        var user = CreateUserWithClaims(new Claim[]
        {
            new(SubscriptionClaims.HasActiveSubscription, "true"),
            new(SubscriptionClaims.SubscriptionStatus, "None")
        });

        var requirement = new ActiveSubscriptionRequirement();
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await _authService.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_SubscriptionStatusError_ShouldFail()
    {
        // Arrange
        var user = CreateUserWithClaims(new Claim[]
        {
            new(SubscriptionClaims.HasActiveSubscription, "true"),
            new(SubscriptionClaims.SubscriptionStatus, "Error")
        });

        var requirement = new ActiveSubscriptionRequirement();
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await _authService.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_InvalidFeaturesJson_ShouldFail()
    {
        // Arrange
        var user = CreateUserWithClaims(new Claim[]
        {
            new(SubscriptionClaims.HasActiveSubscription, "true"),
            new(SubscriptionClaims.SubscriptionStatus, "Active"),
            new(SubscriptionClaims.AvailableFeatures, "invalid-json{[")
        });

        var requirement = new ApiAccessRequirement();
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await _authService.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_ComplexRequirement_AllConditionsMet_ShouldSucceed()
    {
        // Arrange
        var features = new[] { "api_access", "advanced_analytics", "priority_support" };
        var featuresJson = JsonSerializer.Serialize(features);

        var user = CreateUserWithClaims(new Claim[]
        {
            new(SubscriptionClaims.HasActiveSubscription, "true"),
            new(SubscriptionClaims.SubscriptionStatus, "Active"),
            new(SubscriptionClaims.SubscriptionTierName, "Enterprise"),
            new(SubscriptionClaims.MaxActiveProjects, "100"),
            new(SubscriptionClaims.MaxTeamMembers, "50"),
            new(SubscriptionClaims.AvailableFeatures, featuresJson)
        });

        var requirement = new SubscriptionRequirement
        {
            RequiredTierNames = new List<string> { "Enterprise" },
            RequiredFeatures = new List<string> { "api_access", "advanced_analytics" },
            MinMaxActiveProjects = 50,
            MinMaxTeamMembers = 10,
            AllowTrial = false
        };
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await _authService.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_ComplexRequirement_OneConditionFails_ShouldFail()
    {
        // Arrange
        var features = new[] { "api_access", "priority_support" }; // Missing advanced_analytics
        var featuresJson = JsonSerializer.Serialize(features);

        var user = CreateUserWithClaims(new Claim[]
        {
            new(SubscriptionClaims.HasActiveSubscription, "true"),
            new(SubscriptionClaims.SubscriptionStatus, "Active"),
            new(SubscriptionClaims.SubscriptionTierName, "Enterprise"),
            new(SubscriptionClaims.MaxActiveProjects, "100"),
            new(SubscriptionClaims.MaxTeamMembers, "50"),
            new(SubscriptionClaims.AvailableFeatures, featuresJson)
        });

        var requirement = new SubscriptionRequirement
        {
            RequiredTierNames = new List<string> { "Enterprise" },
            RequiredFeatures = new List<string> { "api_access", "advanced_analytics" },
            MinMaxActiveProjects = 50,
            MinMaxTeamMembers = 10,
            AllowTrial = false
        };
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await _authService.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_FeatureCaseSensitivity_ShouldBeIgnored()
    {
        // Arrange
        var features = new[] { "API_ACCESS", "Priority_Support" }; // Different casing
        var featuresJson = JsonSerializer.Serialize(features);

        var user = CreateUserWithClaims(new Claim[]
        {
            new(SubscriptionClaims.HasActiveSubscription, "true"),
            new(SubscriptionClaims.SubscriptionStatus, "Active"),
            new(SubscriptionClaims.AvailableFeatures, featuresJson)
        });

        var requirement = new ApiAccessRequirement(); // Requires "api_access"
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await _authService.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue();
    }

    private static ClaimsPrincipal CreateUserWithClaims(Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "Test");
        return new ClaimsPrincipal(identity);
    }
}
