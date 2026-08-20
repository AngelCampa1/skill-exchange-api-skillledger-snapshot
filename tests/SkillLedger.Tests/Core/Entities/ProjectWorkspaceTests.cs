using FluentAssertions;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Tests.Infrastructure;
using Xunit;

namespace SkillLedger.Tests.Core.Entities
{
    [UnitTest]
    [CoreTest]
    public class ProjectWorkspaceTests
    {
        [Fact]
        public void ProjectWorkspace_Should_Initialize_With_Default_Values()
        {
            // Arrange & Act
            var workspace = new ProjectWorkspace();

            // Assert
            workspace.Id.Should().NotBeEmpty();
            workspace.Status.Should().Be(WorkspaceStatus.Active);
            workspace.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            workspace.ArchivedAt.Should().BeNull();
            workspace.WorkspaceKey.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void ProjectWorkspace_Should_Initialize_With_Empty_RequiredIds_When_Not_Set()
        {
            // Arrange & Act
            var workspace = new ProjectWorkspace();

            // Assert - These should be empty until explicitly set
            workspace.ProjectId.Should().Be(Guid.Empty);
            workspace.ClientId.Should().Be(Guid.Empty);
            workspace.ProviderId.Should().Be(Guid.Empty);
        }

        [Fact]
        public void ProjectWorkspace_Should_Accept_Valid_RequiredIds()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var clientId = Guid.NewGuid();
            var providerId = Guid.NewGuid();

            // Act
            var workspace = new ProjectWorkspace
            {
                ProjectId = projectId,
                ClientId = clientId,
                ProviderId = providerId
            };

            // Assert
            workspace.ProjectId.Should().Be(projectId);
            workspace.ClientId.Should().Be(clientId);
            workspace.ProviderId.Should().Be(providerId);
        }

        [Fact]
        public void ProjectWorkspace_Should_Generate_Unique_WorkspaceKey()
        {
            // Arrange & Act
            var workspace1 = new ProjectWorkspace();
            var workspace2 = new ProjectWorkspace();

            // Assert
            workspace1.WorkspaceKey.Should().NotBe(workspace2.WorkspaceKey);
            workspace1.WorkspaceKey.Length.Should().BeGreaterThan(32); // Encrypted key should be longer
        }

        [Fact]
        public void ArchiveWorkspace_Should_Set_ArchivedAt_And_Change_Status()
        {
            // Arrange
            var workspace = new ProjectWorkspace();

            // Act
            workspace.ArchiveWorkspace();

            // Assert
            workspace.Status.Should().Be(WorkspaceStatus.Archived);
            workspace.ArchivedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void ArchiveWorkspace_Should_Not_Archive_Already_Archived_Workspace()
        {
            // Arrange
            var workspace = new ProjectWorkspace();
            workspace.ArchiveWorkspace();
            var firstArchivedTime = workspace.ArchivedAt;

            // Act
            workspace.ArchiveWorkspace();

            // Assert
            workspace.ArchivedAt.Should().Be(firstArchivedTime);
        }

        [Fact]
        public void IsAccessibleBy_Should_Return_True_For_Client()
        {
            // Arrange
            var clientId = Guid.NewGuid();
            var providerId = Guid.NewGuid();
            var workspace = new ProjectWorkspace
            {
                ClientId = clientId,
                ProviderId = providerId
            };

            // Act & Assert
            workspace.IsAccessibleBy(clientId).Should().BeTrue();
        }

        [Fact]
        public void IsAccessibleBy_Should_Return_True_For_Provider()
        {
            // Arrange
            var clientId = Guid.NewGuid();
            var providerId = Guid.NewGuid();
            var workspace = new ProjectWorkspace
            {
                ClientId = clientId,
                ProviderId = providerId
            };

            // Act & Assert
            workspace.IsAccessibleBy(providerId).Should().BeTrue();
        }

        [Fact]
        public void IsAccessibleBy_Should_Return_False_For_Other_Users()
        {
            // Arrange
            var clientId = Guid.NewGuid();
            var providerId = Guid.NewGuid();
            var otherId = Guid.NewGuid();
            var workspace = new ProjectWorkspace
            {
                ClientId = clientId,
                ProviderId = providerId
            };

            // Act & Assert
            workspace.IsAccessibleBy(otherId).Should().BeFalse();
        }
    }
}