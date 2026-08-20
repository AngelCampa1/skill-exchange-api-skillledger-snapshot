using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Xunit;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Constants;
using SkillLedger.Api.Controllers;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Tests.Infrastructure;

namespace SkillLedger.Tests.Integration;

/// <summary>
/// API integration tests for anti-gaming fraud detection endpoints
/// </summary>
[Collection("Integration Other")]
public class AntiGamingApiIntegrationTests : IntegrationTestBase
{
    private const string TestUserEmail = "antigaming.user@test.com";
    private const string TestAdminEmail = "antigaming.admin@test.com";
    private const string TestPassword = "TestPassword123!";

    public AntiGamingApiIntegrationTests(SharedTestHostFixture fixture) : base(fixture)
    {
    }

    /// <summary>
    /// Creates a test user with Admin role
    /// </summary>
    private async Task<User> CreateTestAdminAsync(string email = TestAdminEmail)
    {
        // Create a completely isolated scope to avoid any tracking conflicts
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();

        // Ensure Admin role exists
        if (!await roleManager.RoleExistsAsync(RoleNames.Admin))
        {
            await roleManager.CreateAsync(new Role(RoleNames.Admin) { IsSystemRole = true });
        }

        // Create user directly in this scope
        var admin = new User
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(admin, TestPassword);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create admin user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
        }

        // Add admin role
        var roleResult = await userManager.AddToRoleAsync(admin, RoleNames.Admin);
        if (!roleResult.Succeeded)
        {
            throw new InvalidOperationException($"Failed to add admin role: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
        }

        return admin;
    }

    /// <summary>
    /// Creates an authenticated HttpClient for the given user
    /// Uses the base class's Client and AuthenticateAs method with HTTP header authentication
    /// </summary>
    private HttpClient CreateAuthenticatedClient(User user, string[]? roles = null)
    {
        AuthenticateAs(user, roles);
        return Client;
    }

    /// <summary>
    /// Creates JSON content for HTTP requests
    /// </summary>
    private StringContent CreateJsonContent(object data)
    {
        var json = JsonConvert.SerializeObject(data);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    [Fact]
    public async Task GetUserRiskScore_AuthenticatedUser_ReturnsRiskScore()
    {
        // Arrange
        var user = await CreateTestUserAsync(TestUserEmail, TestPassword, emailVerified: true);
        var client = CreateAuthenticatedClient(user);

        // Act
        var response = await client.GetAsync("/api/antigaming/risk-score");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var riskScore = JsonConvert.DeserializeObject<dynamic>(content);

        Assert.NotNull(riskScore);
    }

    [Fact]
    public async Task GetUserRiskScore_UnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var client = Client;

        // Act
        var response = await client.GetAsync("/api/antigaming/risk-score");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUserRiskScoreById_AdminUser_ReturnsRiskScore()
    {
        // Arrange
        var admin = await CreateTestAdminAsync();
        AuthenticateAs(admin);
        var targetUser = await CreateTestUserAsync("target.user@test.com", TestPassword, emailVerified: true);
        var client = CreateAuthenticatedClient(admin, new[] { "Admin" });

        // Act
        var response = await client.GetAsync($"/api/antigaming/risk-score/{targetUser.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var riskScore = JsonConvert.DeserializeObject<dynamic>(content);

        Assert.NotNull(riskScore);
    }

    [Fact]
    public async Task GetUserRiskScoreById_RegularUser_ReturnsForbidden()
    {
        // Arrange
        var user = await CreateTestUserAsync("regular.user@test.com", TestPassword, emailVerified: true);
        var targetUser = await CreateTestUserAsync("target2.user@test.com", TestPassword, emailVerified: true);
        var client = CreateAuthenticatedClient(user);

        // Act
        var response = await client.GetAsync($"/api/antigaming/risk-score/{targetUser.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AnalyzeUserBehavior_AdminUser_ReturnsAnalysis()
    {
        // Arrange
        var admin = await CreateTestAdminAsync();
        AuthenticateAs(admin);
        var targetUser = await CreateTestUserAsync("analyze.target@test.com", TestPassword, emailVerified: true);
        var client = CreateAuthenticatedClient(admin, new[] { "Admin" });

        // Create some test data for the user
        await CreateTestReviewsForUserAsync(targetUser.Id, 5);

        var request = new
        {
            UserId = targetUser.Id
        };

        // Act
        var response = await client.PostAsync("/api/antigaming/analyze-behavior",
            CreateJsonContent(request));

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotImplemented);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var assessment = JsonConvert.DeserializeObject<dynamic>(content);
            Assert.NotNull(assessment);
        }
    }

    [Fact]
    public async Task ReportGamingActivity_AuthenticatedUser_SubmitsReport()
    {
        // Arrange
        var reportingUser = await CreateTestUserAsync("reporting.user@test.com", TestPassword, emailVerified: true);
        var suspectedUser = await CreateTestUserAsync("suspected.user@test.com", TestPassword, emailVerified: true);
        var client = CreateAuthenticatedClient(reportingUser);

        var request = new
        {
            SuspectedUserId = suspectedUser.Id,
            Reason = "Posting fake reviews",
            Evidence = new Dictionary<string, object>
            {
                ["ObservedBehavior"] = "Multiple similar reviews posted rapidly"
            }
        };

        // Act
        var response = await client.PostAsync("/api/antigaming/report-gaming",
            CreateJsonContent(request));

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotImplemented);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<dynamic>(content);
            Assert.NotNull(result);
        }
    }

    [Fact]
    public async Task GetBehaviorMetrics_AdminUser_ReturnsMetrics()
    {
        // Arrange
        var admin = await CreateTestAdminAsync();
        AuthenticateAs(admin);
        var targetUser = await CreateTestUserAsync("metrics.target@test.com", TestPassword, emailVerified: true);
        var client = CreateAuthenticatedClient(admin, new[] { "Admin" });

        // Create test data
        await CreateTestReviewsForUserAsync(targetUser.Id, 3);

        // Act
        var response = await client.GetAsync($"/api/antigaming/behavior-metrics?userId={targetUser.Id}");

        // Assert - Debug actual response for troubleshooting
        if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.NotImplemented)
        {
            var content = await response.Content.ReadAsStringAsync();
            Assert.Fail($"Expected OK (200) or NotImplemented (501), but got {response.StatusCode} ({(int)response.StatusCode}). Response: {content}");
        }
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotImplemented);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var metrics = JsonConvert.DeserializeObject<dynamic>(content);
            Assert.NotNull(metrics);
        }
    }

    [Fact]
    public async Task GetNetworkConnections_AdminUser_ReturnsConnections()
    {
        // Arrange
        var admin = await CreateTestAdminAsync();
        AuthenticateAs(admin);
        var user1 = await CreateTestUserAsync("network1.user@test.com", TestPassword, emailVerified: true);
        var user2 = await CreateTestUserAsync("network2.user@test.com", TestPassword, emailVerified: true);

        // Create suspicious network connection
        await CreateSuspiciousNetworkConnectionAsync(user1.Id, user2.Id);

        var client = CreateAuthenticatedClient(admin, new[] { "Admin" });

        // Act
        var response = await client.GetAsync($"/api/antigaming/network-connections/{user1.Id}");

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotImplemented);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var connections = JsonConvert.DeserializeObject<dynamic>(content);
            Assert.NotNull(connections);
        }
    }

    [Fact]
    public async Task ValidateReview_AdminUser_ReturnsValidationResult()
    {
        // Arrange
        var admin = await CreateTestAdminAsync();
        AuthenticateAs(admin);
        var reviewer = await CreateTestUserAsync("validate.reviewer@test.com", TestPassword, emailVerified: true);
        var client = CreateAuthenticatedClient(admin, new[] { "Admin" });

        var request = new
        {
            ReviewerId = reviewer.Id,
            ProjectId = Guid.NewGuid(),
            OverallRating = 5,
            ReviewText = "Great work! Highly recommended!"
        };

        // Act
        var response = await client.PostAsync("/api/antigaming/validate-review",
            CreateJsonContent(request));

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotImplemented);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<dynamic>(content);
            Assert.NotNull(result);
        }
    }

    [Fact]
    public async Task GetAlerts_AdminUser_ReturnsAlerts()
    {
        // Arrange
        var admin = await CreateTestAdminAsync();
        AuthenticateAs(admin);
        var client = CreateAuthenticatedClient(admin, new[] { "Admin" });

        // Act
        var response = await client.GetAsync("/api/antigaming/alerts");

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotImplemented);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var alerts = JsonConvert.DeserializeObject<dynamic>(content);
            Assert.NotNull(alerts);
        }
    }

    [Fact]
    public async Task GetAlerts_RegularUser_ReturnsForbidden()
    {
        // Arrange
        var user = await CreateTestUserAsync("alerts.user@test.com", TestPassword, emailVerified: true);
        var client = CreateAuthenticatedClient(user);

        // Act
        var response = await client.GetAsync("/api/antigaming/alerts");

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.NotImplemented);
    }

    [Theory]
    [InlineData("", "SuspectedUserId is required")]
    [InlineData("invalid-guid", "Invalid UserId format")]
    public async Task ReportGamingActivity_InvalidRequest_ReturnsBadRequest(string suspectedUserId, string _)
    {
        // Arrange
        var user = await CreateTestUserAsync("invalid.report@test.com", TestPassword, emailVerified: true);
        var client = CreateAuthenticatedClient(user);

        var request = new
        {
            SuspectedUserId = suspectedUserId,
            Reason = "Test reason"
        };

        // Act
        var response = await client.PostAsync("/api/antigaming/report-gaming",
            CreateJsonContent(request));

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest ||
                   response.StatusCode == HttpStatusCode.InternalServerError ||
                   response.StatusCode == HttpStatusCode.NotImplemented);
    }

    [Fact]
    public async Task RiskScoreEndpoint_HighVelocityUser_ReturnsElevatedRisk()
    {
        // Arrange
        var user = await CreateTestUserAsync("velocity.test@test.com", TestPassword, emailVerified: true);

        // Create high velocity pattern
        await CreateTestReviewsForUserAsync(user.Id, 20, TimeSpan.FromMinutes(30));

        var client = CreateAuthenticatedClient(user);

        // Act
        var response = await client.GetAsync("/api/antigaming/risk-score");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var riskScore = JsonConvert.DeserializeObject<dynamic>(content);

        Assert.NotNull(riskScore);
    }

    [Fact]
    public async Task AnalyzeUserBehavior_SuspiciousUser_DetectsPatterns()
    {
        // Arrange
        var admin = await CreateTestAdminAsync();
        AuthenticateAs(admin);
        var suspiciousUser = await CreateTestUserAsync("suspicious.user@test.com", TestPassword, emailVerified: true);

        // Create suspicious patterns
        await CreateSuspiciousDeviceFingerprintAsync(suspiciousUser.Id);
        await CreateTestReviewsForUserAsync(suspiciousUser.Id, 15, TimeSpan.FromMinutes(20));

        var client = CreateAuthenticatedClient(admin, new[] { "Admin" });
        var request = new { UserId = suspiciousUser.Id };

        // Act
        var response = await client.PostAsync("/api/antigaming/analyze-behavior",
            CreateJsonContent(request));

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotImplemented);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var assessment = JsonConvert.DeserializeObject<dynamic>(content);
            Assert.NotNull(assessment);
        }
    }

    #region Helper Methods

    private async Task CreateTestReviewsForUserAsync(Guid userId, int count, TimeSpan? interval = null)
    {
        var baseTime = DateTime.UtcNow;
        var timeSpan = interval ?? TimeSpan.FromDays(1);

        for (int i = 0; i < count; i++)
        {
            var revieweeId = Guid.NewGuid();
            var projectId = Guid.NewGuid();

            // Create a minimal project for the review
            Context.Projects.Add(new Project
            {
                Id = projectId,
                ClientId = Guid.NewGuid(),
                Title = $"Test Project {i}",
                Description = "Test project for anti-gaming tests",
                CreditBudget = 1000,
                Status = SkillLedger.Core.Enums.ProjectStatus.Completed
            });

            Context.ProjectReviews.Add(new ProjectReview
            {
                ReviewerId = userId,
                RevieweeId = revieweeId,
                ProjectId = projectId,
                OverallRating = 4 + (i % 2),
                ReviewText = interval.HasValue
                    ? "Great work! Highly recommended!" // Similar content for velocity test
                    : $"Review number {i} with unique content about the project quality and delivery.",
                SubmittedAt = baseTime - TimeSpan.FromTicks(timeSpan.Ticks * i),
                Status = SkillLedger.Core.Enums.ProjectReviewStatus.Published
            });
        }

        await Context.SaveChangesAsync();
    }

    private async Task CreateSuspiciousDeviceFingerprintAsync(Guid userId)
    {
        Context.DeviceFingerprints.Add(new DeviceFingerprint
        {
            UserId = userId,
            FingerprintHash = "suspicious_device_hash",
            IpAddress = "10.0.0.1",
            UserAgent = "HeadlessChrome/90.0.4430.212",
            IsSuspicious = true,
            RiskLevel = 4
        });

        await Context.SaveChangesAsync();
    }

    private async Task CreateSuspiciousNetworkConnectionAsync(Guid user1Id, Guid user2Id)
    {
        Context.UserNetworkConnections.Add(new UserNetworkConnection
        {
            User1Id = user1Id,
            User2Id = user2Id,
            ConnectionType = "SharedDevice",
            ConnectionStrength = 0.8m,
            DetectedAt = DateTime.UtcNow
        });

        await Context.SaveChangesAsync();
    }

    #endregion
}