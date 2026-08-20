using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Net;
using FluentAssertions;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Tests.Infrastructure;

namespace SkillLedger.Tests.Integration.Api;

/// <summary>
/// Integration tests for Monitoring API endpoints
/// Tests health checks, metrics, logs, tracing, and alerts
/// </summary>
[IntegrationTest]
[ApiTest]
[Collection("Integration Api 2")]
public class MonitoringControllerTests : IntegrationTestBase
{
    private User _regularUser = null!;
    private User _adminUser = null!;
    private User _monitoringUser = null!;

    public MonitoringControllerTests(SharedTestHostFixture fixture) : base(fixture)
    {
    }

    protected override async Task OnInitializeAsync()
    {
        await base.OnInitializeAsync();

        // Setup regular test user
        _regularUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "monitoring-user@test.com",
            UserName = "monitoring-user@test.com",
            Status = UserStatus.Active
        };

        // Setup admin user
        _adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "monitoring-admin@test.com",
            UserName = "monitoring-admin@test.com",
            Status = UserStatus.Active
        };

        // Setup monitoring role user
        _monitoringUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "monitoring-ops@test.com",
            UserName = "monitoring-ops@test.com",
            Status = UserStatus.Active
        };

        Context.Users.AddRange(_regularUser, _adminUser, _monitoringUser);
        await Context.SaveChangesAsync();
    }

    #region GET /api/monitoring/health Tests

    [Fact]
    [FastTest]
    public async Task GET_Health_WithoutAuth_ReturnsOk()
    {
        // Act
        var response = await Client.GetAsync("/api/monitoring/health");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    [FastTest]
    public async Task GET_Health_WithDetailedFalse_ReturnsBasicInfo()
    {
        // Act
        var response = await Client.GetAsync("/api/monitoring/health?detailed=false");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("status");
        content.Should().Contain("timestamp");
        content.Should().Contain("version");
    }

    [Fact]
    [FastTest]
    public async Task GET_Health_WithDetailedTrue_ReturnsDetailedInfo()
    {
        // Act
        var response = await Client.GetAsync("/api/monitoring/health?detailed=true");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("status");
        content.Should().Contain("systemInfo");
        content.Should().Contain("performanceMetrics");
    }

    #endregion

    #region GET /api/monitoring/metrics Tests

    [Fact]
    [FastTest]
    public async Task GET_Metrics_AsAdmin_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.GetAsync("/api/monitoring/metrics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("system");
        content.Should().Contain("performance");
        content.Should().Contain("memory");
    }

    [Fact]
    [FastTest]
    public async Task GET_Metrics_AsMonitoringRole_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_monitoringUser, new[] { "Monitoring" });

        // Act
        var response = await Client.GetAsync("/api/monitoring/metrics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    [SecurityTest]
    public async Task GET_Metrics_AsRegularUser_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_regularUser);

        // Act
        var response = await Client.GetAsync("/api/monitoring/metrics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    [SecurityTest]
    public async Task GET_Metrics_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/monitoring/metrics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task GET_Metrics_WithSystemCategory_ReturnsSystemMetrics()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.GetAsync("/api/monitoring/metrics?category=system");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("system");
    }

    [Fact]
    [FastTest]
    public async Task GET_Metrics_WithPerformanceCategory_ReturnsPerformanceMetrics()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.GetAsync("/api/monitoring/metrics?category=performance");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("performance");
    }

    [Fact]
    [FastTest]
    public async Task GET_Metrics_WithMemoryCategory_ReturnsMemoryMetrics()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.GetAsync("/api/monitoring/metrics?category=memory");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("memory");
    }

    [Fact]
    [FastTest]
    public async Task GET_Metrics_WithInvalidCategory_ReturnsAllMetrics()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.GetAsync("/api/monitoring/metrics?category=invalid");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("system");
        content.Should().Contain("performance");
    }

    #endregion

    #region GET /api/monitoring/logs Tests

    [Fact]
    [FastTest]
    public async Task GET_Logs_AsAdmin_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.GetAsync("/api/monitoring/logs");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("logs");
        content.Should().Contain("page");
        content.Should().Contain("totalCount");
    }

    [Fact]
    [FastTest]
    public async Task GET_Logs_WithPagination_ReturnsPagedResults()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.GetAsync("/api/monitoring/logs?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("\"page\":1");
        content.Should().Contain("\"pageSize\":10");
    }

    [Fact]
    [FastTest]
    public async Task GET_Logs_WithLevelFilter_ReturnsFilteredLogs()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.GetAsync("/api/monitoring/logs?level=Error");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Error");
    }

    [Fact]
    [SecurityTest]
    public async Task GET_Logs_AsRegularUser_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_regularUser);

        // Act
        var response = await Client.GetAsync("/api/monitoring/logs");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    [SecurityTest]
    public async Task GET_Logs_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/monitoring/logs");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/monitoring/tracing Tests

    [Fact]
    [FastTest]
    public async Task GET_Tracing_AsAdmin_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.GetAsync("/api/monitoring/tracing");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("traces");
        content.Should().Contain("queryParameters");
    }

    [Fact]
    [FastTest]
    public async Task GET_Tracing_WithTraceId_ReturnsTraceData()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });
        var traceId = Guid.NewGuid().ToString();

        // Act
        var response = await Client.GetAsync($"/api/monitoring/tracing?traceId={traceId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("traces");
    }

    [Fact]
    [FastTest]
    public async Task GET_Tracing_WithHoursParameter_ReturnsFilteredTraces()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.GetAsync("/api/monitoring/tracing?hours=12");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("\"hours\":12");
    }

    [Fact]
    [SecurityTest]
    public async Task GET_Tracing_AsRegularUser_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_regularUser);

        // Act
        var response = await Client.GetAsync("/api/monitoring/tracing");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    [SecurityTest]
    public async Task GET_Tracing_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/monitoring/tracing");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/monitoring/alerts Tests

    [Fact]
    [FastTest]
    public async Task GET_Alerts_AsAdmin_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.GetAsync("/api/monitoring/alerts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("alerts");
        content.Should().Contain("configuration");
    }

    [Fact]
    [FastTest]
    public async Task GET_Alerts_WithActiveOnlyTrue_ReturnsOnlyActiveAlerts()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.GetAsync("/api/monitoring/alerts?activeOnly=true");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("alerts");
    }

    [Fact]
    [FastTest]
    public async Task GET_Alerts_WithActiveOnlyFalse_ReturnsAllAlerts()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.GetAsync("/api/monitoring/alerts?activeOnly=false");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("alerts");
    }

    [Fact]
    [SecurityTest]
    public async Task GET_Alerts_AsRegularUser_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_regularUser);

        // Act
        var response = await Client.GetAsync("/api/monitoring/alerts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    [SecurityTest]
    public async Task GET_Alerts_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/monitoring/alerts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization Tests

    [Fact]
    [SecurityTest]
    public async Task AllProtectedEndpoints_WithoutAuth_ReturnUnauthorized()
    {
        // Test all endpoints that require authentication
        var endpoints = new[]
        {
            "/api/monitoring/metrics",
            "/api/monitoring/logs",
            "/api/monitoring/tracing",
            "/api/monitoring/alerts"
        };

        foreach (var endpoint in endpoints)
        {
            var response = await Client.GetAsync(endpoint);
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
                $"GET {endpoint} should require authentication");
        }
    }

    [Fact]
    [SecurityTest]
    public async Task AllProtectedEndpoints_AsRegularUser_ReturnForbidden()
    {
        // Arrange
        AuthenticateAs(_regularUser);

        var endpoints = new[]
        {
            "/api/monitoring/metrics",
            "/api/monitoring/logs",
            "/api/monitoring/tracing",
            "/api/monitoring/alerts"
        };

        foreach (var endpoint in endpoints)
        {
            var response = await Client.GetAsync(endpoint);
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
                $"GET {endpoint} should require Admin or Monitoring role");
        }
    }

    #endregion
}
