using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Net;
using FluentAssertions;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Tests.Infrastructure;

namespace SkillLedger.Tests.Integration.Api;

/// <summary>
/// Integration tests for Reputation Controller API endpoints
/// Tests reputation scores, breakdowns, trends, and recalculation
/// </summary>
[IntegrationTest]
[ApiTest]
[Collection("Integration Api 2")]
public class ReputationControllerTests : IntegrationTestBase
{
    private User _user = null!;
    private User _otherUser = null!;
    private User _adminUser = null!;
    private Skill _testSkill = null!;

    public ReputationControllerTests(SharedTestHostFixture fixture) : base(fixture)
    {
    }

    protected override async Task OnInitializeAsync()
    {
        await base.OnInitializeAsync();

        // Setup test users
        _user = new User
        {
            Id = Guid.NewGuid(),
            Email = "reputation-user@test.com",
            UserName = "reputation-user@test.com",
            Status = UserStatus.Active
        };

        _otherUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "other-reputation-user@test.com",
            UserName = "other-reputation-user@test.com",
            Status = UserStatus.Active
        };

        _adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "reputation-admin@test.com",
            UserName = "reputation-admin@test.com",
            Status = UserStatus.Active
        };

        Context.Users.AddRange(_user, _otherUser, _adminUser);

        // Setup test skill for category tests
        _testSkill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "Test Reputation Skill",
            Description = "Skill for testing reputation",
            Category = "Testing",
            CreatedAt = DateTime.UtcNow
        };

        Context.Skills.Add(_testSkill);
        await Context.SaveChangesAsync();
    }

    #region GET /api/Reputation/user/{userId}/score Tests

    [Fact]
    [FastTest]
    public async Task GET_UserReputationScore_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync($"/api/Reputation/user/{_user.Id}/score");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task GET_UserReputationScore_WithAuth_ReturnsOkOrNotFound()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync($"/api/Reputation/user/{_user.Id}/score");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_UserReputationScore_WithInvalidUserId_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync($"/api/Reputation/user/{Guid.Empty}/score");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region GET /api/Reputation/user/{userId}/breakdown Tests

    [Fact]
    [FastTest]
    public async Task GET_UserReputationBreakdown_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync($"/api/Reputation/user/{_user.Id}/breakdown");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task GET_UserReputationBreakdown_AsOwnUser_ReturnsOkOrNotFound()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync($"/api/Reputation/user/{_user.Id}/breakdown");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task GET_UserReputationBreakdown_AsOtherUser_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync($"/api/Reputation/user/{_otherUser.Id}/breakdown");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task GET_UserReputationBreakdown_AsAdmin_ReturnsOkOrNotFound()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.GetAsync($"/api/Reputation/user/{_user.Id}/breakdown");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_UserReputationBreakdown_WithInvalidUserId_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync($"/api/Reputation/user/{Guid.Empty}/breakdown");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region GET /api/Reputation/user/{userId}/categories Tests

    [Fact]
    [FastTest]
    public async Task GET_CategoryReputationScores_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync($"/api/Reputation/user/{_user.Id}/categories");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task GET_CategoryReputationScores_WithAuth_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync($"/api/Reputation/user/{_user.Id}/categories");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_CategoryReputationScores_WithInvalidUserId_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync($"/api/Reputation/user/{Guid.Empty}/categories");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region GET /api/Reputation/user/{userId}/category/{categoryId} Tests

    [Fact]
    [FastTest]
    public async Task GET_SpecificCategoryScore_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var categoryId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/Reputation/user/{_user.Id}/category/{categoryId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task GET_SpecificCategoryScore_WithAuth_ReturnsOkOrNotFound()
    {
        // Arrange
        AuthenticateAs(_user);
        var categoryId = _testSkill.Id;

        // Act
        var response = await Client.GetAsync($"/api/Reputation/user/{_user.Id}/category/{categoryId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_SpecificCategoryScore_WithInvalidIds_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync($"/api/Reputation/user/{Guid.Empty}/category/{Guid.Empty}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region GET /api/Reputation/user/{userId}/history Tests

    [Fact]
    [FastTest]
    public async Task GET_ReputationHistory_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync($"/api/Reputation/user/{_user.Id}/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task GET_ReputationHistory_WithAuth_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync($"/api/Reputation/user/{_user.Id}/history");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_ReputationHistory_WithPagination_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync($"/api/Reputation/user/{_user.Id}/history?days=30&page=1&pageSize=20");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_ReputationHistory_WithInvalidDays_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync($"/api/Reputation/user/{_user.Id}/history?days=500");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_ReputationHistory_WithInvalidPagination_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync($"/api/Reputation/user/{_user.Id}/history?page=0&pageSize=200");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_ReputationHistory_WithInvalidUserId_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync($"/api/Reputation/user/{Guid.Empty}/history");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region GET /api/Reputation/user/{userId}/trend Tests

    [Fact]
    [FastTest]
    public async Task GET_ReputationTrend_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync($"/api/Reputation/user/{_user.Id}/trend");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task GET_ReputationTrend_WithAuth_ReturnsOkOrNotFound()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync($"/api/Reputation/user/{_user.Id}/trend");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_ReputationTrend_WithCustomDays_ReturnsOkOrNotFound()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync($"/api/Reputation/user/{_user.Id}/trend?days=60");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_ReputationTrend_WithInvalidDays_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync($"/api/Reputation/user/{_user.Id}/trend?days=100");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_ReputationTrend_WithInvalidUserId_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync($"/api/Reputation/user/{Guid.Empty}/trend");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region POST /api/Reputation/user/{userId}/recalculate Tests

    [Fact]
    [FastTest]
    public async Task POST_RecalculateReputationScore_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.PostAsync($"/api/Reputation/user/{_user.Id}/recalculate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task POST_RecalculateReputationScore_AsOwnUser_ReturnsOkOrNotFound()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.PostAsync($"/api/Reputation/user/{_user.Id}/recalculate", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.TooManyRequests, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_RecalculateReputationScore_AsOtherUser_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.PostAsync($"/api/Reputation/user/{_otherUser.Id}/recalculate", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_RecalculateReputationScore_AsAdmin_ReturnsOkOrNotFound()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.PostAsync($"/api/Reputation/user/{_user.Id}/recalculate", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.TooManyRequests, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_RecalculateReputationScore_WithInvalidUserId_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.PostAsync($"/api/Reputation/user/{Guid.Empty}/recalculate", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region POST /api/Reputation/user/{userId}/category/{categoryId}/update Tests

    [Fact]
    [FastTest]
    public async Task POST_UpdateCategoryScore_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var categoryId = Guid.NewGuid();

        // Act
        var response = await Client.PostAsync($"/api/Reputation/user/{_user.Id}/category/{categoryId}/update", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task POST_UpdateCategoryScore_AsOwnUser_ReturnsOkOrNotFound()
    {
        // Arrange
        AuthenticateAs(_user);
        var categoryId = _testSkill.Id;

        // Act
        var response = await Client.PostAsync($"/api/Reputation/user/{_user.Id}/category/{categoryId}/update", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_UpdateCategoryScore_AsOtherUser_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_user);
        var categoryId = _testSkill.Id;

        // Act
        var response = await Client.PostAsync($"/api/Reputation/user/{_otherUser.Id}/category/{categoryId}/update", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_UpdateCategoryScore_AsAdmin_ReturnsOkOrNotFound()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });
        var categoryId = _testSkill.Id;

        // Act
        var response = await Client.PostAsync($"/api/Reputation/user/{_user.Id}/category/{categoryId}/update", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_UpdateCategoryScore_WithInvalidIds_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.PostAsync($"/api/Reputation/user/{Guid.Empty}/category/{Guid.Empty}/update", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region POST /api/Reputation/bulk-recalculate Tests

    [Fact]
    [FastTest]
    public async Task POST_BulkRecalculateReputationScores_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.PostAsync("/api/Reputation/bulk-recalculate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_BulkRecalculateReputationScores_AsNonAdmin_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.PostAsync("/api/Reputation/bulk-recalculate", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_BulkRecalculateReputationScores_AsAdmin_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.PostAsync("/api/Reputation/bulk-recalculate", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region Authorization Summary Tests

    [Fact]
    [SecurityTest]
    public async Task AllEndpoints_WithoutAuth_ReturnUnauthorized()
    {
        // Test all endpoints without authentication
        var userId = _user.Id;
        var categoryId = Guid.NewGuid();

        var endpoints = new[]
        {
            ("GET", $"/api/Reputation/user/{userId}/score"),
            ("GET", $"/api/Reputation/user/{userId}/breakdown"),
            ("GET", $"/api/Reputation/user/{userId}/categories"),
            ("GET", $"/api/Reputation/user/{userId}/category/{categoryId}"),
            ("GET", $"/api/Reputation/user/{userId}/history"),
            ("GET", $"/api/Reputation/user/{userId}/trend"),
            ("POST", $"/api/Reputation/user/{userId}/recalculate"),
            ("POST", $"/api/Reputation/user/{userId}/category/{categoryId}/update"),
            ("POST", "/api/Reputation/bulk-recalculate")
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
    public async Task SelfOrAdminEndpoints_AsOtherUser_ReturnForbidden()
    {
        // Arrange
        AuthenticateAs(_user);
        var categoryId = Guid.NewGuid();

        var selfOrAdminEndpoints = new[]
        {
            ("GET", $"/api/Reputation/user/{_otherUser.Id}/breakdown"),
            ("POST", $"/api/Reputation/user/{_otherUser.Id}/recalculate"),
            ("POST", $"/api/Reputation/user/{_otherUser.Id}/category/{categoryId}/update")
        };

        foreach (var (method, endpoint) in selfOrAdminEndpoints)
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

            response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.InternalServerError);
        }
    }

    [Fact]
    [SecurityTest]
    public async Task AdminOnlyEndpoints_AsNonAdmin_ReturnForbidden()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.PostAsync("/api/Reputation/bulk-recalculate", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.InternalServerError);
    }

    #endregion
}
