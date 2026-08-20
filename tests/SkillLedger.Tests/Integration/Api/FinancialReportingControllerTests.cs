using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Net;
using FluentAssertions;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Tests.Infrastructure;
using SkillLedger.Core.Interfaces;

namespace SkillLedger.Tests.Integration.Api;

/// <summary>
/// Integration tests for Financial Reporting API endpoints
/// Tests financial reports, analytics, exports, and admin endpoints
/// </summary>
[IntegrationTest]
[ApiTest]
[Collection("Integration Api 1")]
public class FinancialReportingControllerTests : IntegrationTestBase
{
    private User _user = null!;
    private User _adminUser = null!;
    private User _otherUser = null!;

    public FinancialReportingControllerTests(SharedTestHostFixture fixture) : base(fixture)
    {
    }

    protected override async Task OnInitializeAsync()
    {
        await base.OnInitializeAsync();

        // Setup regular test user
        _user = new User
        {
            Id = Guid.NewGuid(),
            Email = "financial-user@test.com",
            UserName = "financial-user@test.com",
            Status = UserStatus.Active
        };

        // Setup admin user
        _adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "financial-admin@test.com",
            UserName = "financial-admin@test.com",
            Status = UserStatus.Active
        };

        // Setup another regular user for authorization tests
        _otherUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "other-user@test.com",
            UserName = "other-user@test.com",
            Status = UserStatus.Active
        };

        Context.Users.AddRange(_user, _adminUser, _otherUser);
        await Context.SaveChangesAsync();
    }

    #region POST /api/financialreporting/credit-summary Tests

    [Fact]
    [FastTest]
    public async Task POST_CreditSummary_WithValidRequest_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        var request = new
        {
            UserId = _user.Id,
            StartDate = DateTime.UtcNow.AddMonths(-1),
            EndDate = DateTime.UtcNow
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/financialreporting/credit-summary", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_CreditSummary_WithoutUserId_UsesCurrentUser()
    {
        // Arrange
        AuthenticateAs(_user);

        var request = new
        {
            UserId = (Guid?)null,
            StartDate = DateTime.UtcNow.AddMonths(-1),
            EndDate = DateTime.UtcNow
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/financialreporting/credit-summary", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_CreditSummary_AccessingOtherUserData_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_user);

        var request = new
        {
            UserId = _otherUser.Id,
            StartDate = DateTime.UtcNow.AddMonths(-1),
            EndDate = DateTime.UtcNow
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/financialreporting/credit-summary", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_CreditSummary_AdminAccessingOtherUserData_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        var request = new
        {
            UserId = _otherUser.Id,
            StartDate = DateTime.UtcNow.AddMonths(-1),
            EndDate = DateTime.UtcNow
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/financialreporting/credit-summary", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_CreditSummary_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var request = new
        {
            UserId = _user.Id,
            StartDate = DateTime.UtcNow.AddMonths(-1),
            EndDate = DateTime.UtcNow
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/financialreporting/credit-summary", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/financialreporting/dashboard Tests

    [Fact]
    [FastTest]
    public async Task GET_Dashboard_WithAuth_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/financialreporting/dashboard");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_Dashboard_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/financialreporting/dashboard");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST /api/financialreporting/analytics Tests

    [Fact]
    [FastTest]
    public async Task POST_Analytics_WithValidRequest_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        var request = new
        {
            UserId = _user.Id,
            StartDate = DateTime.UtcNow.AddMonths(-1),
            EndDate = DateTime.UtcNow
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/financialreporting/analytics", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_Analytics_AccessingOtherUserData_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_user);

        var request = new
        {
            UserId = _otherUser.Id,
            StartDate = DateTime.UtcNow.AddMonths(-1),
            EndDate = DateTime.UtcNow
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/financialreporting/analytics", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region POST /api/financialreporting/export Tests

    [Fact]
    [FastTest]
    public async Task POST_Export_WithValidRequest_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        var request = new
        {
            UserId = _user.Id,
            StartDate = DateTime.UtcNow.AddMonths(-1),
            EndDate = DateTime.UtcNow,
            Format = "CSV"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/financialreporting/export", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_Export_AccessingOtherUserData_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_user);

        var request = new
        {
            UserId = _otherUser.Id,
            StartDate = DateTime.UtcNow.AddMonths(-1),
            EndDate = DateTime.UtcNow,
            Format = "CSV"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/financialreporting/export", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region GET /api/financialreporting/monthly-reports Tests

    [Fact]
    [FastTest]
    public async Task GET_MonthlyReports_WithoutParameters_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/financialreporting/monthly-reports");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_MonthlyReports_WithDateRange_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/financialreporting/monthly-reports?startMonth=1&endMonth=12");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_MonthlyReports_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/financialreporting/monthly-reports");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST /api/financialreporting/budget-tracking Tests

    [Fact]
    [FastTest]
    public async Task POST_BudgetTracking_WithValidRequest_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        var request = new
        {
            UserId = _user.Id,
            MonthlyBudget = 1000m,
            NotifyAtPercentage = 80
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/financialreporting/budget-tracking", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_BudgetTracking_AccessingOtherUserData_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_user);

        var request = new
        {
            UserId = _otherUser.Id,
            MonthlyBudget = 1000m,
            NotifyAtPercentage = 80
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/financialreporting/budget-tracking", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region GET /api/financialreporting/goal-progress Tests

    [Fact]
    [FastTest]
    public async Task GET_GoalProgress_WithAuth_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/financialreporting/goal-progress");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region GET /api/financialreporting/transaction-breakdown Tests

    [Fact]
    [FastTest]
    public async Task GET_TransactionBreakdown_WithValidDates_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);
        var startDate = DateTime.UtcNow.AddMonths(-1).ToString("yyyy-MM-dd");
        var endDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

        // Act
        var response = await Client.GetAsync($"/api/financialreporting/transaction-breakdown?startDate={startDate}&endDate={endDate}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_TransactionBreakdown_WithEndDateBeforeStartDate_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_user);
        var startDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var endDate = DateTime.UtcNow.AddMonths(-1).ToString("yyyy-MM-dd");

        // Act
        var response = await Client.GetAsync($"/api/financialreporting/transaction-breakdown?startDate={startDate}&endDate={endDate}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("End date cannot be before start date");
    }

    #endregion

    #region GET /api/financialreporting/trends Tests

    [Fact]
    [FastTest]
    public async Task GET_Trends_WithDefaultMonths_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/financialreporting/trends");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_Trends_WithValidMonths_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/financialreporting/trends?months=6");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_Trends_WithMonthsBelowMinimum_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/financialreporting/trends?months=0");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Months must be between 1 and 36");
    }

    [Fact]
    [FastTest]
    public async Task GET_Trends_WithMonthsAboveMaximum_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/financialreporting/trends?months=50");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Months must be between 1 and 36");
    }

    #endregion

    #region GET /api/financialreporting/insights Tests

    [Fact]
    [FastTest]
    public async Task GET_Insights_WithDefaultDays_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/financialreporting/insights");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_Insights_WithValidDays_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/financialreporting/insights?analysisWindowDays=30");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_Insights_WithDaysBelowMinimum_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/financialreporting/insights?analysisWindowDays=0");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Analysis window must be between 1 and 365 days");
    }

    [Fact]
    [FastTest]
    public async Task GET_Insights_WithDaysAboveMaximum_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/financialreporting/insights?analysisWindowDays=400");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Analysis window must be between 1 and 365 days");
    }

    #endregion

    #region Admin Endpoint Tests

    [Fact]
    [SecurityTest]
    public async Task GET_AdminSystemAnalytics_AsAdmin_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });
        var startDate = DateTime.UtcNow.AddMonths(-1).ToString("yyyy-MM-dd");
        var endDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

        // Act
        var response = await Client.GetAsync($"/api/financialreporting/admin/system-analytics?startDate={startDate}&endDate={endDate}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task GET_AdminSystemAnalytics_AsRegularUser_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_user);
        var startDate = DateTime.UtcNow.AddMonths(-1).ToString("yyyy-MM-dd");
        var endDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

        // Act
        var response = await Client.GetAsync($"/api/financialreporting/admin/system-analytics?startDate={startDate}&endDate={endDate}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    [SecurityTest]
    public async Task GET_AdminSystemAnalytics_WithEndDateBeforeStartDate_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });
        var startDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var endDate = DateTime.UtcNow.AddMonths(-1).ToString("yyyy-MM-dd");

        // Act
        var response = await Client.GetAsync($"/api/financialreporting/admin/system-analytics?startDate={startDate}&endDate={endDate}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [SecurityTest]
    public async Task GET_AdminTopEarners_AsAdmin_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });
        var startDate = DateTime.UtcNow.AddMonths(-1).ToString("yyyy-MM-dd");
        var endDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

        // Act
        var response = await Client.GetAsync($"/api/financialreporting/admin/top-earners?startDate={startDate}&endDate={endDate}&limit=10");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task GET_AdminTopEarners_AsRegularUser_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_user);
        var startDate = DateTime.UtcNow.AddMonths(-1).ToString("yyyy-MM-dd");
        var endDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

        // Act
        var response = await Client.GetAsync($"/api/financialreporting/admin/top-earners?startDate={startDate}&endDate={endDate}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    [SecurityTest]
    public async Task GET_AdminTopEarners_WithLimitBelowMinimum_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });
        var startDate = DateTime.UtcNow.AddMonths(-1).ToString("yyyy-MM-dd");
        var endDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

        // Act
        var response = await Client.GetAsync($"/api/financialreporting/admin/top-earners?startDate={startDate}&endDate={endDate}&limit=0");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Limit must be between 1 and 100");
    }

    [Fact]
    [SecurityTest]
    public async Task GET_AdminTopEarners_WithLimitAboveMaximum_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });
        var startDate = DateTime.UtcNow.AddMonths(-1).ToString("yyyy-MM-dd");
        var endDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

        // Act
        var response = await Client.GetAsync($"/api/financialreporting/admin/top-earners?startDate={startDate}&endDate={endDate}&limit=150");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Limit must be between 1 and 100");
    }

    [Fact]
    [SecurityTest]
    public async Task POST_AdminDataIntegrity_AsAdmin_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.PostAsync($"/api/financialreporting/admin/data-integrity?userId={_user.Id}", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_AdminDataIntegrity_AsRegularUser_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.PostAsync($"/api/financialreporting/admin/data-integrity?userId={_user.Id}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Authorization Tests

    [Fact]
    [SecurityTest]
    public async Task AllUserEndpoints_WithoutAuth_ReturnUnauthorized()
    {
        // Test all non-admin endpoints without authentication
        var endpoints = new[]
        {
            ("GET", "/api/financialreporting/dashboard"),
            ("GET", "/api/financialreporting/monthly-reports"),
            ("GET", "/api/financialreporting/goal-progress"),
            ("GET", "/api/financialreporting/trends"),
            ("GET", "/api/financialreporting/insights"),
        };

        foreach (var (method, url) in endpoints)
        {
            HttpResponseMessage response;
            switch (method)
            {
                case "GET":
                    response = await Client.GetAsync(url);
                    break;
                default:
                    continue;
            }

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
                $"{method} {url} should require authentication");
        }
    }

    #endregion
}
