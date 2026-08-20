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
/// Integration tests for WorkspaceService - COLLABORATION CORE.
///
/// CORRECT PATTERN (per TDD_GUIDE.md):
/// - Uses real database (in-memory EF Core)
/// - Uses MockAuditLogService that writes to real database (internal service)
/// - Verifies actual database state, not mock interactions
///
/// Max mocked external dependencies: 0
/// </summary>
[IntegrationTest]
public class WorkspaceServiceIntegrationTests : IDisposable
{
    private readonly SkillLedgerDbContext _context;
    private readonly MockAuditLogService _auditLogService;
    private readonly ILogger<WorkspaceService> _logger;
    private readonly WorkspaceService _workspaceService;

    public WorkspaceServiceIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<SkillLedgerDbContext>()
            .UseInMemoryDatabase(databaseName: $"WorkspaceServiceTests_{Guid.NewGuid()}")
            .Options;

        _context = new SkillLedgerDbContext(options);
        _context.Database.EnsureCreated();

        _auditLogService = new MockAuditLogService(_context);
        _logger = new LoggerFactory().CreateLogger<WorkspaceService>();

        _workspaceService = new WorkspaceService(
            _context,
            _logger,
            _auditLogService
        );
    }

    #region Helper Methods

    private async Task<User> CreateTestUserAsync(string email)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            FirstName = "Test",
            LastName = "User",
            Status = UserStatus.Active
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    private async Task<Project> CreateTestProjectAsync(Guid clientId, string title = "Test Project", Guid? providerId = null)
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = "Test project description",
            ClientId = clientId,
            ProviderId = providerId,
            CreditBudget = 1000,
            Status = ProjectStatus.Published,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(3)
        };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
        return project;
    }

    private async Task<ProjectWorkspace> CreateTestWorkspaceAsync(Guid projectId, Guid clientId, Guid providerId)
    {
        var workspace = new ProjectWorkspace
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            ClientId = clientId,
            ProviderId = providerId,
            Status = WorkspaceStatus.Active,
            IntegrationStatus = "initialized"
        };
        _context.ProjectWorkspaces.Add(workspace);
        await _context.SaveChangesAsync();
        return workspace;
    }

    #endregion

    #region CreateWorkspaceAsync Tests

    [Fact]
    public async Task CreateWorkspaceAsync_ValidProject_CreatesSuccessfully()
    {
        // Arrange
        var client = await CreateTestUserAsync("client@test.com");
        var provider = await CreateTestUserAsync("provider@test.com");
        var project = await CreateTestProjectAsync(client.Id, providerId: provider.Id);

        // Act
        var workspace = await _workspaceService.CreateWorkspaceAsync(project.Id, provider.Id);

        // Assert
        workspace.Should().NotBeNull();
        workspace.ProjectId.Should().Be(project.Id);
        workspace.ClientId.Should().Be(client.Id);
        workspace.ProviderId.Should().Be(provider.Id);
        workspace.Status.Should().Be(WorkspaceStatus.Active);
        workspace.IntegrationStatus.Should().Be("initialized");
        workspace.WorkspaceKey.Should().NotBeNullOrEmpty();

        // Verify in database
        var savedWorkspace = await _context.ProjectWorkspaces.FindAsync(workspace.Id);
        savedWorkspace.Should().NotBeNull();
        savedWorkspace!.ProjectId.Should().Be(project.Id);
    }

    [Fact]
    public async Task CreateWorkspaceAsync_NonExistentProject_ThrowsException()
    {
        // Arrange
        var provider = await CreateTestUserAsync("provider@test.com");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _workspaceService.CreateWorkspaceAsync(Guid.NewGuid(), provider.Id));
    }

    [Fact]
    public async Task CreateWorkspaceAsync_DuplicateWorkspace_ThrowsException()
    {
        // Arrange
        var client = await CreateTestUserAsync("client@test.com");
        var provider = await CreateTestUserAsync("provider@test.com");
        var project = await CreateTestProjectAsync(client.Id, providerId: provider.Id);

        // First workspace creation
        await _workspaceService.CreateWorkspaceAsync(project.Id, provider.Id);

        // Act & Assert - Second creation should throw
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _workspaceService.CreateWorkspaceAsync(project.Id, provider.Id));
    }

    [Fact]
    public async Task CreateWorkspaceAsync_CreatesAuditLog()
    {
        // Arrange
        var client = await CreateTestUserAsync("client@test.com");
        var provider = await CreateTestUserAsync("provider@test.com");
        var project = await CreateTestProjectAsync(client.Id, providerId: provider.Id);

        // Act
        var workspace = await _workspaceService.CreateWorkspaceAsync(project.Id, provider.Id);

        // Assert
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.Action == "WorkspaceCreated");
        auditLog.Should().NotBeNull();
        auditLog!.UserId.Should().Be(client.Id);
        auditLog.Details.Should().Contain(workspace.Id.ToString());
    }

    #endregion

    #region GetWorkspaceAsync Tests

    [Fact]
    public async Task GetWorkspaceAsync_ClientAccess_ReturnsWorkspace()
    {
        // Arrange
        var client = await CreateTestUserAsync("client@test.com");
        var provider = await CreateTestUserAsync("provider@test.com");
        var project = await CreateTestProjectAsync(client.Id);
        var workspace = await CreateTestWorkspaceAsync(project.Id, client.Id, provider.Id);
        _context.ChangeTracker.Clear();

        // Act
        var result = await _workspaceService.GetWorkspaceAsync(workspace.Id, client.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(workspace.Id);
        result.Project.Should().NotBeNull();
        result.Client.Should().NotBeNull();
        result.Provider.Should().NotBeNull();
    }

    [Fact]
    public async Task GetWorkspaceAsync_ProviderAccess_ReturnsWorkspace()
    {
        // Arrange
        var client = await CreateTestUserAsync("client@test.com");
        var provider = await CreateTestUserAsync("provider@test.com");
        var project = await CreateTestProjectAsync(client.Id);
        var workspace = await CreateTestWorkspaceAsync(project.Id, client.Id, provider.Id);
        _context.ChangeTracker.Clear();

        // Act
        var result = await _workspaceService.GetWorkspaceAsync(workspace.Id, provider.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(workspace.Id);
    }

    [Fact]
    public async Task GetWorkspaceAsync_UnauthorizedUser_ReturnsNull()
    {
        // Arrange
        var client = await CreateTestUserAsync("client@test.com");
        var provider = await CreateTestUserAsync("provider@test.com");
        var unauthorized = await CreateTestUserAsync("unauthorized@test.com");
        var project = await CreateTestProjectAsync(client.Id);
        var workspace = await CreateTestWorkspaceAsync(project.Id, client.Id, provider.Id);
        _context.ChangeTracker.Clear();

        // Act
        var result = await _workspaceService.GetWorkspaceAsync(workspace.Id, unauthorized.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetWorkspaceAsync_NonExistentWorkspace_ReturnsNull()
    {
        // Arrange
        var user = await CreateTestUserAsync("user@test.com");

        // Act
        var result = await _workspaceService.GetWorkspaceAsync(Guid.NewGuid(), user.Id);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetWorkspaceByProjectAsync Tests

    [Fact]
    public async Task GetWorkspaceByProjectAsync_ExistingProject_ReturnsWorkspace()
    {
        // Arrange
        var client = await CreateTestUserAsync("client@test.com");
        var provider = await CreateTestUserAsync("provider@test.com");
        var project = await CreateTestProjectAsync(client.Id);
        var workspace = await CreateTestWorkspaceAsync(project.Id, client.Id, provider.Id);
        _context.ChangeTracker.Clear();

        // Act
        var result = await _workspaceService.GetWorkspaceByProjectAsync(project.Id, client.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(workspace.Id);
        result.ProjectId.Should().Be(project.Id);
    }

    [Fact]
    public async Task GetWorkspaceByProjectAsync_UnauthorizedUser_ReturnsNull()
    {
        // Arrange
        var client = await CreateTestUserAsync("client@test.com");
        var provider = await CreateTestUserAsync("provider@test.com");
        var unauthorized = await CreateTestUserAsync("unauthorized@test.com");
        var project = await CreateTestProjectAsync(client.Id);
        await CreateTestWorkspaceAsync(project.Id, client.Id, provider.Id);
        _context.ChangeTracker.Clear();

        // Act
        var result = await _workspaceService.GetWorkspaceByProjectAsync(project.Id, unauthorized.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetWorkspaceByProjectAsync_NoWorkspace_ReturnsNull()
    {
        // Arrange
        var client = await CreateTestUserAsync("client@test.com");
        var project = await CreateTestProjectAsync(client.Id);

        // Act
        var result = await _workspaceService.GetWorkspaceByProjectAsync(project.Id, client.Id);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetUserWorkspacesAsync Tests

    [Fact]
    public async Task GetUserWorkspacesAsync_ClientWorkspaces_ReturnsAllWorkspaces()
    {
        // Arrange
        var client = await CreateTestUserAsync("client@test.com");
        var provider1 = await CreateTestUserAsync("provider1@test.com");
        var provider2 = await CreateTestUserAsync("provider2@test.com");

        var project1 = await CreateTestProjectAsync(client.Id, "Project 1", provider1.Id);
        var project2 = await CreateTestProjectAsync(client.Id, "Project 2", provider2.Id);

        await CreateTestWorkspaceAsync(project1.Id, client.Id, provider1.Id);
        await CreateTestWorkspaceAsync(project2.Id, client.Id, provider2.Id);
        _context.ChangeTracker.Clear();

        // Act
        var result = await _workspaceService.GetUserWorkspacesAsync(client.Id);

        // Assert
        result.Should().HaveCount(2);
        result.All(w => w.IsClient).Should().BeTrue();
    }

    [Fact]
    public async Task GetUserWorkspacesAsync_ProviderWorkspaces_ReturnsCorrectly()
    {
        // Arrange
        var client1 = await CreateTestUserAsync("client1@test.com");
        var client2 = await CreateTestUserAsync("client2@test.com");
        var provider = await CreateTestUserAsync("provider@test.com");

        var project1 = await CreateTestProjectAsync(client1.Id, "Project 1");
        var project2 = await CreateTestProjectAsync(client2.Id, "Project 2");

        await CreateTestWorkspaceAsync(project1.Id, client1.Id, provider.Id);
        await CreateTestWorkspaceAsync(project2.Id, client2.Id, provider.Id);
        _context.ChangeTracker.Clear();

        // Act
        var result = await _workspaceService.GetUserWorkspacesAsync(provider.Id);

        // Assert
        result.Should().HaveCount(2);
        result.All(w => !w.IsClient).Should().BeTrue();
    }

    [Fact]
    public async Task GetUserWorkspacesAsync_MixedRoles_ReturnsAllWorkspaces()
    {
        // Arrange
        var user = await CreateTestUserAsync("user@test.com");
        var otherClient = await CreateTestUserAsync("otherclient@test.com");
        var otherProvider = await CreateTestUserAsync("otherprovider@test.com");

        // User as client
        var projectAsClient = await CreateTestProjectAsync(user.Id, "Client Project");
        await CreateTestWorkspaceAsync(projectAsClient.Id, user.Id, otherProvider.Id);

        // User as provider
        var projectAsProvider = await CreateTestProjectAsync(otherClient.Id, "Provider Project");
        await CreateTestWorkspaceAsync(projectAsProvider.Id, otherClient.Id, user.Id);
        _context.ChangeTracker.Clear();

        // Act
        var result = await _workspaceService.GetUserWorkspacesAsync(user.Id);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(w => w.IsClient);
        result.Should().Contain(w => !w.IsClient);
    }

    [Fact]
    public async Task GetUserWorkspacesAsync_NoWorkspaces_ReturnsEmpty()
    {
        // Arrange
        var user = await CreateTestUserAsync("user@test.com");

        // Act
        var result = await _workspaceService.GetUserWorkspacesAsync(user.Id);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserWorkspacesAsync_OrdersByLastActivityDescending()
    {
        // Arrange
        var client = await CreateTestUserAsync("client@test.com");
        var provider = await CreateTestUserAsync("provider@test.com");

        var project1 = await CreateTestProjectAsync(client.Id, "Project 1", provider.Id);
        var project2 = await CreateTestProjectAsync(client.Id, "Project 2", provider.Id);

        var workspace1 = await CreateTestWorkspaceAsync(project1.Id, client.Id, provider.Id);
        workspace1.LastSyncedAt = DateTime.UtcNow.AddDays(-7);

        var workspace2 = await CreateTestWorkspaceAsync(project2.Id, client.Id, provider.Id);
        workspace2.LastSyncedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var result = (await _workspaceService.GetUserWorkspacesAsync(client.Id)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.First().ProjectTitle.Should().Be("Project 2"); // Most recent first
    }

    #endregion

    #region GetWorkspaceDashboardAsync Tests

    [Fact]
    public async Task GetWorkspaceDashboardAsync_ValidAccess_ReturnsDashboard()
    {
        // Arrange
        var client = await CreateTestUserAsync("client@test.com");
        var provider = await CreateTestUserAsync("provider@test.com");
        var project = await CreateTestProjectAsync(client.Id, "Dashboard Test Project");
        var workspace = await CreateTestWorkspaceAsync(project.Id, client.Id, provider.Id);
        workspace.TimelineData = "{\"events\": []}";
        workspace.MilestoneData = "{\"milestones\": []}";
        workspace.IntegrationStatus = "connected";
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var dashboard = await _workspaceService.GetWorkspaceDashboardAsync(workspace.Id, client.Id);

        // Assert
        dashboard.Should().NotBeNull();
        dashboard.WorkspaceId.Should().Be(workspace.Id);
        dashboard.ProjectTitle.Should().Be("Dashboard Test Project");
        dashboard.ClientName.Should().Be("client@test.com");
        dashboard.ProviderName.Should().Be("provider@test.com");
        dashboard.Status.Should().Be(WorkspaceStatus.Active);
        dashboard.TimelineData.Should().NotBeNullOrEmpty();
        dashboard.MilestoneData.Should().NotBeNullOrEmpty();
        dashboard.IntegrationStatus.Should().Be("connected");
    }

    [Fact]
    public async Task GetWorkspaceDashboardAsync_UnauthorizedUser_ThrowsException()
    {
        // Arrange
        var client = await CreateTestUserAsync("client@test.com");
        var provider = await CreateTestUserAsync("provider@test.com");
        var unauthorized = await CreateTestUserAsync("unauthorized@test.com");
        var project = await CreateTestProjectAsync(client.Id, providerId: provider.Id);
        var workspace = await CreateTestWorkspaceAsync(project.Id, client.Id, provider.Id);
        _context.ChangeTracker.Clear();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _workspaceService.GetWorkspaceDashboardAsync(workspace.Id, unauthorized.Id));
    }

    [Fact]
    public async Task GetWorkspaceDashboardAsync_ArchivedWorkspace_StillReturnsData()
    {
        // Arrange
        var client = await CreateTestUserAsync("client@test.com");
        var provider = await CreateTestUserAsync("provider@test.com");
        var project = await CreateTestProjectAsync(client.Id, providerId: provider.Id);
        var workspace = await CreateTestWorkspaceAsync(project.Id, client.Id, provider.Id);
        workspace.ArchiveWorkspace();
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var dashboard = await _workspaceService.GetWorkspaceDashboardAsync(workspace.Id, client.Id);

        // Assert
        dashboard.Should().NotBeNull();
        dashboard.Status.Should().Be(WorkspaceStatus.Archived);
        dashboard.ArchivedAt.Should().NotBeNull();
    }

    #endregion

    #region ArchiveWorkspaceAsync Tests

    [Fact]
    public async Task ArchiveWorkspaceAsync_ValidClient_ArchivesSuccessfully()
    {
        // Arrange
        var client = await CreateTestUserAsync("client@test.com");
        var provider = await CreateTestUserAsync("provider@test.com");
        var project = await CreateTestProjectAsync(client.Id);
        var workspace = await CreateTestWorkspaceAsync(project.Id, client.Id, provider.Id);

        // Act
        var result = await _workspaceService.ArchiveWorkspaceAsync(workspace.Id, client.Id);

        // Assert
        result.Should().BeTrue();

        _context.ChangeTracker.Clear();
        var archivedWorkspace = await _context.ProjectWorkspaces.FindAsync(workspace.Id);
        archivedWorkspace!.Status.Should().Be(WorkspaceStatus.Archived);
        archivedWorkspace.ArchivedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ArchiveWorkspaceAsync_ValidProvider_ArchivesSuccessfully()
    {
        // Arrange
        var client = await CreateTestUserAsync("client@test.com");
        var provider = await CreateTestUserAsync("provider@test.com");
        var project = await CreateTestProjectAsync(client.Id);
        var workspace = await CreateTestWorkspaceAsync(project.Id, client.Id, provider.Id);

        // Act
        var result = await _workspaceService.ArchiveWorkspaceAsync(workspace.Id, provider.Id);

        // Assert
        result.Should().BeTrue();

        _context.ChangeTracker.Clear();
        var archivedWorkspace = await _context.ProjectWorkspaces.FindAsync(workspace.Id);
        archivedWorkspace!.Status.Should().Be(WorkspaceStatus.Archived);
    }

    [Fact]
    public async Task ArchiveWorkspaceAsync_UnauthorizedUser_ReturnsFalse()
    {
        // Arrange
        var client = await CreateTestUserAsync("client@test.com");
        var provider = await CreateTestUserAsync("provider@test.com");
        var unauthorized = await CreateTestUserAsync("unauthorized@test.com");
        var project = await CreateTestProjectAsync(client.Id);
        var workspace = await CreateTestWorkspaceAsync(project.Id, client.Id, provider.Id);

        // Act
        var result = await _workspaceService.ArchiveWorkspaceAsync(workspace.Id, unauthorized.Id);

        // Assert
        result.Should().BeFalse();

        // Workspace should not be archived
        _context.ChangeTracker.Clear();
        var unchangedWorkspace = await _context.ProjectWorkspaces.FindAsync(workspace.Id);
        unchangedWorkspace!.Status.Should().Be(WorkspaceStatus.Active);
    }

    [Fact]
    public async Task ArchiveWorkspaceAsync_NonExistentWorkspace_ReturnsFalse()
    {
        // Arrange
        var user = await CreateTestUserAsync("user@test.com");

        // Act
        var result = await _workspaceService.ArchiveWorkspaceAsync(Guid.NewGuid(), user.Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ArchiveWorkspaceAsync_CreatesAuditLog()
    {
        // Arrange
        var client = await CreateTestUserAsync("client@test.com");
        var provider = await CreateTestUserAsync("provider@test.com");
        var project = await CreateTestProjectAsync(client.Id);
        var workspace = await CreateTestWorkspaceAsync(project.Id, client.Id, provider.Id);

        // Act
        await _workspaceService.ArchiveWorkspaceAsync(workspace.Id, client.Id);

        // Assert
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.Action == "WorkspaceArchived");
        auditLog.Should().NotBeNull();
        auditLog!.UserId.Should().Be(client.Id);
    }

    #endregion

    #region UpdateTimelineAsync Tests

    [Fact]
    public async Task UpdateTimelineAsync_ValidData_UpdatesSuccessfully()
    {
        // Arrange
        var client = await CreateTestUserAsync("client@test.com");
        var provider = await CreateTestUserAsync("provider@test.com");
        var project = await CreateTestProjectAsync(client.Id);
        var workspace = await CreateTestWorkspaceAsync(project.Id, client.Id, provider.Id);

        var timelineData = new TimelineDataDto
        {
            Events = new List<TimelineEventDto>
            {
                new TimelineEventDto
                {
                    Id = Guid.NewGuid(),
                    Title = "Project Kickoff",
                    Description = "Initial project meeting",
                    EventDate = DateTime.UtcNow,
                    EventType = "meeting",
                    Status = "completed"
                }
            },
            LastUpdated = DateTime.UtcNow,
            Notes = "Initial timeline"
        };

        // Act
        var result = await _workspaceService.UpdateTimelineAsync(workspace.Id, client.Id, timelineData);

        // Assert
        result.Should().BeTrue();

        _context.ChangeTracker.Clear();
        var updatedWorkspace = await _context.ProjectWorkspaces.FindAsync(workspace.Id);
        updatedWorkspace!.TimelineData.Should().NotBeNullOrEmpty();
        updatedWorkspace.TimelineData.Should().Contain("Project Kickoff");
        updatedWorkspace.LastSyncedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateTimelineAsync_UnauthorizedUser_ReturnsFalse()
    {
        // Arrange
        var client = await CreateTestUserAsync("client@test.com");
        var provider = await CreateTestUserAsync("provider@test.com");
        var unauthorized = await CreateTestUserAsync("unauthorized@test.com");
        var project = await CreateTestProjectAsync(client.Id);
        var workspace = await CreateTestWorkspaceAsync(project.Id, client.Id, provider.Id);

        var timelineData = new TimelineDataDto { Events = new List<TimelineEventDto>() };

        // Act
        var result = await _workspaceService.UpdateTimelineAsync(workspace.Id, unauthorized.Id, timelineData);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateTimelineAsync_CreatesAuditLog()
    {
        // Arrange
        var client = await CreateTestUserAsync("client@test.com");
        var provider = await CreateTestUserAsync("provider@test.com");
        var project = await CreateTestProjectAsync(client.Id);
        var workspace = await CreateTestWorkspaceAsync(project.Id, client.Id, provider.Id);

        var timelineData = new TimelineDataDto { Events = new List<TimelineEventDto>() };

        // Act
        await _workspaceService.UpdateTimelineAsync(workspace.Id, client.Id, timelineData);

        // Assert
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.Action == "TimelineUpdated");
        auditLog.Should().NotBeNull();
    }

    #endregion

    #region UpdateMilestonesAsync Tests

    [Fact]
    public async Task UpdateMilestonesAsync_ValidData_UpdatesSuccessfully()
    {
        // Arrange
        var client = await CreateTestUserAsync("client@test.com");
        var provider = await CreateTestUserAsync("provider@test.com");
        var project = await CreateTestProjectAsync(client.Id);
        var workspace = await CreateTestWorkspaceAsync(project.Id, client.Id, provider.Id);

        var milestoneData = new
        {
            milestones = new[]
            {
                new { id = Guid.NewGuid(), title = "Phase 1", status = "completed" },
                new { id = Guid.NewGuid(), title = "Phase 2", status = "in_progress" }
            }
        };

        // Act
        var result = await _workspaceService.UpdateMilestonesAsync(workspace.Id, client.Id, milestoneData);

        // Assert
        result.Should().BeTrue();

        _context.ChangeTracker.Clear();
        var updatedWorkspace = await _context.ProjectWorkspaces.FindAsync(workspace.Id);
        updatedWorkspace!.MilestoneData.Should().NotBeNullOrEmpty();
        updatedWorkspace.MilestoneData.Should().Contain("Phase 1");
        updatedWorkspace.MilestoneData.Should().Contain("Phase 2");
    }

    [Fact]
    public async Task UpdateMilestonesAsync_UnauthorizedUser_ReturnsFalse()
    {
        // Arrange
        var client = await CreateTestUserAsync("client@test.com");
        var provider = await CreateTestUserAsync("provider@test.com");
        var unauthorized = await CreateTestUserAsync("unauthorized@test.com");
        var project = await CreateTestProjectAsync(client.Id);
        var workspace = await CreateTestWorkspaceAsync(project.Id, client.Id, provider.Id);

        // Act
        var result = await _workspaceService.UpdateMilestonesAsync(workspace.Id, unauthorized.Id, new { });

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateMilestonesAsync_CreatesAuditLog()
    {
        // Arrange
        var client = await CreateTestUserAsync("client@test.com");
        var provider = await CreateTestUserAsync("provider@test.com");
        var project = await CreateTestProjectAsync(client.Id);
        var workspace = await CreateTestWorkspaceAsync(project.Id, client.Id, provider.Id);

        // Act
        await _workspaceService.UpdateMilestonesAsync(workspace.Id, provider.Id, new { });

        // Assert
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.Action == "MilestonesUpdated");
        auditLog.Should().NotBeNull();
    }

    #endregion

    #region UpdateIntegrationStatusAsync Tests

    [Fact]
    public async Task UpdateIntegrationStatusAsync_ValidStatus_UpdatesSuccessfully()
    {
        // Arrange
        var client = await CreateTestUserAsync("client@test.com");
        var provider = await CreateTestUserAsync("provider@test.com");
        var project = await CreateTestProjectAsync(client.Id);
        var workspace = await CreateTestWorkspaceAsync(project.Id, client.Id, provider.Id);

        // Act
        var result = await _workspaceService.UpdateIntegrationStatusAsync(workspace.Id, "connected");

        // Assert
        result.Should().BeTrue();

        _context.ChangeTracker.Clear();
        var updatedWorkspace = await _context.ProjectWorkspaces.FindAsync(workspace.Id);
        updatedWorkspace!.IntegrationStatus.Should().Be("connected");
        updatedWorkspace.LastSyncedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateIntegrationStatusAsync_NonExistentWorkspace_ReturnsFalse()
    {
        // Act
        var result = await _workspaceService.UpdateIntegrationStatusAsync(Guid.NewGuid(), "connected");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateIntegrationStatusAsync_MultipleUpdates_KeepsLatest()
    {
        // Arrange
        var client = await CreateTestUserAsync("client@test.com");
        var provider = await CreateTestUserAsync("provider@test.com");
        var project = await CreateTestProjectAsync(client.Id);
        var workspace = await CreateTestWorkspaceAsync(project.Id, client.Id, provider.Id);

        // Act
        await _workspaceService.UpdateIntegrationStatusAsync(workspace.Id, "connecting");
        await _workspaceService.UpdateIntegrationStatusAsync(workspace.Id, "connected");
        await _workspaceService.UpdateIntegrationStatusAsync(workspace.Id, "syncing");

        // Assert
        _context.ChangeTracker.Clear();
        var updatedWorkspace = await _context.ProjectWorkspaces.FindAsync(workspace.Id);
        updatedWorkspace!.IntegrationStatus.Should().Be("syncing");
    }

    #endregion

    #region HasUserAccessAsync Tests

    [Fact]
    public async Task HasUserAccessAsync_Client_ReturnsTrue()
    {
        // Arrange
        var client = await CreateTestUserAsync("client@test.com");
        var provider = await CreateTestUserAsync("provider@test.com");
        var project = await CreateTestProjectAsync(client.Id);
        var workspace = await CreateTestWorkspaceAsync(project.Id, client.Id, provider.Id);

        // Act
        var hasAccess = await _workspaceService.HasUserAccessAsync(workspace.Id, client.Id);

        // Assert
        hasAccess.Should().BeTrue();
    }

    [Fact]
    public async Task HasUserAccessAsync_Provider_ReturnsTrue()
    {
        // Arrange
        var client = await CreateTestUserAsync("client@test.com");
        var provider = await CreateTestUserAsync("provider@test.com");
        var project = await CreateTestProjectAsync(client.Id);
        var workspace = await CreateTestWorkspaceAsync(project.Id, client.Id, provider.Id);

        // Act
        var hasAccess = await _workspaceService.HasUserAccessAsync(workspace.Id, provider.Id);

        // Assert
        hasAccess.Should().BeTrue();
    }

    [Fact]
    public async Task HasUserAccessAsync_Unauthorized_ReturnsFalse()
    {
        // Arrange
        var client = await CreateTestUserAsync("client@test.com");
        var provider = await CreateTestUserAsync("provider@test.com");
        var unauthorized = await CreateTestUserAsync("unauthorized@test.com");
        var project = await CreateTestProjectAsync(client.Id);
        var workspace = await CreateTestWorkspaceAsync(project.Id, client.Id, provider.Id);

        // Act
        var hasAccess = await _workspaceService.HasUserAccessAsync(workspace.Id, unauthorized.Id);

        // Assert
        hasAccess.Should().BeFalse();
    }

    [Fact]
    public async Task HasUserAccessAsync_NonExistentWorkspace_ReturnsFalse()
    {
        // Arrange
        var user = await CreateTestUserAsync("user@test.com");

        // Act
        var hasAccess = await _workspaceService.HasUserAccessAsync(Guid.NewGuid(), user.Id);

        // Assert
        hasAccess.Should().BeFalse();
    }

    #endregion

    #region WorkspaceKey Security Tests

    [Fact]
    public async Task CreateWorkspaceAsync_GeneratesUniqueKey()
    {
        // Arrange
        var client = await CreateTestUserAsync("client@test.com");
        var provider1 = await CreateTestUserAsync("provider1@test.com");
        var provider2 = await CreateTestUserAsync("provider2@test.com");

        var project1 = await CreateTestProjectAsync(client.Id, "Project 1", provider1.Id);
        var project2 = await CreateTestProjectAsync(client.Id, "Project 2", provider2.Id);

        // Act
        var workspace1 = await _workspaceService.CreateWorkspaceAsync(project1.Id, provider1.Id);
        var workspace2 = await _workspaceService.CreateWorkspaceAsync(project2.Id, provider2.Id);

        // Assert
        workspace1.WorkspaceKey.Should().NotBeNullOrEmpty();
        workspace2.WorkspaceKey.Should().NotBeNullOrEmpty();
        workspace1.WorkspaceKey.Should().NotBe(workspace2.WorkspaceKey);
    }

    [Fact]
    public async Task CreateWorkspaceAsync_KeyIsUrlSafe()
    {
        // Arrange
        var client = await CreateTestUserAsync("client@test.com");
        var provider = await CreateTestUserAsync("provider@test.com");
        var project = await CreateTestProjectAsync(client.Id, providerId: provider.Id);

        // Act
        var workspace = await _workspaceService.CreateWorkspaceAsync(project.Id, provider.Id);

        // Assert
        workspace.WorkspaceKey.Should().NotContain("+");
        workspace.WorkspaceKey.Should().NotContain("/");
        workspace.WorkspaceKey.Should().NotContain("=");
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
