using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Tests.Infrastructure;

namespace SkillLedger.Tests.Core.Services;

/// <summary>
/// TDD tests for ProviderSelectionService following Red-Green-Refactor methodology
/// </summary>
[UnitTest]
[CoreTest]
[Collection("Integration Financial")]
public class ProviderSelectionServiceTests : IntegrationTestBase
{
    private readonly IProviderSelectionService _service;
    private readonly IProjectApplicationService _applicationService;

    private User _testClient = null!;
    private User _testProvider1 = null!;
    private User _testProvider2 = null!;
    private Project _testProject = null!;
    private Skill _testSkill1 = null!;
    private ProjectApplication _testApplication1 = null!;
    private ProjectApplication _testApplication2 = null!;

    public ProviderSelectionServiceTests(SharedTestHostFixture fixture) : base(fixture)
    {
        _service = ServiceScope.ServiceProvider.GetRequiredService<IProviderSelectionService>();
        _applicationService = ServiceScope.ServiceProvider.GetRequiredService<IProjectApplicationService>();
    }

    protected override async Task OnInitializeAsync()
    {
        await base.OnInitializeAsync();

        // Create test client
        _testClient = new User
        {
            Id = Guid.NewGuid(),
            Email = "client@example.com",
            UserName = "client@example.com",
            NormalizedEmail = "CLIENT@EXAMPLE.COM",
            NormalizedUserName = "CLIENT@EXAMPLE.COM",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        Context.Users.Add(_testClient);

        // Create test providers
        _testProvider1 = new User
        {
            Id = Guid.NewGuid(),
            Email = "provider1@example.com",
            UserName = "provider1@example.com",
            NormalizedEmail = "PROVIDER1@EXAMPLE.COM",
            NormalizedUserName = "PROVIDER1@EXAMPLE.COM",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        Context.Users.Add(_testProvider1);

        _testProvider2 = new User
        {
            Id = Guid.NewGuid(),
            Email = "provider2@example.com",
            UserName = "provider2@example.com",
            NormalizedEmail = "PROVIDER2@EXAMPLE.COM",
            NormalizedUserName = "PROVIDER2@EXAMPLE.COM",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        Context.Users.Add(_testProvider2);

        // Create test skill
        _testSkill1 = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "C# Programming",
            Description = "Programming in C#",
            Category = "Programming",
            IsActive = true,
            IsSystemManaged = true
        };
        Context.Skills.Add(_testSkill1);

        // Create test project
        _testProject = new Project
        {
            Id = Guid.NewGuid(),
            ClientId = _testClient.Id,
            Title = "Test Project",
            Description = "A test project for provider selection",
            Status = ProjectStatus.Published,
            CreditBudget = 1000,
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(30),
            ModerationStatus = ModerationStatus.Approved,
            CreatedAt = DateTime.UtcNow
        };
        Context.Projects.Add(_testProject);

        // Create project skill requirements
        var projectSkill = new ProjectSkill
        {
            ProjectId = _testProject.Id,
            SkillId = _testSkill1.Id,
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
            CoverLetter = "I am an experienced C# developer with 5 years of experience.",
            ProposedTimeline = 20,
            IsAvailableImmediately = true,
            ProposedBudget = 900,
            Status = ApplicationStatus.Pending,
            SkillMatchScore = 0.85m,
            CreatedAt = DateTime.UtcNow
        };
        Context.ProjectApplications.Add(_testApplication1);

        _testApplication2 = new ProjectApplication
        {
            Id = Guid.NewGuid(),
            ProjectId = _testProject.Id,
            ProviderId = _testProvider2.Id,
            CoverLetter = "I have some experience with C# and am willing to learn more.",
            ProposedTimeline = 35,
            IsAvailableImmediately = false,
            ProposedBudget = 1100,
            Status = ApplicationStatus.Pending,
            SkillMatchScore = 0.60m,
            CreatedAt = DateTime.UtcNow
        };
        Context.ProjectApplications.Add(_testApplication2);

        await Context.SaveChangesAsync();
    }

    #region TDD Tests for CreateProviderSelectionAsync

    [Fact]
    public async Task CreateProviderSelectionAsync_WithValidData_ShouldCreateSelection()
    {
        // Arrange
        var createDto = new CreateProviderSelectionDto
        {
            ProjectId = _testProject.Id,
            SelectedProviderId = _testProvider1.Id,
            SelectedApplicationId = _testApplication1.Id,
            SelectionReason = "Best skill match and competitive timeline.",
            EscrowAmount = 900,
            ExpectedStartDate = DateTime.UtcNow.AddDays(7),
            ExpectedCompletionDate = DateTime.UtcNow.AddDays(27)
        };

        // Act
        var result = await _service.CreateProviderSelectionAsync(createDto, _testClient.Id, "127.0.0.1");

        // Assert
        Assert.True(result.Success);
        Assert.Contains("Provider selected successfully", result.Message);
        Assert.NotNull(result.Data);

        // Verify selection was created in database
        var selection = await Context.ProviderSelections
            .FirstOrDefaultAsync(ps => ps.Id == (Guid)result.Data);
        Assert.NotNull(selection);
        Assert.Equal(_testProject.Id, selection.ProjectId);
        Assert.Equal(_testProvider1.Id, selection.SelectedProviderId);
        Assert.Equal(_testApplication1.Id, selection.SelectedApplicationId);
        Assert.Equal(ProviderSelectionStatus.Selected, selection.Status);
    }

    [Fact]
    public async Task CreateProviderSelectionAsync_WithNonExistentProject_ShouldFail()
    {
        // Arrange
        var createDto = new CreateProviderSelectionDto
        {
            ProjectId = Guid.NewGuid(),
            SelectedProviderId = _testProvider1.Id,
            SelectedApplicationId = _testApplication1.Id,
            SelectionReason = "Test reason",
            EscrowAmount = 900
        };

        // Act
        var result = await _service.CreateProviderSelectionAsync(createDto, _testClient.Id, "127.0.0.1");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Project not found", result.Message);
    }

    [Fact]
    public async Task CreateProviderSelectionAsync_WithUnauthorizedUser_ShouldFail()
    {
        // Arrange
        var unauthorizedUserId = Guid.NewGuid();
        var createDto = new CreateProviderSelectionDto
        {
            ProjectId = _testProject.Id,
            SelectedProviderId = _testProvider1.Id,
            SelectedApplicationId = _testApplication1.Id,
            SelectionReason = "Test reason",
            EscrowAmount = 900
        };

        // Act
        var result = await _service.CreateProviderSelectionAsync(createDto, unauthorizedUserId, "127.0.0.1");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("permission", result.Message);
    }

    [Fact]
    public async Task CreateProviderSelectionAsync_WithExistingSelection_ShouldFail()
    {
        // Arrange - Create first selection
        var existingSelection = new ProviderSelection
        {
            Id = Guid.NewGuid(),
            ProjectId = _testProject.Id,
            SelectedProviderId = _testProvider1.Id,
            SelectedApplicationId = _testApplication1.Id,
            SelectionReason = "Already selected",
            EscrowAmount = 500
        };
        Context.ProviderSelections.Add(existingSelection);
        Context.SaveChanges();

        var createDto = new CreateProviderSelectionDto
        {
            ProjectId = _testProject.Id,
            SelectedProviderId = _testProvider2.Id,
            SelectedApplicationId = _testApplication2.Id,
            SelectionReason = "Second attempt",
            EscrowAmount = 600
        };

        // Act
        var result = await _service.CreateProviderSelectionAsync(createDto, _testClient.Id, "127.0.0.1");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("already been selected", result.Message);
    }

    [Fact]
    public async Task CreateProviderSelectionAsync_WithExcessiveEscrowAmount_ShouldFail()
    {
        // Arrange
        var createDto = new CreateProviderSelectionDto
        {
            ProjectId = _testProject.Id,
            SelectedProviderId = _testProvider1.Id,
            SelectedApplicationId = _testApplication1.Id,
            SelectionReason = "Test reason",
            EscrowAmount = _testProject.CreditBudget + 100 // Exceeds budget
        };

        // Act
        var result = await _service.CreateProviderSelectionAsync(createDto, _testClient.Id, "127.0.0.1");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("cannot exceed project budget", result.Message);
    }

    [Fact]
    public async Task CreateProviderSelectionAsync_ShouldUpdateApplicationStatuses()
    {
        // Arrange
        var createDto = new CreateProviderSelectionDto
        {
            ProjectId = _testProject.Id,
            SelectedProviderId = _testProvider1.Id,
            SelectedApplicationId = _testApplication1.Id,
            SelectionReason = "Best candidate",
            EscrowAmount = 900
        };

        // Act
        var result = await _service.CreateProviderSelectionAsync(createDto, _testClient.Id, "127.0.0.1");

        // Assert
        Assert.True(result.Success);

        // Use a new scope to avoid concurrency issues with the service's context
        using var verificationScope = Factory.Services.CreateScope();
        using var verificationContext = verificationScope.ServiceProvider.GetRequiredService<SkillLedgerDbContext>();

        // Verify selected application is accepted
        var selectedApp = await verificationContext.ProjectApplications
            .FirstAsync(pa => pa.Id == _testApplication1.Id);
        Assert.Equal(ApplicationStatus.Accepted, selectedApp.Status);
        Assert.NotNull(selectedApp.ReviewedAt);

        // Verify other application is rejected
        var rejectedApp = await verificationContext.ProjectApplications
            .FirstAsync(pa => pa.Id == _testApplication2.Id);
        Assert.Equal(ApplicationStatus.Rejected, rejectedApp.Status);
        Assert.NotNull(rejectedApp.ReviewedAt);
        Assert.Contains("Another provider was selected", rejectedApp.ClientFeedback);
    }

    #endregion

    #region TDD Tests for GetSelectionDashboardAsync

    [Fact]
    public async Task GetSelectionDashboardAsync_WithValidProject_ShouldReturnDashboard()
    {
        // Arrange - no existing selection

        // Act
        var dashboard = await _service.GetSelectionDashboardAsync(_testProject.Id, _testClient.Id);

        // Assert
        Assert.NotNull(dashboard);
        Assert.Equal(_testProject.Id, dashboard.Project.Id);
        Assert.Equal(2, dashboard.RankedApplications.Count);
        Assert.False(dashboard.IsSelectionMade);
        Assert.Null(dashboard.CurrentSelection);

        // Verify applications are ranked by score
        var firstApp = dashboard.RankedApplications.First();
        var secondApp = dashboard.RankedApplications.Last();
        Assert.True(firstApp.RankingScore >= secondApp.RankingScore);
    }

    [Fact]
    public async Task GetSelectionDashboardAsync_WithExistingSelection_ShouldShowSelection()
    {
        // Arrange
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

        // Act
        var dashboard = await _service.GetSelectionDashboardAsync(_testProject.Id, _testClient.Id);

        // Assert
        Assert.NotNull(dashboard);
        Assert.True(dashboard.IsSelectionMade);
        Assert.NotNull(dashboard.CurrentSelection);
        Assert.Equal(selection.Id, dashboard.CurrentSelection.Id);
    }

    [Fact]
    public async Task GetSelectionDashboardAsync_WithUnauthorizedUser_ShouldThrowException()
    {
        // Arrange
        var unauthorizedUserId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.GetSelectionDashboardAsync(_testProject.Id, unauthorizedUserId));
    }

    #endregion

    #region TDD Tests for RankApplicationsAsync

    [Fact]
    public async Task RankApplicationsAsync_ShouldRankByMultipleFactors()
    {
        // Act
        var rankedApplications = await _service.RankApplicationsAsync(_testProject.Id, _testClient.Id);

        // Assert
        Assert.Equal(2, rankedApplications.Count);

        var topApplication = rankedApplications.First();
        var secondApplication = rankedApplications.Last();

        // First application should rank higher (better skill match, immediate availability, under budget)
        Assert.True(topApplication.RankingScore >= secondApplication.RankingScore);
        Assert.Equal(_testApplication1.Id, topApplication.Application.Id);
        Assert.Equal(_testApplication2.Id, secondApplication.Application.Id);
    }

    [Fact]
    public async Task RankApplicationsAsync_ShouldCalculateSkillMatchCorrectly()
    {
        // Act
        var rankedApplications = await _service.RankApplicationsAsync(_testProject.Id, _testClient.Id);

        // Assert
        var app1 = rankedApplications.First(ra => ra.Application.Id == _testApplication1.Id);
        var app2 = rankedApplications.First(ra => ra.Application.Id == _testApplication2.Id);

        // App1 has better skill match score
        Assert.True(app1.SkillMatchPercentage > app2.SkillMatchPercentage);
        Assert.Equal(85.0m, app1.SkillMatchPercentage); // Based on stored skill match score
        Assert.Equal(60.0m, app2.SkillMatchPercentage);
    }

    [Fact]
    public async Task RankApplicationsAsync_ShouldIncludeRecommendationLevel()
    {
        // Act
        var rankedApplications = await _service.RankApplicationsAsync(_testProject.Id, _testClient.Id);

        // Assert
        var topApplication = rankedApplications.First();
        Assert.True(topApplication.RecommendationLevel >= RecommendationLevel.GoodCandidate);

        var lowerApplication = rankedApplications.Last();
        Assert.True(lowerApplication.RecommendationLevel >= RecommendationLevel.ConsiderWithCaution);
    }

    #endregion

    #region TDD Tests for CalculateApplicationRankingAsync

    [Fact]
    public async Task CalculateApplicationRankingAsync_ShouldReturnDetailedComparison()
    {
        // Act
        var comparison = await _service.CalculateApplicationRankingAsync(_testApplication1.Id, _testProject.Id);

        // Assert
        Assert.NotNull(comparison);
        Assert.Equal(_testApplication1.Id, comparison.Application.Id);
        Assert.True(comparison.RankingScore > 0);
        Assert.True(comparison.SkillMatchPercentage > 0);
        Assert.True(comparison.ReputationScore > 0);
        Assert.NotEmpty(comparison.Strengths);
    }

    [Fact]
    public async Task CalculateApplicationRankingAsync_WithNonExistentApplication_ShouldThrowException()
    {
        // Arrange
        var nonExistentAppId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CalculateApplicationRankingAsync(nonExistentAppId, _testProject.Id));
    }

    #endregion

    #region TDD Tests for GetProviderHistorySummaryAsync

    [Fact]
    public async Task GetProviderHistorySummaryAsync_ShouldReturnHistory()
    {
        // Act
        var history = await _service.GetProviderHistorySummaryAsync(_testProvider1.Id);

        // Assert
        Assert.NotNull(history);
        Assert.True(history.AverageRating > 0);
        Assert.True(history.OnTimeDeliveryRate >= 0);
        Assert.True(history.ClientSatisfactionScore > 0);
        Assert.Equal(_testProvider1.CreatedAt, history.MemberSince);
    }

    [Fact]
    public async Task GetProviderHistorySummaryAsync_WithNonExistentProvider_ShouldThrowException()
    {
        // Arrange
        var nonExistentProviderId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetProviderHistorySummaryAsync(nonExistentProviderId));
    }

    #endregion

    #region TDD Tests for GetProviderSelectionByIdAsync

    [Fact]
    public async Task GetProviderSelectionByIdAsync_WithValidSelection_ShouldReturnSelection()
    {
        // Arrange
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

        // Act
        var result = await _service.GetProviderSelectionByIdAsync(selection.Id, _testClient.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(selection.Id, result.Id);
        Assert.Equal(_testProject.Id, result.Project.Id);
        Assert.Equal(_testProvider1.Id, result.SelectedProvider.Id);
    }

    [Fact]
    public async Task GetProviderSelectionByIdAsync_WithUnauthorizedUser_ShouldReturnNull()
    {
        // Arrange
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

        var unauthorizedUserId = Guid.NewGuid();

        // Act
        var result = await _service.GetProviderSelectionByIdAsync(selection.Id, unauthorizedUserId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetProviderSelectionByIdAsync_WithNonExistentSelection_ShouldReturnNull()
    {
        // Arrange
        var nonExistentSelectionId = Guid.NewGuid();

        // Act
        var result = await _service.GetProviderSelectionByIdAsync(nonExistentSelectionId, _testClient.Id);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region TDD Tests for IsProjectReadyForSelectionAsync

    [Fact]
    public async Task IsProjectReadyForSelectionAsync_WithApplicationsAndPublishedProject_ShouldReturnTrue()
    {
        // Act
        var isReady = await _service.IsProjectReadyForSelectionAsync(_testProject.Id);

        // Assert
        Assert.True(isReady);
    }

    [Fact]
    public async Task IsProjectReadyForSelectionAsync_WithNoApplications_ShouldReturnFalse()
    {
        // Arrange - Create project without applications
        var projectWithoutApps = new Project
        {
            Id = Guid.NewGuid(),
            ClientId = _testClient.Id,
            Title = "Project Without Apps",
            Description = "No applications",
            Status = ProjectStatus.Published,
            CreditBudget = 500
        };
        Context.Projects.Add(projectWithoutApps);
        Context.SaveChanges();

        // Act
        var isReady = await _service.IsProjectReadyForSelectionAsync(projectWithoutApps.Id);

        // Assert
        Assert.False(isReady);
    }

    [Fact]
    public async Task IsProjectReadyForSelectionAsync_WithDraftProject_ShouldReturnFalse()
    {
        // Arrange - Set project to draft status
        _testProject.Status = ProjectStatus.Draft;
        Context.SaveChanges();

        // Act
        var isReady = await _service.IsProjectReadyForSelectionAsync(_testProject.Id);

        // Assert
        Assert.False(isReady);
    }

    #endregion

    #region TDD Tests for SendSelectionNotificationsAsync

    [Fact]
    public async Task SendSelectionNotificationsAsync_WithValidSelection_ShouldSendNotifications()
    {
        // Arrange
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

        // Mark other applications as rejected (as would happen in real scenario)
        _testApplication2.Status = ApplicationStatus.Rejected;
        Context.SaveChanges();

        // Act
        var result = await _service.SendSelectionNotificationsAsync(selection.Id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task SendSelectionNotificationsAsync_WithNonExistentSelection_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentSelectionId = Guid.NewGuid();

        // Act
        var result = await _service.SendSelectionNotificationsAsync(nonExistentSelectionId);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region TDD Tests for GetRecommendedProvidersAsync

    [Fact]
    public async Task GetRecommendedProvidersAsync_ShouldReturnTopCandidates()
    {
        // Act
        var recommendations = await _service.GetRecommendedProvidersAsync(_testProject.Id, 5);

        // Assert
        Assert.NotEmpty(recommendations);
        Assert.True(recommendations.Count <= 5);

        // All recommendations should be at least good candidates
        Assert.All(recommendations, r =>
            Assert.True(r.RecommendationLevel >= RecommendationLevel.GoodCandidate));

        // Should be ordered by ranking score
        for (int i = 0; i < recommendations.Count - 1; i++)
        {
            Assert.True(recommendations[i].RankingScore >= recommendations[i + 1].RankingScore);
        }
    }

    [Fact]
    public async Task GetRecommendedProvidersAsync_WithNonExistentProject_ShouldReturnEmptyList()
    {
        // Arrange
        var nonExistentProjectId = Guid.NewGuid();

        // Act
        var recommendations = await _service.GetRecommendedProvidersAsync(nonExistentProjectId, 5);

        // Assert
        Assert.Empty(recommendations);
    }

    #endregion

    public override void Dispose()
    {
        try
        {
            // Clean up test data - handle concurrency issues gracefully
            if (Context.Database.ProviderName?.Contains("InMemory") == true)
            {
                // For InMemory database, just dispose - data is automatically cleaned up
                base.Dispose();
                return;
            }

            Context.ProviderSelections.RemoveRange(Context.ProviderSelections);
            Context.ProjectApplications.RemoveRange(Context.ProjectApplications);
            Context.ProjectSkills.RemoveRange(Context.ProjectSkills);
            Context.Projects.RemoveRange(Context.Projects);
            Context.Skills.RemoveRange(Context.Skills);
            Context.Users.RemoveRange(Context.Users);
            Context.SaveChanges();
        }
        catch (InvalidOperationException)
        {
            // Handle concurrent access gracefully - test cleanup shouldn't fail tests
        }

        base.Dispose();
    }
}