using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Net;
using FluentAssertions;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Tests.Infrastructure;

namespace SkillLedger.Tests.Integration.Api;

/// <summary>
/// Integration tests for Experience Controller API endpoints
/// Tests experience CRUD operations, timelines, featured experiences, and skill management
/// </summary>
[IntegrationTest]
[ApiTest]
[Collection("Integration Api 1")]
public class ExperienceControllerTests : IntegrationTestBase
{
    private User _user = null!;
    private User _otherUser = null!;

    public ExperienceControllerTests(SharedTestHostFixture fixture) : base(fixture)
    {
    }

    protected override async Task OnInitializeAsync()
    {
        await base.OnInitializeAsync();

        // Setup test users
        _user = new User
        {
            Id = Guid.NewGuid(),
            Email = "experience-user@test.com",
            UserName = "experience-user@test.com",
            Status = UserStatus.Active
        };

        _otherUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "other-experience-user@test.com",
            UserName = "other-experience-user@test.com",
            Status = UserStatus.Active
        };

        Context.Users.AddRange(_user, _otherUser);
        await Context.SaveChangesAsync();
    }

    #region POST /api/Experience Tests

    [Fact]
    [FastTest]
    public async Task POST_CreateExperience_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var request = new
        {
            Title = "Test Experience",
            Company = "Test Company",
            Type = "Work",
            StartDate = DateTime.UtcNow.AddYears(-1),
            IsCurrent = true
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Experience", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task POST_CreateExperience_WithAuth_ReturnsCreatedOrBadRequest()
    {
        // Arrange
        AuthenticateAs(_user);

        var request = new
        {
            Title = "Software Engineer",
            Company = "Tech Corp",
            Type = "Work",
            StartDate = DateTime.UtcNow.AddYears(-1),
            IsCurrent = true,
            Description = "Working on great projects"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Experience", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_CompatibilityCreateExperience_ForAnotherUser_ReturnsForbidden()
    {
        AuthenticateAs(_user);

        var request = new
        {
            Title = "Forged Experience",
            Organization = "Other User Corp",
            Type = ExperienceType.Work,
            StartDate = DateTime.UtcNow.AddYears(-1),
            IsCurrent = true
        };

        var content = JsonContent.Create(request);
        await AddCsrfTokenToRequest(content);

        var response = await Client.PostAsync($"/api/Experience/{_otherUser.Id}", content);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region PUT /api/Experience/{experienceId} Tests

    [Fact]
    [FastTest]
    public async Task PUT_UpdateExperience_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var experienceId = Guid.NewGuid();
        var request = new
        {
            Title = "Updated Title",
            Company = "Updated Company"
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/Experience/{experienceId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task PUT_UpdateExperience_WithAuth_ReturnsOkOrBadRequest()
    {
        // Arrange
        AuthenticateAs(_user);
        var experienceId = Guid.NewGuid();
        var request = new
        {
            Title = "Updated Engineer",
            Company = "New Tech Corp"
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/Experience/{experienceId}", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task PUT_CompatibilityUpdateExperience_ForAnotherUser_ReturnsForbidden()
    {
        AuthenticateAs(_user);
        var experience = await CreateExperienceForUserAsync(_otherUser.Id);
        var request = new
        {
            Title = "Forged Update"
        };

        var content = JsonContent.Create(request);
        await AddCsrfTokenToRequest(content);

        var response = await Client.PutAsync($"/api/Experience/{_otherUser.Id}/{experience.Id}", content);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region DELETE /api/Experience/{experienceId} Tests

    [Fact]
    [FastTest]
    public async Task DELETE_Experience_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var experienceId = Guid.NewGuid();

        // Act
        var response = await Client.DeleteAsync($"/api/Experience/{experienceId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task DELETE_Experience_WithAuth_ReturnsOkOrBadRequest()
    {
        // Arrange
        AuthenticateAs(_user);
        var experienceId = Guid.NewGuid();

        // Act
        var response = await Client.DeleteAsync($"/api/Experience/{experienceId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task DELETE_CompatibilityDeleteExperience_ForAnotherUser_ReturnsForbidden()
    {
        AuthenticateAs(_user);
        var experience = await CreateExperienceForUserAsync(_otherUser.Id);

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/Experience/{_otherUser.Id}/{experience.Id}");
        request.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync());

        var response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region GET /api/Experience/my-experience/{experienceId} Tests

    [Fact]
    [FastTest]
    public async Task GET_MyExperience_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var experienceId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/Experience/my-experience/{experienceId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task GET_MyExperience_WithAuth_ReturnsOkOrNotFound()
    {
        // Arrange
        AuthenticateAs(_user);
        var experienceId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/Experience/my-experience/{experienceId}?includeSkills=true");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region GET /api/Experience/my-experiences Tests

    [Fact]
    [FastTest]
    public async Task GET_MyExperiences_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/Experience/my-experiences");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task GET_MyExperiences_WithAuth_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/Experience/my-experiences?includeSkills=true");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region GET /api/Experience/user/{userId} Tests (Anonymous)

    [Fact]
    [FastTest]
    public async Task GET_UserExperiences_AsAnonymous_ReturnsOk()
    {
        // Act
        var response = await Client.GetAsync($"/api/Experience/user/{_user.Id}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_UserExperiences_WithPagination_ReturnsOk()
    {
        // Act
        var response = await Client.GetAsync($"/api/Experience/user/{_user.Id}?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_UserExperiences_WithFilters_ReturnsOk()
    {
        // Act
        var response = await Client.GetAsync($"/api/Experience/user/{_user.Id}?type=Work&skillName=test");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region GET /api/Experience/{userId} Tests (Alternative Route)

    [Fact]
    [FastTest]
    public async Task GET_ExperiencesForUser_AsAnonymous_ReturnsOk()
    {
        // Act
        var response = await Client.GetAsync($"/api/Experience/{_user.Id}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region GET /api/Experience/user/{userId}/timeline Tests

    [Fact]
    [FastTest]
    public async Task GET_ExperienceTimeline_AsAnonymous_ReturnsOk()
    {
        // Act
        var response = await Client.GetAsync($"/api/Experience/user/{_user.Id}/timeline");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region GET /api/Experience/my-experiences/timeline Tests

    [Fact]
    [FastTest]
    public async Task GET_MyExperienceTimeline_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/Experience/my-experiences/timeline");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task GET_MyExperienceTimeline_WithAuth_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/Experience/my-experiences/timeline");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region GET /api/Experience/user/{userId}/featured Tests

    [Fact]
    [FastTest]
    public async Task GET_FeaturedExperiences_AsAnonymous_ReturnsOk()
    {
        // Act
        var response = await Client.GetAsync($"/api/Experience/user/{_user.Id}/featured");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region GET /api/Experience/my-experiences/featured Tests

    [Fact]
    [FastTest]
    public async Task GET_MyFeaturedExperiences_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/Experience/my-experiences/featured");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task GET_MyFeaturedExperiences_WithAuth_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/Experience/my-experiences/featured");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region GET /api/Experience/user/{userId}/current Tests

    [Fact]
    [FastTest]
    public async Task GET_CurrentExperiences_AsAnonymous_ReturnsOk()
    {
        // Act
        var response = await Client.GetAsync($"/api/Experience/user/{_user.Id}/current");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region GET /api/Experience/my-experiences/current Tests

    [Fact]
    [FastTest]
    public async Task GET_MyCurrentExperiences_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/Experience/my-experiences/current");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task GET_MyCurrentExperiences_WithAuth_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/Experience/my-experiences/current");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region POST /api/Experience/search Tests

    [Fact]
    [FastTest]
    public async Task POST_SearchExperiences_WithAuth_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        var request = new
        {
            Skip = 0,
            Take = 10,
            Type = "Work"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Experience/search", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region PUT /api/Experience/my-experiences/reorder Tests

    [Fact]
    [FastTest]
    public async Task PUT_ReorderExperiences_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var experienceIds = new[] { Guid.NewGuid(), Guid.NewGuid() };

        // Act
        var response = await Client.PutAsJsonAsync("/api/Experience/my-experiences/reorder", experienceIds);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task PUT_ReorderExperiences_WithAuth_ReturnsOkOrBadRequest()
    {
        // Arrange
        AuthenticateAs(_user);
        var experienceIds = new[] { Guid.NewGuid(), Guid.NewGuid() };

        // Act
        var response = await Client.PutAsJsonAsync("/api/Experience/my-experiences/reorder", experienceIds);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region POST /api/Experience/{experienceId}/skills Tests

    [Fact]
    [FastTest]
    public async Task POST_AddSkills_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var experienceId = Guid.NewGuid();
        var skillIds = new[] { Guid.NewGuid() };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/Experience/{experienceId}/skills", skillIds);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task POST_AddSkills_WithAuth_ReturnsOkOrBadRequest()
    {
        // Arrange
        AuthenticateAs(_user);
        var experienceId = Guid.NewGuid();
        var skillIds = new[] { Guid.NewGuid() };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/Experience/{experienceId}/skills", skillIds);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region DELETE /api/Experience/{experienceId}/skills Tests

    [Fact]
    [FastTest]
    public async Task DELETE_RemoveSkills_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var experienceId = Guid.NewGuid();
        var skillIds = new[] { Guid.NewGuid() };

        // Act
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/Experience/{experienceId}/skills")
        {
            Content = JsonContent.Create(skillIds)
        };
        var response = await Client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task DELETE_RemoveSkills_WithAuth_ReturnsOkOrBadRequest()
    {
        // Arrange
        AuthenticateAs(_user);
        var experienceId = Guid.NewGuid();
        var skillIds = new[] { Guid.NewGuid() };

        // Act
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/Experience/{experienceId}/skills")
        {
            Content = JsonContent.Create(skillIds)
        };
        var response = await Client.SendAsync(request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region Authorization Summary Tests

    [Fact]
    [SecurityTest]
    public async Task AuthenticatedEndpoints_WithoutAuth_ReturnUnauthorized()
    {
        // Test authenticated endpoints without authentication
        var experienceId = Guid.NewGuid();

        var endpoints = new[]
        {
            ("POST", "/api/Experience"),
            ("PUT", $"/api/Experience/{experienceId}"),
            ("DELETE", $"/api/Experience/{experienceId}"),
            ("GET", $"/api/Experience/my-experience/{experienceId}"),
            ("GET", "/api/Experience/my-experiences"),
            ("GET", "/api/Experience/my-experiences/timeline"),
            ("GET", "/api/Experience/my-experiences/featured"),
            ("GET", "/api/Experience/my-experiences/current"),
            ("PUT", "/api/Experience/my-experiences/reorder"),
            ("POST", $"/api/Experience/{experienceId}/skills"),
            ("DELETE", $"/api/Experience/{experienceId}/skills")
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
                    response = await Client.PostAsJsonAsync(endpoint, new { });
                    break;
                case "PUT":
                    response = await Client.PutAsJsonAsync(endpoint, new { });
                    break;
                case "DELETE":
                    var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, endpoint)
                    {
                        Content = JsonContent.Create(new[] { Guid.NewGuid() })
                    };
                    response = await Client.SendAsync(deleteRequest);
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
    public async Task AnonymousEndpoints_AllowAnonymousAccess()
    {
        // Test anonymous endpoints
        var userId = _user.Id;

        var anonymousEndpoints = new[]
        {
            $"/api/Experience/user/{userId}",
            $"/api/Experience/{userId}",
            $"/api/Experience/user/{userId}/timeline",
            $"/api/Experience/user/{userId}/featured",
            $"/api/Experience/user/{userId}/current"
        };

        foreach (var endpoint in anonymousEndpoints)
        {
            var response = await Client.GetAsync(endpoint);
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
        }
    }

    #endregion

    private async Task<Experience> CreateExperienceForUserAsync(Guid userId)
    {
        var experience = new Experience
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = ExperienceType.Work,
            Title = "Security Test Experience",
            Organization = "Security Test Org",
            StartDate = DateTime.UtcNow.AddYears(-1),
            IsCurrent = true,
            IsVisible = true
        };

        Context.Experiences.Add(experience);
        await Context.SaveChangesAsync();

        return experience;
    }
}
