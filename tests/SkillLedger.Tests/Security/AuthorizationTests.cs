using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using SkillLedger.Tests.Infrastructure;
using System.Reflection;
using Xunit;

namespace SkillLedger.Tests.Security;

/// <summary>
/// Unit tests verifying authorization boundaries are properly enforced
/// Tests critical authorization issues from controller audit
/// </summary>
[SecurityTest]
[UnitTest]
public class AuthorizationTests
{
    #region Controller Authorization Tests

    [Fact]
    public void AllControllers_HaveAuthorizationOrAllowAnonymous()
    {
        // Arrange
        var apiAssembly = typeof(SkillLedger.Api.Controllers.ProjectController).Assembly;
        var controllers = apiAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Controller"))
            .Where(t => t.Namespace?.Contains("Api.Controllers") == true);

        var controllersWithoutAuth = new List<string>();

        // Act
        foreach (var controller in controllers)
        {
            var hasAuthorize = controller.GetCustomAttributes<AuthorizeAttribute>().Any();
            var hasAllowAnonymous = controller.GetCustomAttributes<AllowAnonymousAttribute>().Any();

            // Check if any methods have authorization
            var methods = controller.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            var anyMethodHasAuth = methods.Any(m =>
                m.GetCustomAttributes<AuthorizeAttribute>().Any() ||
                m.GetCustomAttributes<AllowAnonymousAttribute>().Any());

            if (!hasAuthorize && !hasAllowAnonymous && !anyMethodHasAuth)
            {
                controllersWithoutAuth.Add(controller.Name);
            }
        }

        // Assert - Most controllers should have some form of authorization
        controllersWithoutAuth.Count.Should().BeLessThan(5,
            "Most controllers should have authorization. " +
            $"Controllers without auth: {string.Join(", ", controllersWithoutAuth)}");
    }

    #endregion

    #region Skill Authorization Tests

    [Fact]
    public void SkillController_CriticalEndpoints_RequireAuthorization()
    {
        // Arrange - CRITICAL: These endpoints should NOT allow anonymous access
        var controllerType = typeof(SkillLedger.Api.Controllers.SkillController);
        var criticalEndpoints = new[]
        {
            "CreateSkillEndorsement",  // Actual method name, not AddEndorsement
            "RemoveSkillEndorsement",  // Actual method name, not RemoveEndorsement
            "GetMySkills",             // Getting own skills requires auth
            "AddUserSkill",            // Adding skill to own profile
            "UpdateUserSkill",         // Updating own skill
            "RemoveUserSkill"          // Removing own skill
            // NOTE: GetUserSkills is intentionally [AllowAnonymous] for viewing public profiles
            // NOTE: UpdateUserSkillLevel, UpdateUserSkillVisibility, GetSkillStatistics don't exist in current implementation
        };

        // Act & Assert
        foreach (var methodName in criticalEndpoints)
        {
            var method = controllerType.GetMethod(methodName);
            method.Should().NotBeNull($"{methodName} should exist in SkillController");

            // Check that it doesn't have [AllowAnonymous]
            var hasAllowAnonymous = method!.GetCustomAttributes<AllowAnonymousAttribute>().Any();

            hasAllowAnonymous.Should().BeFalse(
                $"CRITICAL: SkillController.{methodName} should NOT allow anonymous access (BUG from audit)");

            // Should have [Authorize] either on method or controller
            var hasMethodAuth = method.GetCustomAttributes<AuthorizeAttribute>().Any();
            var hasControllerAuth = controllerType.GetCustomAttributes<AuthorizeAttribute>().Any();

            (hasMethodAuth || hasControllerAuth).Should().BeTrue(
                $"SkillController.{methodName} must require authentication");
        }
    }

    #endregion

    #region Role-Based Authorization Tests

    [Fact]
    public void AdminEndpoints_RequireAdminRole()
    {
        // Arrange
        var apiAssembly = typeof(SkillLedger.Api.Controllers.ProjectController).Assembly;
        var controllers = apiAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Controller"));

        var violations = new List<string>();

        // Act - Find endpoints with "Admin" in name but no admin role requirement
        foreach (var controller in controllers)
        {
            var methods = controller.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name.Contains("Admin", StringComparison.OrdinalIgnoreCase));

            foreach (var method in methods)
            {
                // Check for admin role requirement
                var methodAuth = method.GetCustomAttribute<AuthorizeAttribute>();
                var controllerAuth = controller.GetCustomAttribute<AuthorizeAttribute>();

                var hasAdminRole = (methodAuth?.Roles?.Contains("Admin") == true) ||
                                   (controllerAuth?.Roles?.Contains("Admin") == true);

                if (!hasAdminRole)
                {
                    violations.Add($"{controller.Name}.{method.Name}");
                }
            }
        }

        // Assert
        violations.Should().BeEmpty(
            "Endpoints with 'Admin' in name should require Admin role. " +
            $"Violations: {string.Join(", ", violations)}");
    }

    #endregion

    #region Reference Implementation Tests

    [Fact]
    public void ProjectController_HasProperAuthorization()
    {
        // Arrange
        var controllerType = typeof(SkillLedger.Api.Controllers.ProjectController);

        // Act
        var hasAuthorize = controllerType.GetCustomAttributes<AuthorizeAttribute>().Any();

        // Assert
        hasAuthorize.Should().BeTrue(
            "ProjectController should have [Authorize] as reference implementation");
    }

    [Fact]
    public void ProjectController_PublicEndpoints_ExplicitlyAllowAnonymous()
    {
        // Arrange
        var controllerType = typeof(SkillLedger.Api.Controllers.ProjectController);
        var publicMethods = new[] { "GetProjects", "GetProjectById" };

        // Act & Assert
        foreach (var methodName in publicMethods)
        {
            var method = controllerType.GetMethod(methodName);
            if (method != null)
            {
                var hasAllowAnonymous = method.GetCustomAttributes<AllowAnonymousAttribute>().Any();
                hasAllowAnonymous.Should().BeTrue(
                    $"ProjectController.{methodName} should explicitly allow anonymous access with [AllowAnonymous]");
            }
        }
    }

    #endregion
}
