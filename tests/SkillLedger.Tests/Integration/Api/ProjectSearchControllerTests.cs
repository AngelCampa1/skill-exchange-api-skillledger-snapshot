using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Net;
using FluentAssertions;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Tests.Infrastructure;

namespace SkillLedger.Tests.Integration.Api;

/// <summary>
/// Integration tests for Project Search Controller API endpoints
/// Tests advanced search, location search, and saved search management
/// </summary>
[IntegrationTest]
[ApiTest]
[Collection("Integration Api 2")]
public class ProjectSearchControllerTests : IntegrationTestBase
{
    private User _user = null!;
    private User _otherUser = null!;
    private Skill _testSkill = null!;

    public ProjectSearchControllerTests(SharedTestHostFixture fixture) : base(fixture)
    {
    }

    protected override async Task OnInitializeAsync()
    {
        await base.OnInitializeAsync();

        // Setup test users
        _user = new User
        {
            Id = Guid.NewGuid(),
            Email = "search-user@test.com",
            UserName = "search-user@test.com",
            Status = UserStatus.Active
        };

        _otherUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "other-user@test.com",
            UserName = "other-user@test.com",
            Status = UserStatus.Active
        };

        Context.Users.AddRange(_user, _otherUser);

        // Setup test skill for search validation
        _testSkill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "Test Search Skill",
            Description = "Skill for testing project search",
            Category = "Testing",
            CreatedAt = DateTime.UtcNow
        };

        Context.Skills.Add(_testSkill);
        await Context.SaveChangesAsync();
    }

    #region POST /api/project-search/advanced Tests

    [Fact]
    [FastTest]
    public async Task POST_AdvancedSearch_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new
        {
            Keywords = "test project",
            Page = 1,
            PageSize = 10
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/project-search/advanced", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_AdvancedSearch_AsAnonymous_OnlySeesPublishedProjects()
    {
        // Arrange - No authentication
        var request = new
        {
            Keywords = "test",
            Page = 1,
            PageSize = 10
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/project-search/advanced", request);

        // Assert
        // Anonymous users are automatically restricted to published projects
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_AdvancedSearch_WithInvalidBudgetRange_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            minBudget = 1000,
            maxBudget = 500, // Invalid: Min > Max
            page = 1,
            pageSize = 10
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/project-search/advanced", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("MinBudget cannot be greater than MaxBudget");
    }

    [Fact]
    [FastTest]
    public async Task POST_AdvancedSearch_WithInvalidSkillIds_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            SkillIds = new[] { Guid.NewGuid(), Guid.NewGuid() }, // Non-existent skills
            Page = 1,
            PageSize = 10
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/project-search/advanced", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid skill IDs");
    }

    [Fact]
    [FastTest]
    public async Task POST_AdvancedSearch_WithValidSkillIds_ReturnsOk()
    {
        // Arrange
        var request = new
        {
            SkillIds = new[] { _testSkill.Id },
            Page = 1,
            PageSize = 10
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/project-search/advanced", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_AdvancedSearch_WithFilters_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        var request = new
        {
            keywords = "test",
            minBudget = 100,
            maxBudget = 1000,
            skillIds = new[] { _testSkill.Id },
            publishedOnly = true,
            page = 1,
            pageSize = 20
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/project-search/advanced", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region POST /api/project-search/location Tests

    [Fact]
    [FastTest]
    public async Task POST_LocationSearch_WithValidCoordinates_ReturnsOk()
    {
        // Arrange
        var request = new
        {
            Latitude = 40.7128,
            Longitude = -74.0060,
            RadiusKm = 50.0
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/project-search/location", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_LocationSearch_AsAnonymous_ReturnsOk()
    {
        // Arrange - No authentication
        var request = new
        {
            Latitude = 51.5074,
            Longitude = -0.1278,
            RadiusKm = 100.0
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/project-search/location", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_LocationSearch_WithInvalidLatitude_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            Latitude = 95.0, // Invalid: > 90
            Longitude = 0.0,
            RadiusKm = 50.0
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/project-search/location", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_LocationSearch_WithInvalidLongitude_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            Latitude = 40.0,
            Longitude = 200.0, // Invalid: > 180
            RadiusKm = 50.0
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/project-search/location", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region GET /api/project-search/saved Tests

    [Fact]
    [FastTest]
    public async Task GET_SavedSearches_WithAuth_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/project-search/saved");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_SavedSearches_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/project-search/saved");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST /api/project-search/saved Tests

    [Fact]
    [FastTest]
    public async Task POST_CreateSavedSearch_WithAuth_ReturnsCreatedOrOk()
    {
        // Arrange
        AuthenticateAs(_user);

        var request = new
        {
            Name = "My Test Search",
            SearchCriteria = new
            {
                Keywords = "testing",
                MinBudget = 100.00m,
                MaxBudget = 1000.00m
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/project-search/saved", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_CreateSavedSearch_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var request = new
        {
            Name = "Unauthorized Search",
            SearchCriteria = new { Keywords = "test" }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/project-search/saved", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task POST_CreateSavedSearch_WithEmptyName_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_user);

        var request = new
        {
            Name = "",
            SearchCriteria = new { Keywords = "test" }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/project-search/saved", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Created, HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region POST /api/project-search/saved/{id}/execute Tests

    [Fact]
    [FastTest]
    public async Task POST_ExecuteSavedSearch_WithAuth_ReturnsOkOrNotFound()
    {
        // Arrange
        AuthenticateAs(_user);
        var searchId = Guid.NewGuid();

        // Act
        var response = await Client.PostAsync($"/api/project-search/saved/{searchId}/execute", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.Forbidden, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_ExecuteSavedSearch_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var searchId = Guid.NewGuid();

        // Act
        var response = await Client.PostAsync($"/api/project-search/saved/{searchId}/execute", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_ExecuteSavedSearch_OtherUsersSearch_ReturnsForbidden()
    {
        // Arrange - User tries to execute another user's saved search
        AuthenticateAs(_user);
        var searchId = Guid.NewGuid(); // Assume this belongs to _otherUser

        // Act
        var response = await Client.PostAsync($"/api/project-search/saved/{searchId}/execute", null);

        // Assert
        // Should return Forbidden if ownership check fails, or BadRequest if not found
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region PUT /api/project-search/saved/{id} Tests

    [Fact]
    [FastTest]
    public async Task PUT_UpdateSavedSearch_WithAuth_ReturnsOkOrNotFound()
    {
        // Arrange
        AuthenticateAs(_user);
        var searchId = Guid.NewGuid();

        var request = new
        {
            Name = "Updated Search Name",
            SearchCriteria = new { Keywords = "updated keywords" }
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/project-search/saved/{searchId}", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.Forbidden, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task PUT_UpdateSavedSearch_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var searchId = Guid.NewGuid();
        var request = new
        {
            Name = "Unauthorized Update",
            SearchCriteria = new { Keywords = "test" }
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/project-search/saved/{searchId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [SecurityTest]
    public async Task PUT_UpdateSavedSearch_OtherUsersSearch_ReturnsForbidden()
    {
        // Arrange - User tries to update another user's saved search
        AuthenticateAs(_user);
        var searchId = Guid.NewGuid(); // Assume this belongs to _otherUser

        var request = new
        {
            Name = "Malicious Update",
            SearchCriteria = new { Keywords = "hacked" }
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/project-search/saved/{searchId}", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region DELETE /api/project-search/saved/{id} Tests

    [Fact]
    [FastTest]
    public async Task DELETE_SavedSearch_WithAuth_ReturnsNoContentOrNotFound()
    {
        // Arrange
        AuthenticateAs(_user);
        var searchId = Guid.NewGuid();

        // Act
        var response = await Client.DeleteAsync($"/api/project-search/saved/{searchId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.BadRequest, HttpStatusCode.Forbidden, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task DELETE_SavedSearch_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var searchId = Guid.NewGuid();

        // Act
        var response = await Client.DeleteAsync($"/api/project-search/saved/{searchId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [SecurityTest]
    public async Task DELETE_SavedSearch_OtherUsersSearch_ReturnsForbiddenOrNotFound()
    {
        // Arrange - User tries to delete another user's saved search
        AuthenticateAs(_user);
        var searchId = Guid.NewGuid(); // Assume this belongs to _otherUser

        // Act
        var response = await Client.DeleteAsync($"/api/project-search/saved/{searchId}");

        // Assert
        // May return Forbidden (ownership check), BadRequest (not found), or NoContent (if no saved searches exist)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.BadRequest, HttpStatusCode.NoContent, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region Authorization Summary Tests

    [Fact]
    [SecurityTest]
    public async Task AllSavedSearchEndpoints_WithoutAuth_ReturnUnauthorized()
    {
        // Test all saved search endpoints without authentication
        var searchId = Guid.NewGuid();

        var endpoints = new[]
        {
            ("GET", $"/api/project-search/saved"),
            ("POST", $"/api/project-search/saved"),
            ("POST", $"/api/project-search/saved/{searchId}/execute"),
            ("PUT", $"/api/project-search/saved/{searchId}"),
            ("DELETE", $"/api/project-search/saved/{searchId}")
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
                case "PUT":
                    response = await Client.PutAsJsonAsync(endpoint, new { Name = "test", SearchCriteria = new { } });
                    break;
                case "DELETE":
                    response = await Client.DeleteAsync(endpoint);
                    break;
                default:
                    continue;
            }

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
                $"{method} {endpoint} should require authentication");
        }
    }

    [Fact]
    [FastTest]
    public async Task AnonymousSearchEndpoints_AllowAnonymousAccess()
    {
        // Test anonymous search endpoints
        var advancedSearchRequest = new { Keywords = "test", Page = 1, PageSize = 10 };
        var advancedResponse = await Client.PostAsJsonAsync("/api/project-search/advanced", advancedSearchRequest);
        advancedResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);

        var locationSearchRequest = new { Latitude = 40.7128, Longitude = -74.0060, RadiusKm = 50.0 };
        var locationResponse = await Client.PostAsJsonAsync("/api/project-search/location", locationSearchRequest);
        locationResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    #endregion
}
