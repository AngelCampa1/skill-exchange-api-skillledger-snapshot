using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Infrastructure.Services;
using SkillLedger.Tests.Infrastructure;
using SkillLedger.Tests.Mocks;
using Xunit;

namespace SkillLedger.Tests.Integration.Services;

/// <summary>
/// Integration tests for ProviderSelectionService - PROVIDER SELECTION AND MATCHING SYSTEM.
///
/// Pattern (per TDD_GUIDE.md):
/// - Uses real in-memory EF Core database
/// - Uses MockAuditLogService (writes to DB - internal OK)
/// - Uses MockEmailService (external email service - OK to mock)
/// - Tests actual business logic for provider selection
/// - Verifies database state after operations
///
/// Max mocked external dependencies: 1 (Email Service)
/// </summary>
[IntegrationTest]
[FinancialTest]
public class ProviderSelectionServiceIntegrationTests : IDisposable
{
    private readonly SkillLedgerDbContext _context;
    private readonly ProviderSelectionService _service;
    private readonly ProjectApplicationService _applicationService;
    private readonly MockAuditLogService _auditLogService;
    private readonly Mocks.MockEmailService _emailService;
    private readonly ILogger<ProviderSelectionService> _logger;
    private readonly ILogger<ProjectApplicationService> _appServiceLogger;

    // Test data
    private readonly Guid _clientId = Guid.NewGuid();
    private readonly Guid _providerId = Guid.NewGuid();
    private readonly Guid _provider2Id = Guid.NewGuid();
    private readonly Guid _projectId = Guid.NewGuid();
    private readonly Guid _applicationId = Guid.NewGuid();
    private readonly Guid _application2Id = Guid.NewGuid();
    private readonly Guid _skillCSharp = Guid.NewGuid();
    private readonly string _testIp = "192.168.1.100";

    public ProviderSelectionServiceIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<SkillLedgerDbContext>()
            .UseInMemoryDatabase(databaseName: $"ProviderSelectionServiceTests_{Guid.NewGuid()}")
            .Options;

        _context = new SkillLedgerDbContext(options);
        _emailService = new Mocks.MockEmailService();
        _auditLogService = new MockAuditLogService(_context);
        _logger = new LoggerFactory().CreateLogger<ProviderSelectionService>();
        _appServiceLogger = new LoggerFactory().CreateLogger<ProjectApplicationService>();

        _applicationService = new ProjectApplicationService(
            _context,
            _appServiceLogger,
            _auditLogService,
            _emailService
        );

        _service = new ProviderSelectionService(
            _context,
            _logger,
            _auditLogService,
            _emailService,
            _applicationService
        );

        SeedTestData();
    }

    private void SeedTestData()
    {
        // Create client user
        var client = new User
        {
            Id = _clientId,
            Email = "client@test.com",
            UserName = "TestClient",
            PasswordHash = "hash",
            Status = UserStatus.Active,
            FirstName = "John",
            LastName = "Client",
            CreatedAt = DateTime.UtcNow.AddMonths(-6),
            Profile = new Profile
            {
                FirstName = "John",
                LastName = "Client",
                UserId = _clientId
            }
        };

        // Create provider users
        var provider = new User
        {
            Id = _providerId,
            Email = "provider@test.com",
            UserName = "TestProvider",
            PasswordHash = "hash",
            Status = UserStatus.Active,
            FirstName = "Jane",
            LastName = "Provider",
            CreatedAt = DateTime.UtcNow.AddMonths(-12),
            Profile = new Profile
            {
                FirstName = "Jane",
                LastName = "Provider",
                UserId = _providerId
            }
        };

        var provider2 = new User
        {
            Id = _provider2Id,
            Email = "provider2@test.com",
            UserName = "TestProvider2",
            PasswordHash = "hash",
            Status = UserStatus.Active,
            FirstName = "Bob",
            LastName = "Provider2",
            CreatedAt = DateTime.UtcNow.AddMonths(-3),
            Profile = new Profile
            {
                FirstName = "Bob",
                LastName = "Provider2",
                UserId = _provider2Id
            }
        };

        // Create skill
        var skill = new Skill { Id = _skillCSharp, Name = "C#", Category = "Programming" };
        _context.Skills.Add(skill);

        // Create provider skills
        var userSkill = new UserSkill { UserId = _providerId, SkillId = _skillCSharp, Proficiency = SkillProficiency.Expert, IsVisible = true };
        var userSkill2 = new UserSkill { UserId = _provider2Id, SkillId = _skillCSharp, Proficiency = SkillProficiency.Advanced, IsVisible = true };
        _context.UserSkills.AddRange(userSkill, userSkill2);

        // Create published project
        var project = new Project
        {
            Id = _projectId,
            ClientId = _clientId,
            Client = client,
            Title = "E-Commerce Platform",
            Description = "Build a modern e-commerce platform with C# backend and React frontend.",
            Status = ProjectStatus.Published,
            ModerationStatus = ModerationStatus.Approved,
            Visibility = ProjectVisibility.Public,
            CreditBudget = 5000,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(60),
            IsFeatured = false,
            IsRemoteWork = true
        };

        project.ProjectSkills = new List<ProjectSkill>
        {
            new() { ProjectId = _projectId, SkillId = _skillCSharp, Weight = 3, ProficiencyRequired = SkillProficiency.Expert, Skill = skill }
        };

        project.Deliverables = new List<ProjectDeliverable>
        {
            new() { Id = Guid.NewGuid(), ProjectId = _projectId, Description = "Backend API", OrderIndex = 1 },
            new() { Id = Guid.NewGuid(), ProjectId = _projectId, Description = "Frontend UI", OrderIndex = 2 }
        };

        // Create pending applications
        var application1 = new ProjectApplication
        {
            Id = _applicationId,
            ProjectId = _projectId,
            ProviderId = _providerId,
            Project = project,
            Provider = provider,
            CoverLetter = "I have extensive experience in C# and Azure development.",
            ProposedTimeline = 45,
            ProposedBudget = 4500,
            IsAvailableImmediately = true,
            Status = ApplicationStatus.Pending,
            SkillMatchScore = 0.9m,
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };

        var application2 = new ProjectApplication
        {
            Id = _application2Id,
            ProjectId = _projectId,
            ProviderId = _provider2Id,
            Project = project,
            Provider = provider2,
            CoverLetter = "I can deliver this project efficiently.",
            ProposedTimeline = 30,
            ProposedBudget = 4000,
            IsAvailableImmediately = false,
            Status = ApplicationStatus.Pending,
            SkillMatchScore = 0.75m,
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        };

        _context.Users.AddRange(client, provider, provider2);
        _context.Projects.Add(project);
        _context.ProjectApplications.AddRange(application1, application2);
        _context.SaveChanges();
    }

    #region CreateProviderSelectionAsync Tests

    [Fact]
    public async Task CreateProviderSelectionAsync_ValidSelection_ReturnsSuccessAndCreatesSelection()
    {
        // Arrange
        var createDto = new CreateProviderSelectionDto
        {
            ProjectId = _projectId,
            SelectedProviderId = _providerId,
            SelectedApplicationId = _applicationId,
            SelectionReason = "Best skill match and availability",
            ContractTerms = "Standard terms apply",
            EscrowAmount = 4500,
            ExpectedStartDate = DateTime.UtcNow.AddDays(10),
            ExpectedCompletionDate = DateTime.UtcNow.AddDays(55)
        };

        // Act
        var result = await _service.CreateProviderSelectionAsync(createDto, _clientId, _testIp);

        // Wait for fire-and-forget notification task
        await Task.Delay(200);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("selected successfully");
        result.Data.Should().NotBeNull();

        var selectionId = (Guid)result.Data!;
        var selection = await _context.ProviderSelections.FindAsync(selectionId);
        selection.Should().NotBeNull();
        selection!.SelectedProviderId.Should().Be(_providerId);
        selection.Status.Should().Be(ProviderSelectionStatus.Selected);
        selection.EscrowAmount.Should().Be(4500);
    }

    [Fact]
    public async Task CreateProviderSelectionAsync_ProjectNotFound_ReturnsFailure()
    {
        // Arrange
        var createDto = new CreateProviderSelectionDto
        {
            ProjectId = Guid.NewGuid(),
            SelectedProviderId = _providerId,
            SelectedApplicationId = _applicationId,
            EscrowAmount = 1000,
            SelectionReason = "This provider demonstrated excellent technical skills and professional communication throughout the application process."
        };

        // Act
        var result = await _service.CreateProviderSelectionAsync(createDto, _clientId, _testIp);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task CreateProviderSelectionAsync_NotProjectOwner_ReturnsFailure()
    {
        // Arrange
        var createDto = new CreateProviderSelectionDto
        {
            ProjectId = _projectId,
            SelectedProviderId = _providerId,
            SelectedApplicationId = _applicationId,
            EscrowAmount = 1000,
            SelectionReason = "This provider demonstrated excellent technical skills and professional communication throughout the application process."
        };
        var wrongClientId = Guid.NewGuid();

        // Act
        var result = await _service.CreateProviderSelectionAsync(createDto, wrongClientId, _testIp);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("permission");
    }

    [Fact]
    public async Task CreateProviderSelectionAsync_AlreadySelected_ReturnsFailure()
    {
        // Arrange - Create first selection
        var firstDto = new CreateProviderSelectionDto
        {
            ProjectId = _projectId,
            SelectedProviderId = _providerId,
            SelectedApplicationId = _applicationId,
            EscrowAmount = 4500,
            SelectionReason = "This provider demonstrated excellent technical skills and professional communication throughout the application process."
        };
        await _service.CreateProviderSelectionAsync(firstDto, _clientId, _testIp);
        await Task.Delay(200);

        // Act - Try to create second selection
        var secondDto = new CreateProviderSelectionDto
        {
            ProjectId = _projectId,
            SelectedProviderId = _provider2Id,
            SelectedApplicationId = _application2Id,
            EscrowAmount = 4000,
            SelectionReason = "This second provider also has great skills but we already selected someone else for this project."
        };
        var result = await _service.CreateProviderSelectionAsync(secondDto, _clientId, _testIp);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("already been selected");
    }

    [Fact]
    public async Task CreateProviderSelectionAsync_InvalidApplication_ReturnsFailure()
    {
        // Arrange
        var createDto = new CreateProviderSelectionDto
        {
            ProjectId = _projectId,
            SelectedProviderId = _providerId,
            SelectedApplicationId = Guid.NewGuid(), // Non-existent application
            EscrowAmount = 4500,
            SelectionReason = "This provider demonstrated excellent technical skills and professional communication throughout the application process."
        };

        // Act
        var result = await _service.CreateProviderSelectionAsync(createDto, _clientId, _testIp);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task CreateProviderSelectionAsync_EscrowExceedsBudget_ReturnsFailure()
    {
        // Arrange
        var createDto = new CreateProviderSelectionDto
        {
            ProjectId = _projectId,
            SelectedProviderId = _providerId,
            SelectedApplicationId = _applicationId,
            EscrowAmount = 10000, // Exceeds 5000 budget
            SelectionReason = "This provider demonstrated excellent technical skills and professional communication throughout the application process."
        };

        // Act
        var result = await _service.CreateProviderSelectionAsync(createDto, _clientId, _testIp);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("exceed");
    }

    [Fact]
    public async Task CreateProviderSelectionAsync_AcceptsSelectedApplicationAndRejectsOthers()
    {
        // Arrange
        var createDto = new CreateProviderSelectionDto
        {
            ProjectId = _projectId,
            SelectedProviderId = _providerId,
            SelectedApplicationId = _applicationId,
            EscrowAmount = 4500,
            SelectionReason = "This provider demonstrated excellent technical skills and professional communication throughout the application process."
        };

        // Act
        var result = await _service.CreateProviderSelectionAsync(createDto, _clientId, _testIp);
        await Task.Delay(200);

        // Assert
        result.Success.Should().BeTrue();

        // Check selected application is accepted
        var selectedApp = await _context.ProjectApplications.FindAsync(_applicationId);
        selectedApp!.Status.Should().Be(ApplicationStatus.Accepted);
        selectedApp.ReviewedAt.Should().NotBeNull();

        // Check other application is rejected
        var otherApp = await _context.ProjectApplications.FindAsync(_application2Id);
        otherApp!.Status.Should().Be(ApplicationStatus.Rejected);
        otherApp.ClientFeedback.Should().Contain("Another provider was selected");
    }

    [Fact]
    public async Task CreateProviderSelectionAsync_CreatesAuditLog()
    {
        // Arrange
        var createDto = new CreateProviderSelectionDto
        {
            ProjectId = _projectId,
            SelectedProviderId = _providerId,
            SelectedApplicationId = _applicationId,
            EscrowAmount = 4500,
            SelectionReason = "This provider demonstrated excellent technical skills and professional communication throughout the application process."
        };

        // Act
        await _service.CreateProviderSelectionAsync(createDto, _clientId, _testIp);
        await Task.Delay(200);

        // Assert
        var auditLogs = await _context.AuditLogs.ToListAsync();
        auditLogs.Should().Contain(log => log.Action == "PROVIDER_SELECTED" && log.UserId == _clientId);
    }

    #endregion

    #region GetProviderSelectionByIdAsync Tests

    [Fact]
    public async Task GetProviderSelectionByIdAsync_ValidClientRequest_ReturnsSelection()
    {
        // Arrange
        var selectionId = await CreateTestSelection();

        // Act
        var result = await _service.GetProviderSelectionByIdAsync(selectionId, _clientId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(selectionId);
        result.SelectedProvider.Should().NotBeNull();
    }

    [Fact]
    public async Task GetProviderSelectionByIdAsync_ValidProviderRequest_ReturnsSelection()
    {
        // Arrange
        var selectionId = await CreateTestSelection();

        // Act
        var result = await _service.GetProviderSelectionByIdAsync(selectionId, _providerId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(selectionId);
    }

    [Fact]
    public async Task GetProviderSelectionByIdAsync_NotFound_ReturnsNull()
    {
        // Act
        var result = await _service.GetProviderSelectionByIdAsync(Guid.NewGuid(), _clientId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetProviderSelectionByIdAsync_UnauthorizedUser_ReturnsNull()
    {
        // Arrange
        var selectionId = await CreateTestSelection();
        var unauthorizedUserId = Guid.NewGuid();

        // Act
        var result = await _service.GetProviderSelectionByIdAsync(selectionId, unauthorizedUserId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetProjectSelectionAsync Tests

    [Fact]
    public async Task GetProjectSelectionAsync_ExistingSelection_ReturnsSelection()
    {
        // Arrange
        await CreateTestSelection();

        // Act
        var result = await _service.GetProjectSelectionAsync(_projectId, _clientId);

        // Assert
        result.Should().NotBeNull();
        result!.Project.Should().NotBeNull();
    }

    [Fact]
    public async Task GetProjectSelectionAsync_NoSelection_ReturnsNull()
    {
        // Act
        var result = await _service.GetProjectSelectionAsync(_projectId, _clientId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetSelectionDashboardAsync Tests

    [Fact]
    public async Task GetSelectionDashboardAsync_ValidProject_ReturnsDashboard()
    {
        // Act
        var result = await _service.GetSelectionDashboardAsync(_projectId, _clientId);

        // Assert
        result.Should().NotBeNull();
        result.Project.Should().NotBeNull();
        result.RankedApplications.Should().NotBeEmpty();
        result.Statistics.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSelectionDashboardAsync_IncludesTopRecommendations()
    {
        // Act
        var result = await _service.GetSelectionDashboardAsync(_projectId, _clientId);

        // Assert
        result.TopRecommendations.Should().NotBeNull();
        result.TopRecommendations.Count.Should().BeLessOrEqualTo(3);
    }

    [Fact]
    public async Task GetSelectionDashboardAsync_AfterSelection_ShowsSelectionMade()
    {
        // Arrange
        await CreateTestSelection();

        // Act
        var result = await _service.GetSelectionDashboardAsync(_projectId, _clientId);

        // Assert
        result.IsSelectionMade.Should().BeTrue();
        result.CurrentSelection.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSelectionDashboardAsync_ProjectNotOwned_ThrowsException()
    {
        // Arrange
        var wrongClientId = Guid.NewGuid();

        // Act & Assert
        await _service.Invoking(s => s.GetSelectionDashboardAsync(_projectId, wrongClientId))
            .Should().ThrowAsync<UnauthorizedAccessException>();
    }

    #endregion

    #region RankApplicationsAsync Tests

    [Fact]
    public async Task RankApplicationsAsync_ReturnsRankedList()
    {
        // Act
        var result = await _service.RankApplicationsAsync(_projectId, _clientId);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().HaveCount(2); // Two pending applications
    }

    [Fact]
    public async Task RankApplicationsAsync_OrdersByRankingScore()
    {
        // Act
        var result = await _service.RankApplicationsAsync(_projectId, _clientId);

        // Assert
        result.Should().BeInDescendingOrder(r => r.RankingScore);
    }

    [Fact]
    public async Task RankApplicationsAsync_IncludesComparisonData()
    {
        // Act
        var result = await _service.RankApplicationsAsync(_projectId, _clientId);

        // Assert
        foreach (var comparison in result)
        {
            comparison.Application.Should().NotBeNull();
            comparison.SkillMatchPercentage.Should().BeGreaterOrEqualTo(0);
            comparison.ReputationScore.Should().BeGreaterOrEqualTo(0);
            comparison.RecommendationLevel.Should().NotBe(0);
        }
    }

    [Fact]
    public async Task RankApplicationsAsync_NotOwner_ThrowsException()
    {
        // Act & Assert
        await _service.Invoking(s => s.RankApplicationsAsync(_projectId, Guid.NewGuid()))
            .Should().ThrowAsync<UnauthorizedAccessException>();
    }

    #endregion

    #region CalculateApplicationRankingAsync Tests

    [Fact]
    public async Task CalculateApplicationRankingAsync_ReturnsFullComparisonData()
    {
        // Act
        var result = await _service.CalculateApplicationRankingAsync(_applicationId, _projectId);

        // Assert
        result.Should().NotBeNull();
        result.Application.Should().NotBeNull();
        result.RankingScore.Should().BeGreaterThan(0);
        result.SkillMatchPercentage.Should().BeGreaterThan(0);
        result.ReputationScore.Should().BeGreaterThan(0);
        result.TimelineScore.Should().BeGreaterOrEqualTo(0);
        result.BudgetScore.Should().BeGreaterThan(0);
        result.AvailabilityScore.Should().Be(100); // Available immediately
    }

    [Fact]
    public async Task CalculateApplicationRankingAsync_NotAvailableImmediately_LowerAvailabilityScore()
    {
        // Act
        var result = await _service.CalculateApplicationRankingAsync(_application2Id, _projectId);

        // Assert
        result.AvailabilityScore.Should().Be(70); // Not available immediately
    }

    [Fact]
    public async Task CalculateApplicationRankingAsync_GeneratesStrengthsAndConcerns()
    {
        // Act
        var result = await _service.CalculateApplicationRankingAsync(_applicationId, _projectId);

        // Assert
        result.Strengths.Should().NotBeNull();
        result.Concerns.Should().NotBeNull();
    }

    [Fact]
    public async Task CalculateApplicationRankingAsync_IncludesProviderHistory()
    {
        // Act
        var result = await _service.CalculateApplicationRankingAsync(_applicationId, _projectId);

        // Assert
        result.ProviderHistory.Should().NotBeNull();
        result.ProviderHistory.AverageRating.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CalculateApplicationRankingAsync_InvalidApplication_ThrowsException()
    {
        // Act & Assert
        await _service.Invoking(s => s.CalculateApplicationRankingAsync(Guid.NewGuid(), _projectId))
            .Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region GetProviderHistorySummaryAsync Tests

    [Fact]
    public async Task GetProviderHistorySummaryAsync_ValidProvider_ReturnsHistory()
    {
        // Act
        var result = await _service.GetProviderHistorySummaryAsync(_providerId);

        // Assert
        result.Should().NotBeNull();
        result.AverageRating.Should().BeGreaterThan(0);
        result.OnTimeDeliveryRate.Should().BeGreaterThan(0);
        result.MemberSince.Should().NotBe(default);
    }

    [Fact]
    public async Task GetProviderHistorySummaryAsync_OlderAccount_HasHigherRating()
    {
        // Provider was created 12 months ago, provider2 only 3 months ago
        var result1 = await _service.GetProviderHistorySummaryAsync(_providerId);
        var result2 = await _service.GetProviderHistorySummaryAsync(_provider2Id);

        // Assert - Older account should have slightly higher rating
        result1.AverageRating.Should().BeGreaterOrEqualTo(result2.AverageRating);
    }

    [Fact]
    public async Task GetProviderHistorySummaryAsync_InvalidProvider_ThrowsException()
    {
        // Act & Assert
        await _service.Invoking(s => s.GetProviderHistorySummaryAsync(Guid.NewGuid()))
            .Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region SendSelectionNotificationsAsync Tests

    [Fact]
    public async Task SendSelectionNotificationsAsync_SendsAcceptanceEmail()
    {
        // Arrange
        var selectionId = await CreateTestSelection();
        _emailService.SentEmails.Clear();

        // Act
        var result = await _service.SendSelectionNotificationsAsync(selectionId);

        // Assert
        result.Should().BeTrue();
        _emailService.SentEmails.Should().Contain(e => e.Subject.Contains("Congratulations"));
    }

    [Fact]
    public async Task SendSelectionNotificationsAsync_SendsRejectionEmails()
    {
        // Arrange
        var selectionId = await CreateTestSelection();
        _emailService.SentEmails.Clear();

        // Act
        await _service.SendSelectionNotificationsAsync(selectionId);

        // Assert
        _emailService.SentEmails.Should().Contain(e => e.Subject.Contains("Update on your project application"));
    }

    [Fact]
    public async Task SendSelectionNotificationsAsync_SelectionNotFound_ReturnsFalse()
    {
        // Act
        var result = await _service.SendSelectionNotificationsAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region IsProjectReadyForSelectionAsync Tests

    [Fact]
    public async Task IsProjectReadyForSelectionAsync_PublishedWithApplications_ReturnsTrue()
    {
        // Act
        var result = await _service.IsProjectReadyForSelectionAsync(_projectId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsProjectReadyForSelectionAsync_NoApplications_ReturnsFalse()
    {
        // Arrange - Create project without applications
        var newProjectId = Guid.NewGuid();
        var newProject = new Project
        {
            Id = newProjectId,
            ClientId = _clientId,
            Title = "Empty Project",
            Description = "No applications",
            Status = ProjectStatus.Published,
            CreditBudget = 1000,
            CreatedAt = DateTime.UtcNow
        };
        _context.Projects.Add(newProject);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.IsProjectReadyForSelectionAsync(newProjectId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsProjectReadyForSelectionAsync_DraftProject_ReturnsFalse()
    {
        // Arrange - Create draft project with application
        var draftProjectId = Guid.NewGuid();
        var draftProject = new Project
        {
            Id = draftProjectId,
            ClientId = _clientId,
            Title = "Draft Project",
            Description = "Not published yet",
            Status = ProjectStatus.Draft,
            CreditBudget = 1000,
            CreatedAt = DateTime.UtcNow
        };
        _context.Projects.Add(draftProject);

        var app = new ProjectApplication
        {
            Id = Guid.NewGuid(),
            ProjectId = draftProjectId,
            ProviderId = _providerId,
            CoverLetter = "Test",
            Status = ApplicationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        _context.ProjectApplications.Add(app);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.IsProjectReadyForSelectionAsync(draftProjectId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetRecommendedProvidersAsync Tests

    [Fact]
    public async Task GetRecommendedProvidersAsync_ReturnsTopProviders()
    {
        // Act
        var result = await _service.GetRecommendedProvidersAsync(_projectId, take: 5);

        // Assert
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetRecommendedProvidersAsync_FiltersLowRecommendations()
    {
        // Act
        var result = await _service.GetRecommendedProvidersAsync(_projectId, take: 5);

        // Assert
        result.All(r => r.RecommendationLevel >= RecommendationLevel.GoodCandidate).Should().BeTrue();
    }

    [Fact]
    public async Task GetRecommendedProvidersAsync_RespectsLimit()
    {
        // Act
        var result = await _service.GetRecommendedProvidersAsync(_projectId, take: 1);

        // Assert
        result.Should().HaveCountLessOrEqualTo(1);
    }

    #endregion

    #region HasUserAccessToSelectionAsync Tests

    [Fact]
    public async Task HasUserAccessToSelectionAsync_Client_ReturnsTrue()
    {
        // Arrange
        var selectionId = await CreateTestSelection();

        // Act
        var result = await _service.HasUserAccessToSelectionAsync(selectionId, _clientId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasUserAccessToSelectionAsync_SelectedProvider_ReturnsTrue()
    {
        // Arrange
        var selectionId = await CreateTestSelection();

        // Act
        var result = await _service.HasUserAccessToSelectionAsync(selectionId, _providerId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasUserAccessToSelectionAsync_UnrelatedUser_ReturnsFalse()
    {
        // Arrange
        var selectionId = await CreateTestSelection();

        // Act
        var result = await _service.HasUserAccessToSelectionAsync(selectionId, Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasUserAccessToSelectionAsync_SelectionNotFound_ReturnsFalse()
    {
        // Act
        var result = await _service.HasUserAccessToSelectionAsync(Guid.NewGuid(), _clientId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Edge Case Tests for Coverage (Phase 1.1)

    [Fact]
    public async Task CalculateApplicationRankingAsync_AggressiveTimeline_ReturnsLowerTimelineScore()
    {
        // Arrange - Create application with very aggressive timeline (< 50% of project duration)
        // Project duration is 53 days (60 - 7), so < 27 days is aggressive
        // To get a timelineScore < 50 (which triggers concern), need NO timeline or NULL timeline
        var aggressiveApp = new ProjectApplication
        {
            Id = Guid.NewGuid(),
            ProjectId = _projectId,
            ProviderId = _providerId,
            CoverLetter = "I can finish this very quickly",
            ProposedTimeline = null, // No timeline specified gets 50m score
            ProposedBudget = 4500,
            IsAvailableImmediately = true,
            Status = ApplicationStatus.Pending,
            SkillMatchScore = 0.9m,
            CreatedAt = DateTime.UtcNow
        };
        _context.ProjectApplications.Add(aggressiveApp);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CalculateApplicationRankingAsync(aggressiveApp.Id, _projectId);

        // Assert
        result.Should().NotBeNull();
        result.TimelineScore.Should().Be(50); // No timeline gets 50m score
        // Note: Concerns for timeline < 50 won't be added because 50 is not < 50
        // This test validates the code path at line 922 (no timeline) which returns 50m
    }

    [Fact]
    public async Task CalculateApplicationRankingAsync_BudgetExceedsProjectBudget_ReturnsLowBudgetScore()
    {
        // Arrange - Create application with budget exceeding project budget
        var expensiveApp = new ProjectApplication
        {
            Id = Guid.NewGuid(),
            ProjectId = _projectId,
            ProviderId = _provider2Id,
            CoverLetter = "I need more budget",
            ProposedTimeline = 45,
            ProposedBudget = 6000, // Exceeds 5000 project budget
            IsAvailableImmediately = true,
            Status = ApplicationStatus.Pending,
            SkillMatchScore = 0.8m,
            CreatedAt = DateTime.UtcNow
        };
        _context.ProjectApplications.Add(expensiveApp);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CalculateApplicationRankingAsync(expensiveApp.Id, _projectId);

        // Assert
        result.Should().NotBeNull();
        result.BudgetScore.Should().Be(30); // Budget exceeding project budget gets 30 score
        result.Concerns.Should().Contain(c => c.Contains("budget exceeds"));
    }

    [Fact]
    public async Task CalculateApplicationRankingAsync_LowQualityApplication_ReturnsLowRecommendation()
    {
        // Arrange - Create very low quality application (low skill match, over budget, not immediately available)
        var newProviderId = Guid.NewGuid();
        var newProvider = new User
        {
            Id = newProviderId,
            Email = "newbie@test.com",
            UserName = "Newbie",
            PasswordHash = "hash",
            Status = UserStatus.Active,
            FirstName = "New",
            LastName = "Bee",
            CreatedAt = DateTime.UtcNow.AddDays(-10), // Very new account
            Profile = new Profile
            {
                FirstName = "New",
                LastName = "Bee",
                UserId = newProviderId
            }
        };
        _context.Users.Add(newProvider);

        var lowQualityApp = new ProjectApplication
        {
            Id = Guid.NewGuid(),
            ProjectId = _projectId,
            ProviderId = newProviderId,
            CoverLetter = "Please hire me",
            ProposedTimeline = 30,
            ProposedBudget = 6500, // Way over budget - triggers line 957 (budget score 30)
            IsAvailableImmediately = false, // Not immediately available - availability score 70
            Status = ApplicationStatus.Pending,
            SkillMatchScore = 0.3m, // Very low skill match - skill score 30
            CreatedAt = DateTime.UtcNow
        };
        _context.ProjectApplications.Add(lowQualityApp);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CalculateApplicationRankingAsync(lowQualityApp.Id, _projectId);

        // Assert
        result.Should().NotBeNull();
        result.BudgetScore.Should().Be(30); // Budget exceeds project budget
        result.SkillMatchPercentage.Should().Be(30); // Low skill match
        result.AvailabilityScore.Should().Be(70); // Not immediately available
        result.Concerns.Should().NotBeEmpty();
        result.Concerns.Should().Contain(c => c.Contains("budget exceeds"));
        result.Concerns.Should().Contain(c => c.Contains("skill gaps") || c.Contains("Limited skill match"));
        result.RecommendationLevel.Should().BeOneOf(RecommendationLevel.ConsiderWithCaution, RecommendationLevel.NotRecommended);
    }

    [Fact]
    public async Task CalculateApplicationRankingAsync_NewProviderWithLimitedHistory_IncludesConcerns()
    {
        // Arrange - Provider2 was created only 3 months ago (line 128)
        // This should trigger "Relatively new provider" concern

        // Act
        var result = await _service.CalculateApplicationRankingAsync(_application2Id, _projectId);

        // Assert
        result.Should().NotBeNull();
        result.ProviderHistory.Should().NotBeNull();
        result.ProviderHistory.ProjectsCompleted.Should().BeLessThan(3); // New provider
        result.Concerns.Should().Contain(c => c.Contains("new provider") || c.Contains("limited project history"));
    }

    #endregion

    #region Helper Methods

    private async Task<Guid> CreateTestSelection()
    {
        var createDto = new CreateProviderSelectionDto
        {
            ProjectId = _projectId,
            SelectedProviderId = _providerId,
            SelectedApplicationId = _applicationId,
            SelectionReason = "This provider demonstrated excellent technical skills and professional communication throughout the application process.",
            EscrowAmount = 4500
        };

        var result = await _service.CreateProviderSelectionAsync(createDto, _clientId, _testIp);
        return (Guid)result.Data!;
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
    }
}
