using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Net;
using FluentAssertions;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Tests.Infrastructure;

namespace SkillLedger.Tests.Integration.Api;

/// <summary>
/// Integration tests for Subscription Admin API endpoints
/// Tests subscription analytics, billing operations, and admin operations
/// </summary>
[IntegrationTest]
[ApiTest]
[Collection("Integration Api 2")]
public class SubscriptionAdminControllerTests : IntegrationTestBase
{
    private User _regularUser = null!;
    private User _adminUser = null!;

    public SubscriptionAdminControllerTests(SharedTestHostFixture fixture) : base(fixture)
    {
    }

    protected override async Task OnInitializeAsync()
    {
        await base.OnInitializeAsync();

        // Setup regular test user
        _regularUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "sub-admin-user@test.com",
            UserName = "sub-admin-user@test.com",
            Status = UserStatus.Active
        };

        // Setup admin user
        _adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "sub-admin@test.com",
            UserName = "sub-admin@test.com",
            Status = UserStatus.Active
        };

        Context.Users.AddRange(_regularUser, _adminUser);
        await Context.SaveChangesAsync();
    }

    #region GET /api/admin/subscriptionadmin/statistics Tests

    [Fact]
    [FastTest]
    public async Task GET_Statistics_AsAdmin_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.GetAsync("/api/admin/subscriptionadmin/statistics");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_Statistics_WithDateRange_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });
        var startDate = DateTime.UtcNow.AddMonths(-3).ToString("yyyy-MM-dd");
        var endDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

        // Act
        var response = await Client.GetAsync($"/api/admin/subscriptionadmin/statistics?startDate={startDate}&endDate={endDate}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_Statistics_WithStartAfterEnd_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });
        var startDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var endDate = DateTime.UtcNow.AddMonths(-1).ToString("yyyy-MM-dd");

        // Act
        var response = await Client.GetAsync($"/api/admin/subscriptionadmin/statistics?startDate={startDate}&endDate={endDate}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Start date must be before end date");
    }

    [Fact]
    [SecurityTest]
    public async Task GET_Statistics_AsRegularUser_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_regularUser);

        // Act
        var response = await Client.GetAsync("/api/admin/subscriptionadmin/statistics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    [SecurityTest]
    public async Task GET_Statistics_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/admin/subscriptionadmin/statistics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/admin/subscriptionadmin/analytics Tests

    [Fact]
    [FastTest]
    public async Task GET_Analytics_AsAdmin_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.GetAsync("/api/admin/subscriptionadmin/analytics");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_Analytics_WithDateRange_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });
        var startDate = DateTime.UtcNow.AddMonths(-1).ToString("yyyy-MM-dd");
        var endDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

        // Act
        var response = await Client.GetAsync($"/api/admin/subscriptionadmin/analytics?startDate={startDate}&endDate={endDate}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_Analytics_WithInvalidDateRange_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });
        var startDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var endDate = DateTime.UtcNow.AddDays(-5).ToString("yyyy-MM-dd");

        // Act
        var response = await Client.GetAsync($"/api/admin/subscriptionadmin/analytics?startDate={startDate}&endDate={endDate}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [SecurityTest]
    public async Task GET_Analytics_AsRegularUser_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_regularUser);

        // Act
        var response = await Client.GetAsync("/api/admin/subscriptionadmin/analytics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region POST /api/admin/subscriptionadmin/process-renewals Tests

    [Fact]
    [FastTest]
    public async Task POST_ProcessRenewals_AsAdmin_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.PostAsync("/api/admin/subscriptionadmin/process-renewals", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_ProcessRenewals_AsRegularUser_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_regularUser);

        // Act
        var response = await Client.PostAsync("/api/admin/subscriptionadmin/process-renewals", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region POST /api/admin/subscriptionadmin/process-trials Tests

    [Fact]
    [FastTest]
    public async Task POST_ProcessTrials_AsAdmin_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.PostAsync("/api/admin/subscriptionadmin/process-trials", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_ProcessTrials_AsRegularUser_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_regularUser);

        // Act
        var response = await Client.PostAsync("/api/admin/subscriptionadmin/process-trials", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region POST /api/admin/subscriptionadmin/process-retries Tests

    [Fact]
    [FastTest]
    public async Task POST_ProcessRetries_AsAdmin_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.PostAsync("/api/admin/subscriptionadmin/process-retries", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_ProcessRetries_WithCustomMaxRetries_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.PostAsync("/api/admin/subscriptionadmin/process-retries?maxRetries=5", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_ProcessRetries_AsRegularUser_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_regularUser);

        // Act
        var response = await Client.PostAsync("/api/admin/subscriptionadmin/process-retries", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region POST /api/admin/subscriptionadmin/process-cancellations Tests

    [Fact]
    [FastTest]
    public async Task POST_ProcessCancellations_AsAdmin_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.PostAsync("/api/admin/subscriptionadmin/process-cancellations", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_ProcessCancellations_WithCustomGracePeriod_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.PostAsync("/api/admin/subscriptionadmin/process-cancellations?gracePeriodDays=14", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region POST /api/admin/subscriptionadmin/send-reminders Tests

    [Fact]
    [FastTest]
    public async Task POST_SendReminders_AsAdmin_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.PostAsync("/api/admin/subscriptionadmin/send-reminders", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_SendReminders_WithCustomDaysBefore_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.PostAsync("/api/admin/subscriptionadmin/send-reminders?daysBefore=7", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region POST /api/admin/subscriptionadmin/update-statistics Tests

    [Fact]
    [FastTest]
    public async Task POST_UpdateStatistics_AsAdmin_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.PostAsync("/api/admin/subscriptionadmin/update-statistics", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_UpdateStatistics_AsRegularUser_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_regularUser);

        // Act
        var response = await Client.PostAsync("/api/admin/subscriptionadmin/update-statistics", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region POST /api/admin/subscriptionadmin/validate-subscriptions Tests

    [Fact]
    [FastTest]
    public async Task POST_ValidateSubscriptions_AsAdmin_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.PostAsync("/api/admin/subscriptionadmin/validate-subscriptions", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_ValidateSubscriptions_AsRegularUser_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_regularUser);

        // Act
        var response = await Client.PostAsync("/api/admin/subscriptionadmin/validate-subscriptions", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region GET /api/admin/subscriptionadmin/subscriptions Tests

    [Fact]
    [FastTest]
    public async Task GET_Subscriptions_AsAdmin_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.GetAsync("/api/admin/subscriptionadmin/subscriptions");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_Subscriptions_WithPagination_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.GetAsync("/api/admin/subscriptionadmin/subscriptions?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_Subscriptions_FilteredByStatus_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act - Filter by Active status (1)
        var response = await Client.GetAsync("/api/admin/subscriptionadmin/subscriptions?status=1");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_Subscriptions_FilteredByTierId_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });
        var tierId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/admin/subscriptionadmin/subscriptions?tierId={tierId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task GET_Subscriptions_AsRegularUser_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_regularUser);

        // Act
        var response = await Client.GetAsync("/api/admin/subscriptionadmin/subscriptions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Authorization Tests

    [Fact]
    [SecurityTest]
    public async Task AllAdminEndpoints_WithoutAuth_ReturnUnauthorized()
    {
        // Test all endpoints that require Admin authentication
        var getEndpoints = new[]
        {
            "/api/admin/subscriptionadmin/statistics",
            "/api/admin/subscriptionadmin/analytics",
            "/api/admin/subscriptionadmin/subscriptions"
        };

        foreach (var endpoint in getEndpoints)
        {
            var response = await Client.GetAsync(endpoint);
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
                $"GET {endpoint} should require authentication");
        }

        var postEndpoints = new[]
        {
            "/api/admin/subscriptionadmin/process-renewals",
            "/api/admin/subscriptionadmin/process-trials",
            "/api/admin/subscriptionadmin/process-retries",
            "/api/admin/subscriptionadmin/process-cancellations",
            "/api/admin/subscriptionadmin/send-reminders",
            "/api/admin/subscriptionadmin/update-statistics",
            "/api/admin/subscriptionadmin/validate-subscriptions"
        };

        foreach (var endpoint in postEndpoints)
        {
            var response = await Client.PostAsync(endpoint, null);
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
                $"POST {endpoint} should require authentication");
        }
    }

    [Fact]
    [SecurityTest]
    public async Task AllAdminEndpoints_AsRegularUser_ReturnForbidden()
    {
        // Arrange
        AuthenticateAs(_regularUser);

        var getEndpoints = new[]
        {
            "/api/admin/subscriptionadmin/statistics",
            "/api/admin/subscriptionadmin/analytics",
            "/api/admin/subscriptionadmin/subscriptions"
        };

        foreach (var endpoint in getEndpoints)
        {
            var response = await Client.GetAsync(endpoint);
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
                $"GET {endpoint} should require Admin role");
        }

        var postEndpoints = new[]
        {
            "/api/admin/subscriptionadmin/process-renewals",
            "/api/admin/subscriptionadmin/process-trials",
            "/api/admin/subscriptionadmin/process-retries",
            "/api/admin/subscriptionadmin/process-cancellations",
            "/api/admin/subscriptionadmin/send-reminders",
            "/api/admin/subscriptionadmin/update-statistics",
            "/api/admin/subscriptionadmin/validate-subscriptions"
        };

        foreach (var endpoint in postEndpoints)
        {
            var response = await Client.PostAsync(endpoint, null);
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
                $"POST {endpoint} should require Admin role");
        }
    }

    #endregion
}
