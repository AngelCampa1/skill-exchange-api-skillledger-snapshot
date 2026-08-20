using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SkillLedger.Tests.Integration;

/// <summary>
/// Integration tests for Provider Selection API endpoints
/// Following TDD methodology with focus on critical selection flows
/// </summary>
[IntegrationTest]
[ApiTest]
[Collection("Integration Other")]
public class ProviderSelectionApiIntegrationTests : IntegrationTestBase
{
    private User _testClient = null!;
    private User _testProvider1 = null!;
    private User _testProvider2 = null!;
    private Project _testProject = null!;
    private ProjectApplication _testApplication1 = null!;
    private ProjectApplication _testApplication2 = null!;

    public ProviderSelectionApiIntegrationTests(SharedTestHostFixture fixture) : base(fixture)
    {
    }

    protected override async Task OnInitializeAsync()
    {
        await base.OnInitializeAsync();
        await SetupTestDataAsync();
    }

    private async Task SetupTestDataAsync()
    {
        // Create test client user
        _testClient = new User
        {
            Id = Guid.NewGuid(),
            Email = "client@example.com",
            UserName = "client@example.com",
            Status = UserStatus.PhoneVerified
        };
        Context.Users.Add(_testClient);

        // Create test provider users
        _testProvider1 = new User
        {
            Id = Guid.NewGuid(),
            Email = "provider1@example.com",
            UserName = "provider1@example.com",
            Status = UserStatus.PhoneVerified
        };
        Context.Users.Add(_testProvider1);

        _testProvider2 = new User
        {
            Id = Guid.NewGuid(),
            Email = "provider2@example.com",
            UserName = "provider2@example.com",
            Status = UserStatus.PhoneVerified
        };
        Context.Users.Add(_testProvider2);

        // Create test skill
        var testSkill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "C# Development",
            Category = "Programming",
            IsActive = true,
            IsSystemManaged = true
        };
        Context.Skills.Add(testSkill);

        // Create test project
        _testProject = new Project
        {
            Id = Guid.NewGuid(),
            ClientId = _testClient.Id,
            Title = "Test Project for Selection",
            Description = "Integration test project",
            Status = ProjectStatus.Published,
            CreditBudget = 1000,
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(30),
            ModerationStatus = ModerationStatus.Approved
        };
        Context.Projects.Add(_testProject);

        // Create project skill requirement
        var projectSkill = new ProjectSkill
        {
            ProjectId = _testProject.Id,
            SkillId = testSkill.Id,
            ProficiencyRequired = SkillProficiency.Advanced,
            Weight = 4
        };
        Context.ProjectSkills.Add(projectSkill);

        // Create test applications
        _testApplication1 = new ProjectApplication
        {
            Id = Guid.NewGuid(),
            ProjectId = _testProject.Id,
            ProviderId = _testProvider1.Id,
            CoverLetter = "Experienced developer with strong C# skills.",
            ProposedTimeline = 20,
            IsAvailableImmediately = true,
            ProposedBudget = 900,
            Status = ApplicationStatus.Pending,
            SkillMatchScore = 0.85m
        };
        Context.ProjectApplications.Add(_testApplication1);

        _testApplication2 = new ProjectApplication
        {
            Id = Guid.NewGuid(),
            ProjectId = _testProject.Id,
            ProviderId = _testProvider2.Id,
            CoverLetter = "Motivated developer ready to take on challenges.",
            ProposedTimeline = 25,
            IsAvailableImmediately = false,
            ProposedBudget = 950,
            Status = ApplicationStatus.Pending,
            SkillMatchScore = 0.70m
        };
        Context.ProjectApplications.Add(_testApplication2);

        await Context.SaveChangesAsync();
    }


    #region TDD Tests for POST /api/provider-selection (CreateProviderSelection)

    [Fact]
    [SlowTest]
    public async Task CreateProviderSelection_WithValidRequest_ShouldReturn201Created()
    {
        // Arrange
        AuthenticateAs(_testClient);

        var createDto = new CreateProviderSelectionDto
        {
            ProjectId = _testProject.Id,
            SelectedProviderId = _testProvider1.Id,
            SelectedApplicationId = _testApplication1.Id,
            SelectionReason = "Excellent technical skills and competitive pricing. Provider demonstrates strong C# experience and is available to start immediately.",
            EscrowAmount = 900,
            ExpectedStartDate = DateTime.UtcNow.AddDays(7),
            ExpectedCompletionDate = DateTime.UtcNow.AddDays(27),
            ContractTerms = "Standard project terms with milestone-based delivery.",
            NegotiationNotes = "Agreed on weekly progress updates."
        };

        var json = JsonSerializer.Serialize(createDto);
        Console.WriteLine($"Serialized JSON: {json}");
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Get real CSRF token from server
        var csrfToken = await GetCsrfTokenAsync();
        content.Headers.Add("X-CSRF-TOKEN", csrfToken);

        // Act
        var response = await Client.PostAsync("/api/provider-selection", content);

        // Debug: Log response details if not Created
        if (response.StatusCode != HttpStatusCode.Created)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Expected: Created, Actual: {response.StatusCode}");
            Console.WriteLine($"Error Response: {errorContent}");
            Console.WriteLine($"Test Data - ProjectId: {_testProject.Id}");
            Console.WriteLine($"Test Data - ProviderID: {_testProvider1.Id}");
            Console.WriteLine($"Test Data - ApplicationId: {_testApplication1.Id}");
            Console.WriteLine($"Response Headers: {string.Join(", ", response.Headers.Select(h => $"{h.Key}: {string.Join(";", h.Value)}"))}");

            // Check if there's already an existing selection in the database
            var existingSelection = Context.ProviderSelections.FirstOrDefault(ps => ps.ProjectId == _testProject.Id);
            Console.WriteLine($"Existing selection found: {existingSelection != null}");
        }

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var responseData = JsonSerializer.Deserialize<object>(responseContent);
        Assert.NotNull(responseData);

        // Verify Location header is set
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    [SecurityTest]
    public async Task CreateProviderSelection_WithoutAuthentication_ShouldReturn401Unauthorized()
    {
        // Arrange
        var createDto = new CreateProviderSelectionDto
        {
            ProjectId = _testProject.Id,
            SelectedProviderId = _testProvider1.Id,
            SelectedApplicationId = _testApplication1.Id,
            SelectionReason = "Test reason for selection",
            EscrowAmount = 900
        };

        var json = JsonSerializer.Serialize(createDto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/provider-selection", content);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    [FastTest]
    public async Task CreateProviderSelection_WithInvalidData_ShouldReturn400BadRequest()
    {
        // Arrange
        AuthenticateAs(_testClient);

        var invalidDto = new CreateProviderSelectionDto
        {
            ProjectId = _testProject.Id,
            SelectedProviderId = _testProvider1.Id,
            SelectedApplicationId = _testApplication1.Id,
            SelectionReason = "Too short", // Below minimum length requirement
            EscrowAmount = 50000 // Exceeds maximum
        };

        var json = JsonSerializer.Serialize(invalidDto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        AddCsrfTokenHeader();

        // Act
        var response = await Client.PostAsync("/api/provider-selection", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [SecurityTest]
    public async Task CreateProviderSelection_WithUnauthorizedUser_ShouldReturn400BadRequest()
    {
        // Arrange
        AuthenticateAs(_testProvider1); // Provider trying to select themselves

        var createDto = new CreateProviderSelectionDto
        {
            ProjectId = _testProject.Id,
            SelectedProviderId = _testProvider1.Id,
            SelectedApplicationId = _testApplication1.Id,
            SelectionReason = "Self-selecting is not allowed in the system",
            EscrowAmount = 900
        };

        var json = JsonSerializer.Serialize(createDto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        AddCsrfTokenHeader();

        // Act
        var response = await Client.PostAsync("/api/provider-selection", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region TDD Tests for GET /api/provider-selection/dashboard/{projectId}

    [Fact]
    [FastTest]
    public async Task GetSelectionDashboard_WithValidProject_ShouldReturn200OK()
    {
        // Arrange
        AuthenticateAs(_testClient);

        // Act
        var response = await Client.GetAsync($"/api/provider-selection/dashboard/{_testProject.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var dashboard = JsonSerializer.Deserialize<SelectionDashboardDto>(responseContent, JsonOptions);

        Assert.NotNull(dashboard);
        Assert.Equal(_testProject.Id, dashboard.Project.Id);
        Assert.Equal(2, dashboard.RankedApplications.Count);
        Assert.False(dashboard.IsSelectionMade);
    }

    [Fact]
    [SecurityTest]
    public async Task GetSelectionDashboard_WithUnauthorizedUser_ShouldReturn403Forbidden()
    {
        // Arrange
        AuthenticateAs(_testProvider1); // Provider trying to access client's dashboard

        // Act
        var response = await Client.GetAsync($"/api/provider-selection/dashboard/{_testProject.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    [FastTest]
    public async Task GetSelectionDashboard_WithNonExistentProject_ShouldReturn403Forbidden()
    {
        // Arrange
        AuthenticateAs(_testClient);
        var nonExistentProjectId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/provider-selection/dashboard/{nonExistentProjectId}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region TDD Tests for GET /api/provider-selection/rank/{projectId}

    [Fact]
    [FastTest]
    public async Task RankApplications_WithValidProject_ShouldReturn200OK()
    {
        // Arrange
        AuthenticateAs(_testClient);

        // Act
        var response = await Client.GetAsync($"/api/provider-selection/rank/{_testProject.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var applications = JsonSerializer.Deserialize<List<ApplicationComparisonDto>>(responseContent, JsonOptions);

        Assert.NotNull(applications);
        Assert.Equal(2, applications.Count);

        // Verify applications are ranked (first should have higher or equal score)
        Assert.True(applications[0].RankingScore >= applications[1].RankingScore);
    }

    [Fact]
    [SecurityTest]
    public async Task RankApplications_WithUnauthorizedUser_ShouldReturn403Forbidden()
    {
        // Arrange
        AuthenticateAs(_testProvider1);

        // Act
        var response = await Client.GetAsync($"/api/provider-selection/rank/{_testProject.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region TDD Tests for GET /api/provider-selection/compare/{applicationId}/project/{projectId}

    [Fact]
    [FastTest]
    public async Task GetApplicationComparison_WithValidApplication_ShouldReturn200OK()
    {
        // Arrange
        AuthenticateAs(_testClient);

        // Act
        var response = await Client.GetAsync($"/api/provider-selection/compare/{_testApplication1.Id}/project/{_testProject.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var comparison = JsonSerializer.Deserialize<ApplicationComparisonDto>(responseContent, JsonOptions);

        Assert.NotNull(comparison);
        Assert.Equal(_testApplication1.Id, comparison.Application.Id);
        Assert.True(comparison.RankingScore > 0);
        Assert.True(comparison.SkillMatchPercentage > 0);
        Assert.NotEmpty(comparison.Strengths);
    }

    [Fact]
    [FastTest]
    public async Task GetApplicationComparison_WithNonExistentApplication_ShouldReturn404NotFound()
    {
        // Arrange
        AuthenticateAs(_testClient);
        var nonExistentAppId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/provider-selection/compare/{nonExistentAppId}/project/{_testProject.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region TDD Tests for GET /api/provider-selection/provider-history/{providerId}

    [Fact]
    [FastTest]
    public async Task GetProviderHistory_WithValidProvider_ShouldReturn200OK()
    {
        // Arrange
        AuthenticateAs(_testClient);

        // Act
        var response = await Client.GetAsync($"/api/provider-selection/provider-history/{_testProvider1.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var history = JsonSerializer.Deserialize<ProviderHistorySummaryDto>(responseContent, JsonOptions);

        Assert.NotNull(history);
        Assert.True(history.AverageRating > 0);
        Assert.True(history.OnTimeDeliveryRate >= 0);
        Assert.Equal(_testProvider1.CreatedAt, history.MemberSince);
    }

    [Fact]
    [FastTest]
    public async Task GetProviderHistory_WithNonExistentProvider_ShouldReturn404NotFound()
    {
        // Arrange
        AuthenticateAs(_testClient);
        var nonExistentProviderId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/provider-selection/provider-history/{nonExistentProviderId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region TDD Tests for GET /api/provider-selection/{id}

    [Fact]
    [FastTest]
    public async Task GetProviderSelection_WithValidSelection_ShouldReturn200OK()
    {
        // Arrange
        AuthenticateAs(_testClient);

        // Create a selection first
        var selection = new ProviderSelection
        {
            Id = Guid.NewGuid(),
            ProjectId = _testProject.Id,
            SelectedProviderId = _testProvider1.Id,
            SelectedApplicationId = _testApplication1.Id,
            SelectionReason = "Best candidate for integration test",
            EscrowAmount = 900
        };
        Context.ProviderSelections.Add(selection);
        Context.SaveChanges();

        // Act
        var response = await Client.GetAsync($"/api/provider-selection/{selection.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var selectionDto = JsonSerializer.Deserialize<ProviderSelectionDto>(responseContent, JsonOptions);

        Assert.NotNull(selectionDto);
        Assert.Equal(selection.Id, selectionDto.Id);
        Assert.Equal(_testProject.Id, selectionDto.Project.Id);
    }

    [Fact]
    [SecurityTest]
    public async Task GetProviderSelection_WithUnauthorizedUser_ShouldReturn404NotFound()
    {
        // Arrange
        var unauthorizedUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "unauthorized@example.com",
            UserName = "unauthorized@example.com",
            Status = UserStatus.Active
        };
        Context.Users.Add(unauthorizedUser);

        var selection = new ProviderSelection
        {
            Id = Guid.NewGuid(),
            ProjectId = _testProject.Id,
            SelectedProviderId = _testProvider1.Id,
            SelectedApplicationId = _testApplication1.Id,
            SelectionReason = "Test selection",
            EscrowAmount = 900
        };
        Context.ProviderSelections.Add(selection);
        Context.SaveChanges();
        SimpleTestDataSeeder.CreateActiveSubscriptionForUser(Context, unauthorizedUser.Id);

        AuthenticateAs(unauthorizedUser);

        // Act
        var response = await Client.GetAsync($"/api/provider-selection/{selection.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region TDD Tests for GET /api/provider-selection/ready/{projectId}

    [Fact]
    [FastTest]
    public async Task IsProjectReadyForSelection_WithReadyProject_ShouldReturn200OK()
    {
        // Arrange
        AuthenticateAs(_testClient);

        // Act
        var response = await Client.GetAsync($"/api/provider-selection/ready/{_testProject.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<object>(responseContent);
        Assert.NotNull(result);
    }

    [Fact]
    [SecurityTest]
    public async Task IsProjectReadyForSelection_WithoutAuthentication_ShouldReturn401Unauthorized()
    {
        // Act
        var response = await Client.GetAsync($"/api/provider-selection/ready/{_testProject.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region TDD Tests for GET /api/provider-selection/recommendations/{projectId}

    [Fact]
    [FastTest]
    public async Task GetRecommendedProviders_WithValidProject_ShouldReturn200OK()
    {
        // Arrange
        AuthenticateAs(_testClient);

        // Act
        var response = await Client.GetAsync($"/api/provider-selection/recommendations/{_testProject.Id}?take=3");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var recommendations = JsonSerializer.Deserialize<List<ApplicationComparisonDto>>(responseContent, JsonOptions);

        Assert.NotNull(recommendations);
        Assert.True(recommendations.Count <= 3);

        // All recommendations should be at least good candidates
        Assert.All(recommendations, r =>
            Assert.True(r.RecommendationLevel >= RecommendationLevel.GoodCandidate));
    }

    [Fact]
    [FastTest]
    public async Task GetRecommendedProviders_WithNonExistentProject_ShouldReturn404NotFound()
    {
        // Arrange
        AuthenticateAs(_testClient);
        var nonExistentProjectId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/provider-selection/recommendations/{nonExistentProjectId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region TDD Tests for Rate Limiting

    [Fact]
    [SecurityTest]
    public async Task CreateProviderSelection_WithRateLimitingDisabled_ShouldAllowRequests()
    {
        // Arrange
        AuthenticateAs(_testClient);

        var createDto = new CreateProviderSelectionDto
        {
            ProjectId = _testProject.Id,
            SelectedProviderId = _testProvider1.Id,
            SelectedApplicationId = _testApplication1.Id,
            SelectionReason = "Test that rate limiting is properly disabled in test environment",
            EscrowAmount = 900
        };

        var json = JsonSerializer.Serialize(createDto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        AddCsrfTokenHeader();

        // Act - Make request that would normally be rate limited
        var response = await Client.PostAsync("/api/provider-selection", content);

        // Assert - Should NOT return 429 since rate limiting is disabled in test environment
        Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);

        // Should return a valid business response (Created, Conflict, BadRequest, etc.)
        Assert.True(
            response.StatusCode == HttpStatusCode.Created ||
            response.StatusCode == HttpStatusCode.Conflict ||
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.NotFound,
            $"Expected valid business response but got {response.StatusCode}"
        );

        response.Dispose();
    }

    #endregion

    private void AddCsrfTokenHeader()
    {
        // In a real application, you would retrieve the CSRF token from the server
        // For testing purposes, we'll add a mock token
        Client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", "test-csrf-token");
    }

    private static readonly JsonSerializerOptions JsonOptions = TestJsonOptions.Default;
}