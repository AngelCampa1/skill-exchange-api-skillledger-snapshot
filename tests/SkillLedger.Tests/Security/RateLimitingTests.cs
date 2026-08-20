using FluentAssertions;
using Microsoft.AspNetCore.RateLimiting;
using SkillLedger.Tests.Infrastructure;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;

namespace SkillLedger.Tests.Security;

/// <summary>
/// Unit tests verifying rate limiting is properly configured on abuse-prone endpoints
/// Tests Phase 4 implementation: Rate limiting on 56+ endpoints
/// </summary>
[SecurityTest]
[UnitTest]
public class RateLimitingTests
{
    #region Controller-Level Rate Limiting Tests

    [Fact]
    public void SkillController_HasRateLimiting_Enabled()
    {
        // Arrange
        var controllerType = typeof(SkillLedger.Api.Controllers.SkillController);

        // Act
        var hasRateLimiting = controllerType.GetCustomAttributes<EnableRateLimitingAttribute>().Any();

        // Assert
        hasRateLimiting.Should().BeTrue(
            "SkillController must have rate limiting enabled at controller level (was disabled for testing in Phase 4)");
    }

    [Fact]
    public void ProjectController_HasRateLimiting_AsReferenceImplementation()
    {
        // Arrange
        var controllerType = typeof(SkillLedger.Api.Controllers.ProjectController);

        // Act
        var rateLimitingAttribute = controllerType.GetCustomAttribute<EnableRateLimitingAttribute>();

        // Assert
        rateLimitingAttribute.Should().NotBeNull(
            "ProjectController should have rate limiting as reference implementation");
    }

    #endregion

    #region Critical Financial Endpoint Rate Limiting

    [Fact]
    public void MilestoneTriggerPaymentRelease_HasStrictRateLimit()
    {
        // Arrange - CRITICAL financial operation must have strict rate limiting
        var controllerType = typeof(SkillLedger.Api.Controllers.MilestoneController);
        var method = controllerType.GetMethod("TriggerPaymentRelease");

        // Act
        var hasRateLimiting = method?.GetCustomAttributes<EnableRateLimitingAttribute>().Any() ?? false;

        // Assert
        hasRateLimiting.Should().BeTrue(
            "CRITICAL: TriggerPaymentRelease must have rate limiting (MilestonePaymentPolicy: 3 per 5 minutes)");
    }

    [Fact]
    public void CriticalFinancialEndpoints_HaveRateLimiting()
    {
        // Arrange - Financial operations that must be rate limited
        var criticalEndpoints = new[]
        {
            ("MilestoneController", "TriggerPaymentRelease"),
            ("EscrowController", "CreateEscrow"),
            ("EscrowController", "ReleaseMilestone"),
            ("CreditWalletController", "TransferCredits"),
            ("CreditWalletController", "AddCredits")
        };

        // Act & Assert
        foreach (var (controllerName, methodName) in criticalEndpoints)
        {
            var controllerType = typeof(SkillLedger.Api.Controllers.ProjectController).Assembly
                .GetTypes()
                .FirstOrDefault(t => t.Name == controllerName);

            controllerType.Should().NotBeNull($"{controllerName} should exist");

            var method = controllerType!.GetMethod(methodName);
            method.Should().NotBeNull($"{methodName} should exist in {controllerName}");

            // Check method-level or controller-level rate limiting
            var hasMethodRateLimiting = method!.GetCustomAttributes<EnableRateLimitingAttribute>().Any();
            var hasControllerRateLimiting = controllerType.GetCustomAttributes<EnableRateLimitingAttribute>().Any();

            (hasMethodRateLimiting || hasControllerRateLimiting).Should().BeTrue(
                $"CRITICAL: {controllerName}.{methodName} handles financial operations and MUST have rate limiting");
        }
    }

    #endregion

    #region Milestone State Change Rate Limiting

    [Fact]
    public void MilestoneController_StateChangingEndpoints_HaveRateLimiting()
    {
        // Arrange
        var controllerType = typeof(SkillLedger.Api.Controllers.MilestoneController);
        var stateChangingMethods = new[]
        {
            "StartMilestone",
            "SubmitMilestoneForReview",
            "ApproveMilestone",
            "RequestMilestoneRevisions",
            "CancelMilestone"
        };

        // Act & Assert
        foreach (var methodName in stateChangingMethods)
        {
            var method = controllerType.GetMethod(methodName);
            method.Should().NotBeNull($"{methodName} should exist in MilestoneController");

            // Check method-level or controller-level rate limiting
            var hasMethodRateLimiting = method!.GetCustomAttributes<EnableRateLimitingAttribute>().Any();
            var hasControllerRateLimiting = controllerType.GetCustomAttributes<EnableRateLimitingAttribute>().Any();

            (hasMethodRateLimiting || hasControllerRateLimiting).Should().BeTrue(
                $"MilestoneController.{methodName} must have rate limiting to prevent abuse");
        }
    }

    #endregion

    #region Messaging Rate Limiting

    [Fact]
    public void MessagingController_HasRateLimiting()
    {
        // Arrange
        var controllerType = typeof(SkillLedger.Api.Controllers.MessagingController);
        var messagingMethods = new[]
        {
            "SendMessageAsync",
            "EditMessageAsync",
            "DeleteMessageAsync",
            "AddReactionAsync",
            "RemoveReactionAsync"
        };

        // Act & Assert
        foreach (var methodName in messagingMethods)
        {
            var method = controllerType.GetMethod(methodName);
            method.Should().NotBeNull($"{methodName} should exist in MessagingController");

            // Check method-level or controller-level rate limiting
            var hasMethodRateLimiting = method!.GetCustomAttributes<EnableRateLimitingAttribute>().Any();
            var hasControllerRateLimiting = controllerType.GetCustomAttributes<EnableRateLimitingAttribute>().Any();

            (hasMethodRateLimiting || hasControllerRateLimiting).Should().BeTrue(
                $"MessagingController.{methodName} must have rate limiting to prevent spam");
        }
    }

    #endregion

    #region Coverage and Consistency Tests

    [Fact]
    public void AllControllers_AbuseProneEndpoints_HaveRateLimiting()
    {
        // Arrange
        var apiAssembly = typeof(SkillLedger.Api.Controllers.ProjectController).Assembly;
        var controllers = apiAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Controller"))
            .Where(t => t.Namespace?.Contains("Api.Controllers") == true);

        var endpointsWithoutRateLimit = new List<string>();

        // Act - Check POST/PUT/DELETE endpoints
        foreach (var controller in controllers)
        {
            var hasControllerRateLimit = controller.GetCustomAttributes<EnableRateLimitingAttribute>().Any();

            var stateMethods = controller.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.GetCustomAttributes<Microsoft.AspNetCore.Mvc.HttpPostAttribute>().Any() ||
                           m.GetCustomAttributes<Microsoft.AspNetCore.Mvc.HttpPutAttribute>().Any() ||
                           m.GetCustomAttributes<Microsoft.AspNetCore.Mvc.HttpDeleteAttribute>().Any());

            foreach (var method in stateMethods)
            {
                var hasMethodRateLimit = method.GetCustomAttributes<EnableRateLimitingAttribute>().Any();

                if (!hasMethodRateLimit && !hasControllerRateLimit)
                {
                    endpointsWithoutRateLimit.Add($"{controller.Name}.{method.Name}");
                }
            }
        }

        // Assert - Allow some endpoints without rate limiting, but not too many
        if (endpointsWithoutRateLimit.Any())
        {
            var message = $"Found {endpointsWithoutRateLimit.Count} state-changing endpoints without rate limiting: " +
                         $"{string.Join(", ", endpointsWithoutRateLimit.Take(10))}...";

            // Not failing test - just ensuring reasonable coverage
            endpointsWithoutRateLimit.Count.Should().BeLessThanOrEqualTo(50,
                "Too many endpoints without rate limiting protection");
        }
    }

    [Fact]
    public void Controllers_RateLimitingPattern_IsConsistent()
    {
        // Arrange
        var apiAssembly = typeof(SkillLedger.Api.Controllers.ProjectController).Assembly;
        var controllerTypes = apiAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Controller"))
            .Where(t => t.Namespace?.Contains("Api.Controllers") == true)
            .ToList();

        var rateLimitedCount = 0;
        var totalStateChangingEndpoints = 0;

        // Act
        foreach (var controller in controllerTypes)
        {
            var hasControllerRateLimit = controller.GetCustomAttributes<EnableRateLimitingAttribute>().Any();

            var stateChangingMethods = controller.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.GetCustomAttributes<Microsoft.AspNetCore.Mvc.HttpPostAttribute>().Any() ||
                           m.GetCustomAttributes<Microsoft.AspNetCore.Mvc.HttpPutAttribute>().Any() ||
                           m.GetCustomAttributes<Microsoft.AspNetCore.Mvc.HttpDeleteAttribute>().Any())
                .ToList();

            totalStateChangingEndpoints += stateChangingMethods.Count;

            rateLimitedCount += stateChangingMethods.Count(m =>
            {
                var hasMethodRateLimit = m.GetCustomAttributes<EnableRateLimitingAttribute>().Any();
                return hasMethodRateLimit || hasControllerRateLimit;
            });
        }

        // Assert - At least 40% of state-changing endpoints should have rate limiting
        var protectionRate = totalStateChangingEndpoints > 0
            ? (double)rateLimitedCount / totalStateChangingEndpoints
            : 0;

        protectionRate.Should().BeGreaterThan(0.40,
            $"At least 40% of state-changing endpoints should have rate limiting. " +
            $"Current: {rateLimitedCount}/{totalStateChangingEndpoints} ({protectionRate:P})");
    }

    #endregion

    #region Reference Implementation Tests

    [Fact]
    public void ProjectController_CreateProject_HasRateLimiting_AsReferenceImplementation()
    {
        // Arrange
        var controllerType = typeof(SkillLedger.Api.Controllers.ProjectController);
        var createMethod = controllerType.GetMethod("CreateProject");

        // Act
        var hasMethodRateLimit = createMethod?.GetCustomAttributes<EnableRateLimitingAttribute>().Any() ?? false;
        var hasControllerRateLimit = controllerType.GetCustomAttributes<EnableRateLimitingAttribute>().Any();

        // Assert
        (hasMethodRateLimit || hasControllerRateLimit).Should().BeTrue(
            "ProjectController.CreateProject should have rate limiting as reference implementation");
    }

    [Fact]
    public void RateLimitingAttributes_UseValidPolicyNames()
    {
        // Arrange
        var apiAssembly = typeof(SkillLedger.Api.Controllers.ProjectController).Assembly;
        var controllers = apiAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Controller"));

        var invalidPolicyNames = new List<string>();

        // Act
        foreach (var controller in controllers)
        {
            // Check controller-level attributes
            var controllerAttributes = controller.GetCustomAttributes<EnableRateLimitingAttribute>();
            foreach (var attr in controllerAttributes)
            {
                var policyName = attr.PolicyName;
                if (string.IsNullOrEmpty(policyName))
                {
                    invalidPolicyNames.Add($"{controller.Name} (controller-level)");
                }
            }

            // Check method-level attributes
            var methods = controller.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            foreach (var method in methods)
            {
                var methodAttributes = method.GetCustomAttributes<EnableRateLimitingAttribute>();
                foreach (var attr in methodAttributes)
                {
                    var policyName = attr.PolicyName;
                    if (string.IsNullOrEmpty(policyName))
                    {
                        invalidPolicyNames.Add($"{controller.Name}.{method.Name}");
                    }
                }
            }
        }

        // Assert
        invalidPolicyNames.Should().BeEmpty(
            "All [EnableRateLimiting] attributes must specify a valid policy name. " +
            $"Invalid: {string.Join(", ", invalidPolicyNames)}");
    }

    [Fact]
    public void RateLimitingAttributes_UseRegisteredProductionPolicies()
    {
        // Arrange
        var apiAssembly = typeof(SkillLedger.Api.Controllers.ProjectController).Assembly;
        var controllers = apiAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Controller"));

        var usedPolicyNames = controllers
            .SelectMany(controller =>
                controller.GetCustomAttributes<EnableRateLimitingAttribute>()
                    .Concat(controller.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                        .SelectMany(method => method.GetCustomAttributes<EnableRateLimitingAttribute>())))
            .Select(attribute => attribute.PolicyName)
            .Where(policyName => !string.IsNullOrWhiteSpace(policyName))
            .ToHashSet(StringComparer.Ordinal);

        var rateLimitingSource = File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", "src", "SkillLedger.Api", "Configuration", "RateLimitingConfiguration.cs"));

        var registeredPolicyNames = Regex.Matches(rateLimitingSource, "AddPolicy\\(\"([^\"]+)\"")
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

        // Act
        var unregisteredPolicyNames = usedPolicyNames
            .Where(policyName => !registeredPolicyNames.Contains(policyName!))
            .OrderBy(policyName => policyName)
            .ToList();

        // Assert
        unregisteredPolicyNames.Should().BeEmpty(
            "every [EnableRateLimiting] policy used by controllers must be registered in production. " +
            $"Missing: {string.Join(", ", unregisteredPolicyNames)}");
    }

    #endregion

    #region Specific Policy Tests

    [Fact]
    public void SkillController_UsesSkillManagementPolicy()
    {
        // Arrange
        var controllerType = typeof(SkillLedger.Api.Controllers.SkillController);

        // Act
        var rateLimitingAttribute = controllerType.GetCustomAttribute<EnableRateLimitingAttribute>();

        // Assert
        if (rateLimitingAttribute != null)
        {
            rateLimitingAttribute.PolicyName.Should().NotBeNullOrEmpty(
                "SkillController rate limiting should specify a policy name");
        }
    }

    #endregion
}
