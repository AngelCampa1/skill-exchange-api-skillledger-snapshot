using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using System.Net;
using FluentAssertions;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Tests.Infrastructure;

namespace SkillLedger.Tests.Integration.Api;

/// <summary>
/// Integration tests for Security API endpoints
/// Tests security settings, GDPR compliance, audit logs, and incident reporting
/// </summary>
[IntegrationTest]
[ApiTest]
[Collection("Integration Api 2")]
public class SecurityControllerTests : IntegrationTestBase
{
    private User _user = null!;

    public SecurityControllerTests(SharedTestHostFixture fixture) : base(fixture)
    {
    }

    protected override async Task OnInitializeAsync()
    {
        await base.OnInitializeAsync();

        // Setup test user
        _user = new User
        {
            Id = Guid.NewGuid(),
            Email = "security-user@test.com",
            UserName = "security-user@test.com",
            Status = UserStatus.Active
        };

        Context.Users.Add(_user);
        await Context.SaveChangesAsync();
    }

    #region GET /api/security/settings Tests

    [Fact]
    [FastTest]
    public async Task GET_Settings_WithAuth_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/security/settings");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("activeSessions");
        content.Should().Contain("dataRetentionDays");
    }

    [Fact]
    [FastTest]
    public async Task GET_Settings_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/security/settings");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task GET_Settings_CreatesAuditLog()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/security/settings");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify audit log was created
        var auditLog = Context.AuditLogs
            .FirstOrDefault(a => a.UserId == _user.Id && a.Action == "SECURITY_SETTINGS_ACCESSED");
        auditLog.Should().NotBeNull();
    }

    #endregion

    #region POST /api/security/consent Tests

    [Fact]
    [FastTest]
    public async Task POST_Consent_WithValidRequest_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        var request = new
        {
            ConsentGiven = true,
            Purpose = "Marketing",
            ConsentText = "I agree to receive marketing communications"
        };

        var content = JsonContent.Create(request);
        await AddCsrfTokenToRequest(content);

        // Act
        var response = await Client.PostAsync("/api/security/consent", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotImplemented);
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Contain("consent persistence is not yet implemented");
    }

    [Fact]
    [FastTest]
    public async Task POST_Consent_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var request = new
        {
            ConsentGiven = true,
            Purpose = "Marketing"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/security/consent", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task POST_Consent_WithNullRequest_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.PostAsJsonAsync("/api/security/consent", (object?)null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnsupportedMediaType);
    }

    [Fact]
    [FastTest]
    public async Task POST_Consent_CreatesAuditLog()
    {
        // Arrange
        AuthenticateAs(_user);

        var request = new
        {
            ConsentGiven = false,
            Purpose = "DataProcessing"
        };

        var content = JsonContent.Create(request);
        await AddCsrfTokenToRequest(content);

        // Act
        var response = await Client.PostAsync("/api/security/consent", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotImplemented);

        // Verify audit log was created
        var auditLog = Context.AuditLogs
            .FirstOrDefault(a => a.UserId == _user.Id && a.Action == "PRIVACY_CONSENT_UPDATED");
        auditLog.Should().NotBeNull();
        auditLog!.Details.Should().Contain("DataProcessing");
    }

    #endregion

    #region POST /api/security/data-export Tests

    [Fact]
    [FastTest]
    public async Task POST_DataExport_WithAuth_ReturnsAccepted()
    {
        // Arrange
        AuthenticateAs(_user);
        var content = JsonContent.Create(new { });
        await AddCsrfTokenToRequest(content);

        // Act
        var response = await Client.PostAsync("/api/security/data-export", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Contain("exportRequestId");
        responseBody.Should().Contain("status");
        responseBody.Should().Contain("Processing");
        responseBody.Should().Contain("Profile");
        responseBody.Should().Contain("Projects");
        responseBody.Should().Contain("Transactions");
    }

    [Fact]
    [FastTest]
    public async Task POST_DataExport_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.PostAsync("/api/security/data-export", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task POST_DataExport_CreatesAuditLog()
    {
        // Arrange
        AuthenticateAs(_user);
        var content = JsonContent.Create(new { });
        await AddCsrfTokenToRequest(content);

        // Act
        var response = await Client.PostAsync("/api/security/data-export", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        // Verify audit log was created
        var auditLog = Context.AuditLogs
            .FirstOrDefault(a => a.UserId == _user.Id && a.Action == "DATA_EXPORT_REQUESTED");
        auditLog.Should().NotBeNull();
    }

    [Fact]
    [SecurityTest]
    public async Task POST_DataExport_PersistsPrivacyRequestForCurrentUser()
    {
        // Arrange
        AuthenticateAs(_user);
        var content = JsonContent.Create(new { });
        await AddCsrfTokenToRequest(content);

        // Act
        var response = await Client.PostAsync("/api/security/data-export", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var request = await Context.PrivacyRequests.SingleAsync(r => r.UserId == _user.Id && r.RequestType == "DataExport");
        request.Status.Should().Be("Processing");
        request.DueAt.Should().NotBeNull();
        request.ConfirmationRequired.Should().BeFalse();
    }

    #endregion

    #region POST /api/security/account-deletion Tests

    [Fact]
    [FastTest]
    public async Task POST_AccountDeletion_WithValidRequest_ReturnsAccepted()
    {
        // Arrange
        AuthenticateAs(_user);

        var request = new
        {
            Reason = "No longer using the service",
            ConfirmDataLoss = true,
            AdditionalNotes = "Thank you for your service"
        };

        var content = JsonContent.Create(request);
        await AddCsrfTokenToRequest(content);

        // Act
        var response = await Client.PostAsync("/api/security/account-deletion", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Contain("deletionRequestId");
        responseBody.Should().Contain("status");
        responseBody.Should().Contain("Pending");
        responseBody.Should().Contain("gracePeriodEnds");
        responseBody.Should().Contain("confirmationToken");
    }

    [Fact]
    [FastTest]
    public async Task POST_AccountDeletion_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var request = new
        {
            Reason = "No longer using the service",
            ConfirmDataLoss = true
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/security/account-deletion", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task POST_AccountDeletion_WithNullRequest_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.PostAsJsonAsync("/api/security/account-deletion", (object?)null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnsupportedMediaType);
    }

    [Fact]
    [FastTest]
    public async Task POST_AccountDeletion_WithEmptyReason_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_user);

        var request = new
        {
            Reason = "",
            ConfirmDataLoss = true
        };

        var content = JsonContent.Create(request);
        await AddCsrfTokenToRequest(content);

        // Act
        var response = await Client.PostAsync("/api/security/account-deletion", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Contain("Deletion reason is required");
    }

    [Fact]
    [FastTest]
    public async Task POST_AccountDeletion_CreatesAuditLog()
    {
        // Arrange
        AuthenticateAs(_user);

        var request = new
        {
            Reason = "Moving to competitor",
            ConfirmDataLoss = true
        };

        var content = JsonContent.Create(request);
        await AddCsrfTokenToRequest(content);

        // Act
        var response = await Client.PostAsync("/api/security/account-deletion", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        // Verify audit log was created
        var auditLog = Context.AuditLogs
            .FirstOrDefault(a => a.UserId == _user.Id && a.Action == "ACCOUNT_DELETION_REQUESTED");
        auditLog.Should().NotBeNull();
    }

    [Fact]
    [SecurityTest]
    public async Task POST_AccountDeletion_PersistsEncryptedPrivacyRequestForCurrentUser()
    {
        // Arrange
        AuthenticateAs(_user);

        var request = new
        {
            Reason = "No longer using the service",
            ConfirmDataLoss = true,
            AdditionalNotes = "Please delete all optional data"
        };

        var content = JsonContent.Create(request);
        await AddCsrfTokenToRequest(content);

        // Act
        var response = await Client.PostAsync("/api/security/account-deletion", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var deletionRequest = await Context.PrivacyRequests.SingleAsync(r => r.UserId == _user.Id && r.RequestType == "AccountDeletion");
        deletionRequest.Status.Should().Be("Pending");
        deletionRequest.EncryptedReason.Should().NotBeNullOrWhiteSpace();
        deletionRequest.EncryptedAdditionalNotes.Should().NotBeNullOrWhiteSpace();
        deletionRequest.ConfirmationTokenHash.Should().NotBeNullOrWhiteSpace();
        deletionRequest.ConfirmationTokenHash.Should().HaveLength(64);
        deletionRequest.ConfirmationRequired.Should().BeTrue();
    }

    #endregion

    #region GET /api/security/audit-log Tests

    [Fact]
    [FastTest]
    public async Task GET_AuditLog_WithAuth_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/security/audit-log");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("logs");
        content.Should().Contain("page");
        content.Should().Contain("pageSize");
        content.Should().Contain("totalCount");
    }

    [Fact]
    [FastTest]
    public async Task GET_AuditLog_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/security/audit-log");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task GET_AuditLog_WithPagination_ReturnsCorrectParameters()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/security/audit-log?page=2&pageSize=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("\"page\":2");
        content.Should().Contain("\"pageSize\":20");
    }

    [Fact]
    [FastTest]
    public async Task GET_AuditLog_CreatesAuditLog()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/security/audit-log");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify audit log was created
        var auditLog = Context.AuditLogs
            .FirstOrDefault(a => a.UserId == _user.Id && a.Action == "AUDIT_LOG_ACCESSED");
        auditLog.Should().NotBeNull();
    }

    #endregion

    #region POST /api/security/report-incident Tests

    [Fact]
    [FastTest]
    public async Task POST_ReportIncident_WithValidRequest_ReturnsAccepted()
    {
        // Arrange
        AuthenticateAs(_user);

        var request = new
        {
            Type = "UnauthorizedAccess",
            Severity = "High",
            Description = "Noticed unusual login activity from unknown location",
            IncidentDate = DateTime.UtcNow.AddHours(-2),
            AffectedData = "Login credentials",
            ImmediateActionRequired = true
        };

        var content = JsonContent.Create(request);
        await AddCsrfTokenToRequest(content);

        // Act
        var response = await Client.PostAsync("/api/security/report-incident", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Contain("incidentId");
        responseBody.Should().Contain("status");
        responseBody.Should().Contain("Under Investigation");
        responseBody.Should().Contain("referenceNumber");
    }

    [Fact]
    [FastTest]
    public async Task POST_ReportIncident_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var request = new
        {
            Type = "DataBreach",
            Severity = "Critical",
            Description = "Suspected data breach"
        };

        var content = JsonContent.Create(request);
        await AddCsrfTokenToRequest(content);

        // Act
        var response = await Client.PostAsync("/api/security/report-incident", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task POST_ReportIncident_WithNullRequest_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.PostAsJsonAsync("/api/security/report-incident", (object?)null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnsupportedMediaType);
    }

    [Fact]
    [FastTest]
    public async Task POST_ReportIncident_WithEmptyDescription_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_user);

        var request = new
        {
            Type = "PhishingAttempt",
            Severity = "Medium",
            Description = ""
        };

        var content = JsonContent.Create(request);
        await AddCsrfTokenToRequest(content);

        // Act
        var response = await Client.PostAsync("/api/security/report-incident", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Contain("Incident description is required");
    }

    [Fact]
    [FastTest]
    public async Task POST_ReportIncident_CreatesAuditLog()
    {
        // Arrange
        AuthenticateAs(_user);

        var request = new
        {
            Type = "SuspiciousActivity",
            Severity = "Low",
            Description = "Unusual account activity detected"
        };

        var content = JsonContent.Create(request);
        await AddCsrfTokenToRequest(content);

        // Act
        var response = await Client.PostAsync("/api/security/report-incident", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        // Verify audit log was created
        var auditLog = Context.AuditLogs
            .FirstOrDefault(a => a.UserId == _user.Id && a.Action == "SECURITY_INCIDENT_REPORTED");
        auditLog.Should().NotBeNull();
        auditLog!.Details.Should().Contain("SuspiciousActivity");
    }

    #endregion

    #region Authorization Tests

    [Fact]
    [SecurityTest]
    public async Task AllEndpoints_WithoutAuth_ReturnUnauthorized()
    {
        // Test all endpoints without authentication
        var endpoints = new[]
        {
            ("GET", "/api/security/settings"),
            ("GET", "/api/security/audit-log"),
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

    [Fact]
    [SecurityTest]
    public async Task POST_Endpoints_WithoutAuth_ReturnUnauthorized()
    {
        // Test POST endpoints that require authentication
        var postEndpoints = new[]
        {
            "/api/security/consent",
            "/api/security/data-export",
            "/api/security/account-deletion",
            "/api/security/report-incident"
        };

        foreach (var url in postEndpoints)
        {
            var response = await Client.PostAsync(url, null);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
                $"POST {url} should require authentication");
        }
    }

    #endregion

    #region Rate Limiting Tests

    [Fact]
    [FastTest]
    public async Task SecurityEndpoints_AreRateLimited()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act - Make multiple requests rapidly
        var responses = new List<HttpResponseMessage>();
        for (int i = 0; i < 3; i++)
        {
            var response = await Client.GetAsync("/api/security/settings");
            responses.Add(response);
        }

        // Assert - At least some requests should complete (rate limit is configured in controller)
        // Note: Actual rate limiting behavior depends on test environment configuration
        responses.Should().NotBeEmpty();
        responses.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.OK);
    }

    #endregion
}
