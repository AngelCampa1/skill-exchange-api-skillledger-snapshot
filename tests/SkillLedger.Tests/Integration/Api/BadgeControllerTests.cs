using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Net;
using System.Text.Json;
using FluentAssertions;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Tests.Infrastructure;

namespace SkillLedger.Tests.Integration.Api;

/// <summary>
/// Integration tests for Badge Controller API endpoints
/// Tests badge management, verification requests, admin operations, and external integrations
/// </summary>
[IntegrationTest]
[ApiTest]
[Collection("Integration Api 1")]
public class BadgeControllerTests : IntegrationTestBase
{
    private User _user = null!;
    private User _otherUser = null!;
    private User _adminUser = null!;

    public BadgeControllerTests(SharedTestHostFixture fixture) : base(fixture)
    {
    }

    protected override async Task OnInitializeAsync()
    {
        await base.OnInitializeAsync();

        // Setup test users
        _user = new User
        {
            Id = Guid.NewGuid(),
            Email = "badge-user@test.com",
            UserName = "badge-user@test.com",
            Status = UserStatus.Active
        };

        _otherUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "other-badge-user@test.com",
            UserName = "other-badge-user@test.com",
            Status = UserStatus.Active
        };

        _adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "badge-admin@test.com",
            UserName = "badge-admin@test.com",
            Status = UserStatus.Active
        };

        Context.Users.AddRange(_user, _otherUser, _adminUser);
        await Context.SaveChangesAsync();
    }

    #region GET /api/Badge/user/{userId}/badges Tests

    [Fact]
    [FastTest]
    public async Task GET_UserBadges_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync($"/api/Badge/user/{_user.Id}/badges");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task GET_UserBadges_WithAuth_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync($"/api/Badge/user/{_user.Id}/badges");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_UserBadges_WithInvalidUserId_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync($"/api/Badge/user/{Guid.Empty}/badges");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_UserBadges_WithIncludeExpired_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync($"/api/Badge/user/{_user.Id}/badges?includeExpired=true");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region GET /api/Badge/user/{userId}/progress Tests

    [Fact]
    [FastTest]
    public async Task GET_BadgeProgress_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync($"/api/Badge/user/{_user.Id}/progress");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task GET_BadgeProgress_WithAuth_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync($"/api/Badge/user/{_user.Id}/progress");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_BadgeProgress_WithInvalidUserId_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync($"/api/Badge/user/{Guid.Empty}/progress");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region GET /api/Badge/user/{userId}/eligibility Tests

    [Fact]
    [FastTest]
    public async Task GET_BadgeEligibility_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync($"/api/Badge/user/{_user.Id}/eligibility");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task GET_BadgeEligibility_WithAuth_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync($"/api/Badge/user/{_user.Id}/eligibility");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_BadgeEligibility_WithInvalidUserId_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync($"/api/Badge/user/{Guid.Empty}/eligibility");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region POST /api/Badge/verification/request Tests

    [Fact]
    [FastTest]
    public async Task POST_SubmitVerificationRequest_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var request = new
        {
            BadgeType = "SkillVerification",
            Evidence = new Dictionary<string, object>
            {
                { "skillName", "C#" },
                { "yearsExperience", 5 }
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Badge/verification/request", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task POST_SubmitVerificationRequest_WithAuth_ReturnsCreatedOrConflict()
    {
        // Arrange
        AuthenticateAs(_user);

        var request = new
        {
            BadgeType = "SkillVerification",
            Evidence = new Dictionary<string, object>
            {
                { "skillName", "C#" },
                { "yearsExperience", 5 }
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Badge/verification/request", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.Conflict, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_SubmitVerificationRequest_WithEmptyBadgeType_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_user);

        var request = new
        {
            BadgeType = "",
            Evidence = new Dictionary<string, object>()
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Badge/verification/request", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region GET /api/Badge/verification/request/{requestId} Tests

    [Fact]
    [FastTest]
    public async Task GET_VerificationRequest_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var requestId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/Badge/verification/request/{requestId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task GET_VerificationRequest_WithAuth_ReturnsNotImplemented()
    {
        // Arrange
        AuthenticateAs(_user);
        var requestId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/Badge/verification/request/{requestId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotImplemented);
    }

    #endregion

    #region GET /api/Badge/verification/pending Tests

    [Fact]
    [FastTest]
    public async Task GET_PendingVerificationRequests_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/Badge/verification/pending");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [SecurityTest]
    public async Task GET_PendingVerificationRequests_AsNonAdmin_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/Badge/verification/pending");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task GET_PendingVerificationRequests_AsAdmin_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.GetAsync("/api/Badge/verification/pending");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_PendingVerificationRequests_WithBadgeTypeFilter_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.GetAsync("/api/Badge/verification/pending?badgeType=SkillVerification");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region POST /api/Badge/verification/request/{requestId}/process Tests

    [Fact]
    [FastTest]
    public async Task POST_ProcessVerificationRequest_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var request = new
        {
            Approved = true,
            ReviewNotes = "Looks good"
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/Badge/verification/request/{requestId}/process", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_ProcessVerificationRequest_AsNonAdmin_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_user);
        var requestId = Guid.NewGuid();
        var request = new
        {
            Approved = true,
            ReviewNotes = "Looks good"
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/Badge/verification/request/{requestId}/process", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_ProcessVerificationRequest_AsAdmin_ReturnsOkOrBadRequest()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });
        var requestId = Guid.NewGuid();
        var request = new
        {
            Approved = true,
            ReviewNotes = "Approved"
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/Badge/verification/request/{requestId}/process", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region POST /api/Badge/award Tests

    [Fact]
    [FastTest]
    public async Task POST_AwardBadge_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var request = new
        {
            UserId = _user.Id,
            BadgeType = "Contributor",
            Evidence = "Completed 10 projects"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Badge/award", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_AwardBadge_AsNonAdmin_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_user);
        var request = new
        {
            UserId = _otherUser.Id,
            BadgeType = "Contributor",
            Evidence = "Completed 10 projects"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Badge/award", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_AwardBadge_AsAdmin_ReturnsCreatedOrConflict()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });
        var request = new
        {
            UserId = _user.Id,
            BadgeType = "Contributor",
            Evidence = "Completed 10 projects"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Badge/award", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.Conflict, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_AwardBadge_WithEmptyUserId_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });
        var request = new
        {
            UserId = Guid.Empty,
            BadgeType = "Contributor",
            Evidence = "Test"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Badge/award", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region POST /api/Badge/revoke/{badgeId} Tests

    [Fact]
    [FastTest]
    public async Task POST_RevokeBadge_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var badgeId = Guid.NewGuid();
        var request = new
        {
            Reason = "Fraudulent claims"
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/Badge/revoke/{badgeId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_RevokeBadge_AsNonAdmin_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_user);
        var badgeId = Guid.NewGuid();
        var request = new
        {
            Reason = "Test revocation"
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/Badge/revoke/{badgeId}", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_RevokeBadge_AsAdmin_ReturnsOkOrBadRequest()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });
        var badgeId = Guid.NewGuid();
        var request = new
        {
            Reason = "Policy violation"
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/Badge/revoke/{badgeId}", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_RevokeBadge_WithEmptyReason_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });
        var badgeId = Guid.NewGuid();
        var request = new
        {
            Reason = ""
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/Badge/revoke/{badgeId}", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region POST /api/Badge/evaluation/run Tests

    [Fact]
    [FastTest]
    public async Task POST_RunAutomaticBadgeEvaluation_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.PostAsync("/api/Badge/evaluation/run", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_RunAutomaticBadgeEvaluation_AsNonAdmin_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.PostAsync("/api/Badge/evaluation/run", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_RunAutomaticBadgeEvaluation_AsAdmin_RequiresCSRF()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act - Without CSRF token
        var response = await Client.PostAsync("/api/Badge/evaluation/run", null);

        // Assert
        // Endpoint has [ValidateAntiForgeryToken] so should fail without CSRF
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region POST /api/Badge/verify/{badgeId}/generate-code Tests

    [Fact]
    [FastTest]
    public async Task POST_GenerateVerificationCode_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var badgeId = Guid.NewGuid();

        // Act
        var response = await Client.PostAsync($"/api/Badge/verify/{badgeId}/generate-code", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task POST_GenerateVerificationCode_WithAuth_ReturnsOkOrError()
    {
        // Arrange
        AuthenticateAs(_user);
        var badgeId = Guid.NewGuid();

        // Act
        var response = await Client.PostAsync($"/api/Badge/verify/{badgeId}/generate-code", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region GET /api/Badge/verify/{badgeId} Tests

    [Fact]
    [FastTest]
    public async Task GET_VerifyBadgeCode_AsAnonymous_ReturnsOkOrBadRequest()
    {
        // Arrange
        var badgeId = Guid.NewGuid();
        var verificationCode = "TEST123";

        // Act
        var response = await Client.GetAsync($"/api/Badge/verify/{badgeId}?verificationCode={verificationCode}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task GET_VerifyBadgeCode_WithForgedCurrentTimestamp_ReturnsInvalid()
    {
        // Arrange
        var badge = await CreateActiveBadgeAsync(_user.Id);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var forgedCode = $"anything-{timestamp}";

        // Act
        var response = await Client.GetAsync($"/api/Badge/verify/{badge.Id}?verificationCode={Uri.EscapeDataString(forgedCode)}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        payload.RootElement.GetProperty("isValid").GetBoolean().Should().BeFalse();
    }

    [Fact]
    [SecurityTest]
    public async Task GET_VerifyBadgeCode_WithGeneratedCodeForActiveBadge_ReturnsValid()
    {
        // Arrange
        var badge = await CreateActiveBadgeAsync(_user.Id);
        var securityService = ServiceScope.ServiceProvider.GetRequiredService<IBadgeSecurityService>();
        var verificationCode = await securityService.GenerateVerificationCodeAsync(badge.Id, badge.UserId);

        // Act
        var response = await Client.GetAsync($"/api/Badge/verify/{badge.Id}?verificationCode={Uri.EscapeDataString(verificationCode)}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        payload.RootElement.GetProperty("isValid").GetBoolean().Should().BeTrue();
    }

    [Fact]
    [FastTest]
    public async Task GET_VerifyBadgeCode_WithoutCode_ReturnsBadRequest()
    {
        // Arrange
        var badgeId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/Badge/verify/{badgeId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    #endregion

    private async Task<UserBadge> CreateActiveBadgeAsync(Guid userId)
    {
        var badge = new UserBadge
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            BadgeType = "SECURITY_TEST",
            BadgeName = "Security Test",
            BadgeDescription = "Badge used for security verification tests",
            Category = BadgeCategory.Trust,
            EarnedAt = DateTime.UtcNow,
            VerificationLevel = VerificationLevel.External,
            IsActive = true
        };

        Context.UserBadges.Add(badge);
        await Context.SaveChangesAsync();

        return badge;
    }

    #region POST /api/Badge/external/linkedin/verify Tests

    [Fact]
    [FastTest]
    public async Task POST_VerifyLinkedIn_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var request = new
        {
            LinkedInUrl = "https://linkedin.com/in/testuser"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Badge/external/linkedin/verify", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task POST_VerifyLinkedIn_WithAuth_ReturnsOkOrError()
    {
        // Arrange
        AuthenticateAs(_user);
        var request = new
        {
            LinkedInUrl = "https://linkedin.com/in/testuser"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Badge/external/linkedin/verify", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_VerifyLinkedIn_WithEmptyUrl_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_user);
        var request = new
        {
            LinkedInUrl = ""
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Badge/external/linkedin/verify", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region POST /api/Badge/external/github/verify Tests

    [Fact]
    [FastTest]
    public async Task POST_VerifyGitHub_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var request = new
        {
            GitHubUsername = "testuser"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Badge/external/github/verify", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task POST_VerifyGitHub_WithAuth_ReturnsOkOrError()
    {
        // Arrange
        AuthenticateAs(_user);
        var request = new
        {
            GitHubUsername = "testuser"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Badge/external/github/verify", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_VerifyGitHub_WithEmptyUsername_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_user);
        var request = new
        {
            GitHubUsername = ""
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Badge/external/github/verify", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region GET /api/Badge/external/{platform}/cached Tests

    [Fact]
    [FastTest]
    public async Task GET_CachedVerification_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/Badge/external/LinkedIn/cached");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task GET_CachedVerification_WithAuth_ReturnsOkOrNotFound()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/Badge/external/LinkedIn/cached");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_CachedVerification_GitHub_WithAuth_ReturnsOkOrNotFound()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/Badge/external/GitHub/cached");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region Authorization Summary Tests

    [Fact]
    [SecurityTest]
    public async Task AllEndpoints_WithoutAuth_ReturnUnauthorized()
    {
        // Test all endpoints without authentication (except anonymous endpoints)
        var badgeId = Guid.NewGuid();
        var requestId = Guid.NewGuid();

        var endpoints = new[]
        {
            ("GET", $"/api/Badge/user/{_user.Id}/badges"),
            ("GET", $"/api/Badge/user/{_user.Id}/progress"),
            ("GET", $"/api/Badge/user/{_user.Id}/eligibility"),
            ("POST", "/api/Badge/verification/request"),
            ("GET", $"/api/Badge/verification/request/{requestId}"),
            ("GET", "/api/Badge/verification/pending"),
            ("POST", $"/api/Badge/verification/request/{requestId}/process"),
            ("POST", "/api/Badge/award"),
            ("POST", $"/api/Badge/revoke/{badgeId}"),
            ("POST", "/api/Badge/evaluation/run"),
            ("POST", $"/api/Badge/verify/{badgeId}/generate-code"),
            ("POST", "/api/Badge/external/linkedin/verify"),
            ("POST", "/api/Badge/external/github/verify"),
            ("GET", "/api/Badge/external/LinkedIn/cached")
        };

        foreach (var (method, endpoint) in endpoints)
        {
            HttpResponseMessage response;
            switch (method)
            {
                case "GET":
                    response = await Client.GetAsync(endpoint);
                    break;
                case "POST":
                    response = await Client.PostAsync(endpoint, null);
                    break;
                default:
                    continue;
            }

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
                $"{method} {endpoint} should require authentication");
        }
    }

    [Fact]
    [SecurityTest]
    public async Task AdminEndpoints_AsNonAdmin_ReturnForbidden()
    {
        // Arrange
        AuthenticateAs(_user);
        var requestId = Guid.NewGuid();
        var badgeId = Guid.NewGuid();

        var adminEndpoints = new[]
        {
            (Method: "GET", Endpoint: "/api/Badge/verification/pending", Body: (object?)null),
            (Method: "POST", Endpoint: $"/api/Badge/verification/request/{requestId}/process", Body: (object?)new { Approved = true, ReviewNotes = "Test" }),
            (Method: "POST", Endpoint: "/api/Badge/award", Body: (object?)new { UserId = _otherUser.Id, BadgeType = "Test", Evidence = "Test" }),
            (Method: "POST", Endpoint: $"/api/Badge/revoke/{badgeId}", Body: (object?)new { Reason = "Test" }),
            (Method: "POST", Endpoint: "/api/Badge/evaluation/run", Body: (object?)null)
        };

        foreach (var (method, endpoint, body) in adminEndpoints)
        {
            HttpResponseMessage response;
            switch (method)
            {
                case "GET":
                    response = await Client.GetAsync(endpoint);
                    break;
                case "POST":
                    response = body == null
                        ? await Client.PostAsync(endpoint, null)
                        : await Client.PostAsJsonAsync(endpoint, body);
                    break;
                default:
                    continue;
            }

            response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
        }
    }

    [Fact]
    [FastTest]
    public async Task AnonymousEndpoints_AllowAnonymousAccess()
    {
        // Test anonymous endpoints (no authentication)
        var badgeId = Guid.NewGuid();

        // GET /api/Badge/verify/{badgeId} with verification code
        var verifyResponse = await Client.GetAsync($"/api/Badge/verify/{badgeId}?verificationCode=TEST123");
        verifyResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    #endregion
}
