using SkillLedger.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Infrastructure.Services;
using Xunit;

namespace SkillLedger.Tests.Core.Services
{
    [UnitTest]
    [CoreTest]
    public class WorkspaceServiceTests : IDisposable
    {
        private readonly SkillLedgerDbContext _context;
        private readonly Mock<ILogger<WorkspaceService>> _mockLogger;
        private readonly Mock<IAuditLogService> _mockAuditLogService;
        private readonly WorkspaceService _workspaceService;

        public WorkspaceServiceTests()
        {
            var options = new DbContextOptionsBuilder<SkillLedgerDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new SkillLedgerDbContext(options);

            _mockLogger = new Mock<ILogger<WorkspaceService>>();
            _mockAuditLogService = new Mock<IAuditLogService>();

            _workspaceService = new WorkspaceService(_context, _mockLogger.Object, _mockAuditLogService.Object);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task CreateWorkspaceAsync_Should_Create_Workspace_For_Valid_Project()
        {
            // Arrange
            var client = new User { Id = Guid.NewGuid(), Email = "client@test.com" };
            var provider = new User { Id = Guid.NewGuid(), Email = "provider@test.com" };
            var project = new Project
            {
                Id = Guid.NewGuid(),
                ClientId = client.Id,
                ProviderId = provider.Id,
                Title = "Test Project",
                Description = "Test Description"
            };

            _context.Users.AddRange(client, provider);
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // Act
            var result = await _workspaceService.CreateWorkspaceAsync(project.Id, provider.Id);

            // Assert
            result.Should().NotBeNull();
            result.ProjectId.Should().Be(project.Id);
            result.ClientId.Should().Be(client.Id);
            result.ProviderId.Should().Be(provider.Id);
            result.Status.Should().Be(WorkspaceStatus.Active);
        }

        [Fact]
        public async Task CreateWorkspaceAsync_Should_Throw_When_Project_Not_Found()
        {
            // Arrange
            var nonExistentProjectId = Guid.NewGuid();
            var providerId = Guid.NewGuid();

            // Act & Assert
            await FluentActions.Invoking(() => _workspaceService.CreateWorkspaceAsync(nonExistentProjectId, providerId))
                .Should().ThrowAsync<ArgumentException>()
                .WithMessage("Project not found*");
        }

        [Fact]
        public async Task CreateWorkspaceAsync_Should_Throw_When_Workspace_Already_Exists()
        {
            // Arrange
            var client = new User { Id = Guid.NewGuid(), Email = "client@test.com" };
            var provider = new User { Id = Guid.NewGuid(), Email = "provider@test.com" };
            var project = new Project
            {
                Id = Guid.NewGuid(),
                ClientId = client.Id,
                ProviderId = provider.Id,
                Title = "Test Project",
                Description = "Test Description"
            };
            var existingWorkspace = new ProjectWorkspace
            {
                ProjectId = project.Id,
                ClientId = client.Id,
                ProviderId = provider.Id
            };

            _context.Users.AddRange(client, provider);
            _context.Projects.Add(project);
            _context.ProjectWorkspaces.Add(existingWorkspace);
            await _context.SaveChangesAsync();

            // Act & Assert
            await FluentActions.Invoking(() => _workspaceService.CreateWorkspaceAsync(project.Id, provider.Id))
                .Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Workspace already exists for this project*");
        }

        [Fact]
        public async Task GetWorkspaceAsync_Should_Return_Workspace_When_User_Has_Access()
        {
            // Arrange
            var clientId = Guid.NewGuid();
            var providerId = Guid.NewGuid();
            var projectId = Guid.NewGuid();

            // Create required entities
            var client = new User { Id = clientId, Email = "client@test.com" };
            var provider = new User { Id = providerId, Email = "provider@test.com" };
            var project = new Project
            {
                Id = projectId,
                ClientId = clientId,
                Title = "Test Project",
                Description = "Test Description"
            };

            var workspace = new ProjectWorkspace
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                ClientId = clientId,
                ProviderId = providerId
            };

            _context.Users.AddRange(client, provider);
            _context.Projects.Add(project);
            _context.ProjectWorkspaces.Add(workspace);
            await _context.SaveChangesAsync();

            // Act
            var result = await _workspaceService.GetWorkspaceAsync(workspace.Id, clientId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(workspace.Id);
        }

        [Fact]
        public async Task GetWorkspaceAsync_Should_Return_Null_When_User_No_Access()
        {
            // Arrange
            var clientId = Guid.NewGuid();
            var providerId = Guid.NewGuid();
            var unauthorizedUserId = Guid.NewGuid();
            var workspace = new ProjectWorkspace
            {
                Id = Guid.NewGuid(),
                ProjectId = Guid.NewGuid(),
                ClientId = clientId,
                ProviderId = providerId
            };

            _context.ProjectWorkspaces.Add(workspace);
            await _context.SaveChangesAsync();

            // Act
            var result = await _workspaceService.GetWorkspaceAsync(workspace.Id, unauthorizedUserId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetUserWorkspacesAsync_Should_Return_User_Workspaces()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var thirdUserId = Guid.NewGuid();

            // Create users and projects
            var user = new User { Id = userId, Email = "user@test.com" };
            var otherUser = new User { Id = otherUserId, Email = "other@test.com" };
            var thirdUser = new User { Id = thirdUserId, Email = "third@test.com" };

            var project1 = new Project
            {
                Id = Guid.NewGuid(),
                ClientId = userId,
                Title = "Project 1",
                Description = "Project 1 Description"
            };
            var project2 = new Project
            {
                Id = Guid.NewGuid(),
                ClientId = otherUserId,
                Title = "Project 2",
                Description = "Project 2 Description"
            };
            var project3 = new Project
            {
                Id = Guid.NewGuid(),
                ClientId = otherUserId,
                Title = "Project 3",
                Description = "Project 3 Description"
            };

            var workspace1 = new ProjectWorkspace
            {
                Id = Guid.NewGuid(),
                ProjectId = project1.Id,
                ClientId = userId,
                ProviderId = otherUserId
            };

            var workspace2 = new ProjectWorkspace
            {
                Id = Guid.NewGuid(),
                ProjectId = project2.Id,
                ClientId = otherUserId,
                ProviderId = userId
            };

            var workspace3 = new ProjectWorkspace
            {
                Id = Guid.NewGuid(),
                ProjectId = project3.Id,
                ClientId = otherUserId,
                ProviderId = thirdUserId
            };

            _context.Users.AddRange(user, otherUser, thirdUser);
            _context.Projects.AddRange(project1, project2, project3);
            _context.ProjectWorkspaces.AddRange(workspace1, workspace2, workspace3);
            await _context.SaveChangesAsync();

            // Act
            var result = await _workspaceService.GetUserWorkspacesAsync(userId);

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(w => w.Id == workspace1.Id);
            result.Should().Contain(w => w.Id == workspace2.Id);
            result.Should().NotContain(w => w.Id == workspace3.Id);
        }

        [Fact]
        public async Task ArchiveWorkspaceAsync_Should_Archive_Workspace_When_User_Is_Client()
        {
            // Arrange
            var clientId = Guid.NewGuid();
            var providerId = Guid.NewGuid();
            var workspace = new ProjectWorkspace
            {
                Id = Guid.NewGuid(),
                ProjectId = Guid.NewGuid(),
                ClientId = clientId,
                ProviderId = providerId,
                Status = WorkspaceStatus.Active
            };

            _context.ProjectWorkspaces.Add(workspace);
            await _context.SaveChangesAsync();

            // Act
            var result = await _workspaceService.ArchiveWorkspaceAsync(workspace.Id, clientId);

            // Assert
            result.Should().BeTrue();
            workspace.Status.Should().Be(WorkspaceStatus.Archived);
            workspace.ArchivedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task ArchiveWorkspaceAsync_Should_Return_False_When_User_No_Access()
        {
            // Arrange
            var clientId = Guid.NewGuid();
            var providerId = Guid.NewGuid();
            var unauthorizedUserId = Guid.NewGuid();
            var workspace = new ProjectWorkspace
            {
                Id = Guid.NewGuid(),
                ProjectId = Guid.NewGuid(),
                ClientId = clientId,
                ProviderId = providerId,
                Status = WorkspaceStatus.Active
            };

            _context.ProjectWorkspaces.Add(workspace);
            await _context.SaveChangesAsync();

            // Act
            var result = await _workspaceService.ArchiveWorkspaceAsync(workspace.Id, unauthorizedUserId);

            // Assert
            result.Should().BeFalse();
            workspace.Status.Should().Be(WorkspaceStatus.Active);
        }

        [Fact]
        public async Task UpdateTimelineAsync_Should_Update_Timeline_Data()
        {
            // Arrange
            var clientId = Guid.NewGuid();
            var workspace = new ProjectWorkspace
            {
                Id = Guid.NewGuid(),
                ProjectId = Guid.NewGuid(),
                ClientId = clientId,
                ProviderId = Guid.NewGuid()
            };

            _context.ProjectWorkspaces.Add(workspace);
            await _context.SaveChangesAsync();

            // VULN-017 FIX: Updated to use strongly-typed TimelineDataDto
            var timelineData = new TimelineDataDto
            {
                Events = new List<TimelineEventDto>
                {
                    new() { Id = Guid.NewGuid(), Title = "Milestone 1", EventDate = DateTime.UtcNow, EventType = "milestone", Status = "planned" },
                    new() { Id = Guid.NewGuid(), Title = "Milestone 2", EventDate = DateTime.UtcNow.AddDays(7), EventType = "milestone", Status = "planned" }
                },
                LastUpdated = DateTime.UtcNow
            };

            // Act
            var result = await _workspaceService.UpdateTimelineAsync(workspace.Id, clientId, timelineData);

            // Assert
            result.Should().BeTrue();
            workspace.TimelineData.Should().NotBeNullOrEmpty();
            workspace.LastSyncedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task GetWorkspaceDashboardAsync_Should_Return_Dashboard_Data()
        {
            // Arrange
            var clientId = Guid.NewGuid();
            var providerId = Guid.NewGuid();
            var project = new Project
            {
                Id = Guid.NewGuid(),
                ClientId = clientId,
                Title = "Test Project",
                Description = "Test Description"
            };
            var workspace = new ProjectWorkspace
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                ClientId = clientId,
                ProviderId = providerId,
                TimelineData = "{\"milestones\":[\"Milestone 1\"]}"
            };

            var client = new User { Id = clientId, Email = "client@test.com" };
            var provider = new User { Id = providerId, Email = "provider@test.com" };

            _context.Users.AddRange(client, provider);
            _context.Projects.Add(project);
            _context.ProjectWorkspaces.Add(workspace);
            await _context.SaveChangesAsync();

            // Act
            var result = await _workspaceService.GetWorkspaceDashboardAsync(workspace.Id, clientId);

            // Assert
            result.Should().NotBeNull();
            result.WorkspaceId.Should().Be(workspace.Id);
            result.ProjectTitle.Should().Be(project.Title);
            result.TimelineData.Should().NotBeNullOrEmpty();
        }
    }
}
