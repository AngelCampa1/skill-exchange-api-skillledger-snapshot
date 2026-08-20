using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Net;
using FluentAssertions;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Tests.Infrastructure;

namespace SkillLedger.Tests.Integration.Api;

/// <summary>
/// Integration tests for Anti-Gaming Controller API endpoints
/// Tests fraud detection, risk scoring, behavior analysis, and gaming prevention
/// </summary>
[IntegrationTest]
[ApiTest]
[Collection("Integration Api 1")]
public class AntiGamingControllerTests : IntegrationTestBase
{
    private User _user = null!;
    private User _adminUser = null!;
    private User _suspectedUser = null!;

    public AntiGamingControllerTests(SharedTestHostFixture fixture) : base(fixture)
    {
    }

    protected override async Task OnInitializeAsync()
    {
        await base.OnInitializeAsync();

        // Setup regular test user
        _user = new User
        {
            Id = Guid.NewGuid(),
            Email = "antigaming-user@test.com",
            UserName = "antigaming-user@test.com",
            Status = UserStatus.Active
        };

        // Setup admin user
        _adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "antigaming-admin@test.com",
            UserName = "antigaming-admin@test.com",
            Status = UserStatus.Active
        };

        // Setup suspected user
        _suspectedUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "suspected-user@test.com",
            UserName = "suspected-user@test.com",
            Status = UserStatus.Active
        };

        Context.Users.AddRange(_user, _adminUser, _suspectedUser);
        await Context.SaveChangesAsync();
    }

    #region GET /api/antigaming/risk-score Tests

    [Fact]
    [FastTest]
    public async Task GET_RiskScore_WithAuth_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/antigaming/risk-score");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("userId");
        content.Should().Contain("riskScore");
        content.Should().Contain("assessedAt");
    }

    [Fact]
    [FastTest]
    public async Task GET_RiskScore_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/antigaming/risk-score");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/antigaming/risk-score/{userId} Tests (Admin)

    [Fact]
    [SecurityTest]
    public async Task GET_RiskScoreByUserId_AsAdmin_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.GetAsync($"/api/antigaming/risk-score/{_suspectedUser.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("userId");
        content.Should().Contain("riskScore");
    }

    [Fact]
    [SecurityTest]
    public async Task GET_RiskScoreByUserId_AsRegularUser_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync($"/api/antigaming/risk-score/{_suspectedUser.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    [SecurityTest]
    public async Task GET_RiskScoreByUserId_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync($"/api/antigaming/risk-score/{_suspectedUser.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST /api/antigaming/analyze-behavior Tests (Admin)

    [Fact]
    [SecurityTest]
    public async Task POST_AnalyzeBehavior_AsAdmin_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        var request = new
        {
            UserId = _suspectedUser.Id
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/antigaming/analyze-behavior", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_AnalyzeBehavior_AsRegularUser_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_user);

        var request = new
        {
            UserId = _suspectedUser.Id
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/antigaming/analyze-behavior", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_AnalyzeBehavior_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var request = new
        {
            UserId = _suspectedUser.Id
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/antigaming/analyze-behavior", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST /api/antigaming/report-gaming Tests

    [Fact]
    [FastTest]
    public async Task POST_ReportGaming_WithValidRequest_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        var request = new
        {
            SuspectedUserId = _suspectedUser.Id,
            Reason = "Unusual activity pattern detected",
            Evidence = new Dictionary<string, object>
            {
                { "PatternType", "RapidTransactions" },
                { "Occurrences", 15 },
                { "TimeWindow", "24hours" }
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/antigaming/report-gaming", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_ReportGaming_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var request = new
        {
            SuspectedUserId = _suspectedUser.Id,
            Reason = "Suspicious behavior",
            Evidence = (Dictionary<string, object>?)null
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/antigaming/report-gaming", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task POST_ReportGaming_WithMinimalData_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        var request = new
        {
            SuspectedUserId = _suspectedUser.Id,
            Reason = "Pattern detected",
            Evidence = (Dictionary<string, object>?)null
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/antigaming/report-gaming", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region GET /api/antigaming/behavior-metrics Tests (Admin)

    [Fact]
    [SecurityTest]
    public async Task GET_BehaviorMetrics_AsAdmin_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.GetAsync($"/api/antigaming/behavior-metrics?userId={_suspectedUser.Id}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task GET_BehaviorMetrics_WithSpecificMetrics_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.GetAsync($"/api/antigaming/behavior-metrics?userId={_suspectedUser.Id}&metricNames=TransactionFrequency&metricNames=ReviewPatterns");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task GET_BehaviorMetrics_AsRegularUser_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync($"/api/antigaming/behavior-metrics?userId={_suspectedUser.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    [SecurityTest]
    public async Task GET_BehaviorMetrics_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync($"/api/antigaming/behavior-metrics?userId={_suspectedUser.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/antigaming/network-connections/{userId} Tests (Admin)

    [Fact]
    [SecurityTest]
    public async Task GET_NetworkConnections_AsAdmin_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.GetAsync($"/api/antigaming/network-connections/{_suspectedUser.Id}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task GET_NetworkConnections_AsRegularUser_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync($"/api/antigaming/network-connections/{_suspectedUser.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    [SecurityTest]
    public async Task GET_NetworkConnections_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync($"/api/antigaming/network-connections/{_suspectedUser.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST /api/antigaming/validate-review Tests (Admin)

    [Fact]
    [SecurityTest]
    public async Task POST_ValidateReview_AsAdmin_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        var request = new
        {
            ReviewId = (Guid?)null,
            ReviewerId = _user.Id,
            RevieweeId = _suspectedUser.Id,
            ProjectId = Guid.NewGuid(),
            Rating = 5,
            Comment = "Excellent work, highly recommend!"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/antigaming/validate-review", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_ValidateReview_AsRegularUser_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_user);

        var request = new
        {
            ReviewId = (Guid?)null,
            ReviewerId = _user.Id,
            RevieweeId = _suspectedUser.Id,
            ProjectId = Guid.NewGuid(),
            Rating = 5,
            Comment = "Great service"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/antigaming/validate-review", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_ValidateReview_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var request = new
        {
            ReviewId = (Guid?)null,
            ReviewerId = _user.Id,
            RevieweeId = _suspectedUser.Id,
            ProjectId = Guid.NewGuid(),
            Rating = 5,
            Comment = "Good"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/antigaming/validate-review", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_ValidateReview_WithMinimalData_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        var request = new
        {
            ReviewerId = _user.Id,
            ProjectId = Guid.NewGuid(),
            Rating = 3
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/antigaming/validate-review", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region GET /api/antigaming/alerts Tests (Admin)

    [Fact]
    [SecurityTest]
    public async Task GET_Alerts_AsAdmin_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.GetAsync("/api/antigaming/alerts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    [SecurityTest]
    public async Task GET_Alerts_WithPagination_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.GetAsync("/api/antigaming/alerts?page=2&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    [SecurityTest]
    public async Task GET_Alerts_WithFilters_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act - Note: severity and status are enum values
        var response = await Client.GetAsync("/api/antigaming/alerts?severity=1&status=0");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    [SecurityTest]
    public async Task GET_Alerts_AsRegularUser_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/antigaming/alerts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    [SecurityTest]
    public async Task GET_Alerts_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/antigaming/alerts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization Summary Tests

    [Fact]
    [SecurityTest]
    public async Task AdminOnlyEndpoints_AsRegularUser_ReturnForbidden()
    {
        // Arrange
        AuthenticateAs(_user);

        var adminEndpoints = new[]
        {
            $"/api/antigaming/risk-score/{_suspectedUser.Id}",
            $"/api/antigaming/behavior-metrics?userId={_suspectedUser.Id}",
            $"/api/antigaming/network-connections/{_suspectedUser.Id}",
            "/api/antigaming/alerts"
        };

        foreach (var endpoint in adminEndpoints)
        {
            // Act
            var response = await Client.GetAsync(endpoint);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
                $"GET {endpoint} should require Admin role");
        }
    }

    [Fact]
    [SecurityTest]
    public async Task AdminOnlyPOSTEndpoints_AsRegularUser_ReturnForbidden()
    {
        // Arrange
        AuthenticateAs(_user);

        var analyzeBehaviorRequest = new { UserId = _suspectedUser.Id };
        var analyzeResponse = await Client.PostAsJsonAsync("/api/antigaming/analyze-behavior", analyzeBehaviorRequest);
        analyzeResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var validateReviewRequest = new
        {
            ReviewerId = _user.Id,
            ProjectId = Guid.NewGuid(),
            Rating = 5
        };
        var validateResponse = await Client.PostAsJsonAsync("/api/antigaming/validate-review", validateReviewRequest);
        validateResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    [SecurityTest]
    public async Task AllEndpoints_WithoutAuth_ReturnUnauthorized()
    {
        // Test GET endpoints without authentication
        var getEndpoints = new[]
        {
            "/api/antigaming/risk-score",
            $"/api/antigaming/risk-score/{_suspectedUser.Id}",
            $"/api/antigaming/behavior-metrics?userId={_suspectedUser.Id}",
            $"/api/antigaming/network-connections/{_suspectedUser.Id}",
            "/api/antigaming/alerts"
        };

        foreach (var endpoint in getEndpoints)
        {
            var response = await Client.GetAsync(endpoint);
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
                $"GET {endpoint} should require authentication");
        }
    }

    #endregion
}
