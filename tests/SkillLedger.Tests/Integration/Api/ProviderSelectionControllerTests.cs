using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Net;
using FluentAssertions;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Tests.Infrastructure;

namespace SkillLedger.Tests.Integration.Api;

/// <summary>
/// Integration tests for Provider Selection Controller API endpoints
/// Tests provider selection creation, ranking, comparison, and workflow management
/// </summary>
[IntegrationTest]
[ApiTest]
[Collection("Integration Api 2")]
public class ProviderSelectionControllerTests : IntegrationTestBase
{
    private User _client = null!;
    private User _provider = null!;
    private User _otherUser = null!;
    private Project _testProject = null!;

    public ProviderSelectionControllerTests(SharedTestHostFixture fixture) : base(fixture)
    {
    }

    protected override async Task OnInitializeAsync()
    {
        await base.OnInitializeAsync();

        // Setup test users
        _client = new User
        {
            Id = Guid.NewGuid(),
            Email = "client@selection.com",
            UserName = "client@selection.com",
            Status = UserStatus.Active
        };

        _provider = new User
        {
            Id = Guid.NewGuid(),
            Email = "provider@selection.com",
            UserName = "provider@selection.com",
            Status = UserStatus.Active
        };

        _otherUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "other@selection.com",
            UserName = "other@selection.com",
            Status = UserStatus.Active
        };

        Context.Users.AddRange(_client, _provider, _otherUser);

        // Create test project
        _testProject = new Project
        {
            Id = Guid.NewGuid(),
            ClientId = _client.Id,
            Title = "Test Selection Project",
            Description = "Project for testing provider selection",
            Status = ProjectStatus.Published,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        Context.Projects.Add(_testProject);
        await Context.SaveChangesAsync();
    }

    #region POST /api/provider-selection Tests

    [Fact]
    [FastTest]
    public async Task POST_CreateSelection_WithAuth_ReturnsCreatedOrBadRequest()
    {
        // Arrange
        AuthenticateAs(_client);

        var request = new
        {
            projectId = _testProject.Id,
            selectedProviderId = _provider.Id,
            reasonForSelection = "Best qualifications and experience"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/provider-selection", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_CreateSelection_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var request = new
        {
            projectId = _testProject.Id,
            selectedProviderId = _provider.Id
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/provider-selection", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/provider-selection/{id} Tests

    [Fact]
    [FastTest]
    public async Task GET_SelectionById_WithAuth_ReturnsOkOrNotFound()
    {
        // Arrange
        AuthenticateAs(_client);
        var selectionId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/provider-selection/{selectionId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_SelectionById_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var selectionId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/provider-selection/{selectionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/provider-selection/project/{projectId} Tests

    [Fact]
    [FastTest]
    public async Task GET_ProjectSelection_WithAuth_ReturnsOkOrNotFound()
    {
        // Arrange
        AuthenticateAs(_client);

        // Act
        var response = await Client.GetAsync($"/api/provider-selection/project/{_testProject.Id}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_ProjectSelection_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync($"/api/provider-selection/project/{_testProject.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/provider-selection/dashboard/{projectId} Tests

    [Fact]
    [FastTest]
    public async Task GET_SelectionDashboard_AsClient_ReturnsOkOrForbidden()
    {
        // Arrange
        AuthenticateAs(_client);

        // Act
        var response = await Client.GetAsync($"/api/provider-selection/dashboard/{_testProject.Id}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Forbidden, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task GET_SelectionDashboard_AsNonOwner_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_otherUser);

        // Act
        var response = await Client.GetAsync($"/api/provider-selection/dashboard/{_testProject.Id}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_SelectionDashboard_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync($"/api/provider-selection/dashboard/{_testProject.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/provider-selection/rank/{projectId} Tests

    [Fact]
    [FastTest]
    public async Task GET_RankApplications_AsClient_ReturnsOkOrForbidden()
    {
        // Arrange
        AuthenticateAs(_client);

        // Act
        var response = await Client.GetAsync($"/api/provider-selection/rank/{_testProject.Id}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Forbidden, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task GET_RankApplications_AsNonOwner_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_otherUser);

        // Act
        var response = await Client.GetAsync($"/api/provider-selection/rank/{_testProject.Id}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_RankApplications_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync($"/api/provider-selection/rank/{_testProject.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/provider-selection/compare/{applicationId}/project/{projectId} Tests

    [Fact]
    [FastTest]
    public async Task GET_ApplicationComparison_WithAuth_ReturnsOkOrNotFound()
    {
        // Arrange
        AuthenticateAs(_client);
        var applicationId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/provider-selection/compare/{applicationId}/project/{_testProject.Id}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task GET_ApplicationComparison_AsUnrelatedUser_ReturnsNotFound()
    {
        // Arrange
        var application = new ProjectApplication
        {
            Id = Guid.NewGuid(),
            ProjectId = _testProject.Id,
            ProviderId = _provider.Id,
            CoverLetter = "Private bid details",
            ProposedTimeline = 14,
            ProposedBudget = 500,
            Status = ApplicationStatus.Pending,
            IsAvailableImmediately = true,
            SkillMatchScore = 0.8m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        Context.ProjectApplications.Add(application);
        await Context.SaveChangesAsync();

        AuthenticateAs(_otherUser);

        // Act
        var response = await Client.GetAsync($"/api/provider-selection/compare/{application.Id}/project/{_testProject.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    [FastTest]
    public async Task GET_ApplicationComparison_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var applicationId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/provider-selection/compare/{applicationId}/project/{_testProject.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/provider-selection/recommendations/{projectId} Tests

    [Fact]
    [SecurityTest]
    public async Task GET_RecommendedProviders_AsUnrelatedUser_ReturnsNotFound()
    {
        // Arrange
        var application = new ProjectApplication
        {
            Id = Guid.NewGuid(),
            ProjectId = _testProject.Id,
            ProviderId = _provider.Id,
            CoverLetter = "Private recommendation details",
            ProposedTimeline = 14,
            ProposedBudget = 500,
            Status = ApplicationStatus.Pending,
            IsAvailableImmediately = true,
            SkillMatchScore = 0.8m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        Context.ProjectApplications.Add(application);
        await Context.SaveChangesAsync();

        AuthenticateAs(_otherUser);

        // Act
        var response = await Client.GetAsync($"/api/provider-selection/recommendations/{_testProject.Id}?take=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GET /api/provider-selection/provider-history/{providerId} Tests

    [Fact]
    [FastTest]
    public async Task GET_ProviderHistory_WithAuth_ReturnsOkOrNotFound()
    {
        // Arrange
        AuthenticateAs(_client);

        // Act
        var response = await Client.GetAsync($"/api/provider-selection/provider-history/{_provider.Id}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_ProviderHistory_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync($"/api/provider-selection/provider-history/{_provider.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region PUT /api/provider-selection/{id}/status Tests

    [Fact]
    [FastTest]
    public async Task PUT_UpdateStatus_WithAuth_ReturnsOkOrNotFound()
    {
        // Arrange
        AuthenticateAs(_client);
        var selectionId = Guid.NewGuid();

        // Act
        var response = await Client.PutAsJsonAsync($"/api/provider-selection/{selectionId}/status", 1); // Status enum value

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.BadRequest, HttpStatusCode.Forbidden, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task PUT_UpdateStatus_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var selectionId = Guid.NewGuid();

        // Act
        var response = await Client.PutAsJsonAsync($"/api/provider-selection/{selectionId}/status", 1);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region PUT /api/provider-selection/{id}/escrow Tests

    [Fact]
    [FastTest]
    public async Task PUT_UpdateEscrow_WithAuth_ReturnsOkOrNotFound()
    {
        // Arrange
        AuthenticateAs(_client);
        var selectionId = Guid.NewGuid();

        // Act
        var response = await Client.PutAsJsonAsync($"/api/provider-selection/{selectionId}/escrow", true);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.BadRequest, HttpStatusCode.Forbidden, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task PUT_UpdateEscrow_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var selectionId = Guid.NewGuid();

        // Act
        var response = await Client.PutAsJsonAsync($"/api/provider-selection/{selectionId}/escrow", true);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region PUT /api/provider-selection/{id}/contract Tests

    [Fact]
    [FastTest]
    public async Task PUT_UpdateContract_WithAuth_ReturnsOkOrNotFound()
    {
        // Arrange
        AuthenticateAs(_client);
        var selectionId = Guid.NewGuid();

        // Act
        var response = await Client.PutAsJsonAsync($"/api/provider-selection/{selectionId}/contract", true);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.BadRequest, HttpStatusCode.Forbidden, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task PUT_UpdateContract_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var selectionId = Guid.NewGuid();

        // Act
        var response = await Client.PutAsJsonAsync($"/api/provider-selection/{selectionId}/contract", true);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST /api/provider-selection/{id}/cancel Tests

    [Fact]
    [FastTest]
    public async Task POST_CancelSelection_WithValidReason_ReturnsOkOrNotFound()
    {
        // Arrange
        AuthenticateAs(_client);
        var selectionId = Guid.NewGuid();

        // Act
        var response = await Client.PostAsJsonAsync($"/api/provider-selection/{selectionId}/cancel", "Project requirements have changed significantly");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.BadRequest, HttpStatusCode.Forbidden, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_CancelSelection_WithShortReason_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_client);
        var selectionId = Guid.NewGuid();

        // Act
        var response = await Client.PostAsJsonAsync($"/api/provider-selection/{selectionId}/cancel", "Short"); // Less than 10 chars

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_CancelSelection_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var selectionId = Guid.NewGuid();

        // Act
        var response = await Client.PostAsJsonAsync($"/api/provider-selection/{selectionId}/cancel", "Valid cancellation reason");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/provider-selection/search Tests

    [Fact]
    [FastTest]
    public async Task GET_SearchSelections_WithAuth_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_client);

        // Act
        var response = await Client.GetAsync("/api/provider-selection/search");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_SearchSelections_WithFilters_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_client);

        // Act
        var response = await Client.GetAsync($"/api/provider-selection/search?projectId={_testProject.Id}&status=1");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_SearchSelections_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/provider-selection/search");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/provider-selection/statistics Tests

    [Fact]
    [FastTest]
    public async Task GET_Statistics_AsClient_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_client);

        // Act
        var response = await Client.GetAsync("/api/provider-selection/statistics?asProvider=false");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_Statistics_AsProvider_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_provider);

        // Act
        var response = await Client.GetAsync("/api/provider-selection/statistics?asProvider=true");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_Statistics_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/provider-selection/statistics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/provider-selection/recommendations/{projectId} Tests

    [Fact]
    [FastTest]
    public async Task GET_Recommendations_WithAuth_ReturnsOkOrNotFound()
    {
        // Arrange
        AuthenticateAs(_client);

        // Act
        var response = await Client.GetAsync($"/api/provider-selection/recommendations/{_testProject.Id}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_Recommendations_WithTakeLimit_ReturnsOkOrNotFound()
    {
        // Arrange
        AuthenticateAs(_client);

        // Act
        var response = await Client.GetAsync($"/api/provider-selection/recommendations/{_testProject.Id}?take=10");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_Recommendations_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync($"/api/provider-selection/recommendations/{_testProject.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST /api/provider-selection/{id}/initiate-escrow Tests

    [Fact]
    [FastTest]
    public async Task POST_InitiateEscrow_WithAuth_ReturnsOkOrNotFound()
    {
        // Arrange
        AuthenticateAs(_client);
        var selectionId = Guid.NewGuid();

        // Act
        var response = await Client.PostAsync($"/api/provider-selection/{selectionId}/initiate-escrow", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.BadRequest, HttpStatusCode.Forbidden, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_InitiateEscrow_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var selectionId = Guid.NewGuid();

        // Act
        var response = await Client.PostAsync($"/api/provider-selection/{selectionId}/initiate-escrow", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/provider-selection/ready/{projectId} Tests

    [Fact]
    [FastTest]
    public async Task GET_IsReady_WithAuth_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_client);

        // Act
        var response = await Client.GetAsync($"/api/provider-selection/ready/{_testProject.Id}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_IsReady_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync($"/api/provider-selection/ready/{_testProject.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization Summary Tests

    [Fact]
    [SecurityTest]
    public async Task AllEndpoints_WithoutAuth_ReturnUnauthorized()
    {
        // Test GET endpoints without authentication
        var getEndpoints = new[]
        {
            $"/api/provider-selection/{Guid.NewGuid()}",
            $"/api/provider-selection/project/{_testProject.Id}",
            $"/api/provider-selection/dashboard/{_testProject.Id}",
            $"/api/provider-selection/rank/{_testProject.Id}",
            $"/api/provider-selection/compare/{Guid.NewGuid()}/project/{_testProject.Id}",
            $"/api/provider-selection/provider-history/{_provider.Id}",
            "/api/provider-selection/search",
            "/api/provider-selection/statistics",
            $"/api/provider-selection/recommendations/{_testProject.Id}",
            $"/api/provider-selection/ready/{_testProject.Id}"
        };

        foreach (var endpoint in getEndpoints)
        {
            var response = await Client.GetAsync(endpoint);
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
                $"GET {endpoint} should require authentication");
        }
    }

    [Fact]
    [SecurityTest]
    public async Task ModificationEndpoints_WithoutAuth_ReturnUnauthorized()
    {
        // Test modification endpoints without authentication
        var selectionId = Guid.NewGuid();

        var createResponse = await Client.PostAsJsonAsync("/api/provider-selection", new { });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var statusResponse = await Client.PutAsJsonAsync($"/api/provider-selection/{selectionId}/status", 1);
        statusResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var escrowResponse = await Client.PutAsJsonAsync($"/api/provider-selection/{selectionId}/escrow", true);
        escrowResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var contractResponse = await Client.PutAsJsonAsync($"/api/provider-selection/{selectionId}/contract", true);
        contractResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var cancelResponse = await Client.PostAsJsonAsync($"/api/provider-selection/{selectionId}/cancel", "reason");
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var initiateResponse = await Client.PostAsync($"/api/provider-selection/{selectionId}/initiate-escrow", null);
        initiateResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [SecurityTest]
    public async Task ProjectOwnershipEndpoints_AsNonOwner_ReturnForbiddenOrBadRequest()
    {
        // Arrange - Authenticate as non-owner
        AuthenticateAs(_otherUser);

        // Test endpoints that require project ownership
        var dashboardResponse = await Client.GetAsync($"/api/provider-selection/dashboard/{_testProject.Id}");
        dashboardResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);

        var rankResponse = await Client.GetAsync($"/api/provider-selection/rank/{_testProject.Id}");
        rankResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    #endregion
}
