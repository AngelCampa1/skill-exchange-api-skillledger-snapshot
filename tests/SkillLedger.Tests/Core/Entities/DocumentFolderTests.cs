using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Tests.Infrastructure;
using Xunit;

namespace SkillLedger.Tests.Core.Entities
{
    /// <summary>
    /// Unit tests for DocumentFolder entity following TDD principles
    /// Tests focus on folder hierarchy, permissions, and cascade operations
    /// </summary>
    [UnitTest]
    [DocumentTest]
    public class DocumentFolderTests
    {
        [Fact]
        public void Constructor_ShouldInitializeWithDefaults()
        {
            // Arrange
            var beforeCreation = DateTime.UtcNow;

            // Act
            var folder = new DocumentFolder();
            var afterCreation = DateTime.UtcNow;

            // Assert
            Assert.NotEqual(Guid.Empty, folder.Id);
            Assert.False(folder.IsDeleted);
            Assert.Equal(0, folder.SortOrder);

            // Use before/after timestamps to avoid timing issues
            Assert.True(folder.CreatedAt >= beforeCreation,
                $"CreatedAt ({folder.CreatedAt}) should be >= beforeCreation ({beforeCreation})");
            Assert.True(folder.CreatedAt <= afterCreation,
                $"CreatedAt ({folder.CreatedAt}) should be <= afterCreation ({afterCreation})");

            Assert.NotNull(folder.ChildFolders);
            Assert.NotNull(folder.Documents);
        }

        [Fact]
        public void GetFullPath_WithRootFolder_ShouldReturnFolderName()
        {
            // Arrange
            var folder = new DocumentFolder
            {
                FolderName = "Root Folder",
                ParentFolder = null
            };

            // Act
            var result = folder.GetFullPath();

            // Assert
            Assert.Equal("Root Folder", result);
        }

        [Fact]
        public void GetFullPath_WithNestedFolder_ShouldReturnFullPath()
        {
            // Arrange
            var rootFolder = new DocumentFolder
            {
                FolderName = "Documents",
                ParentFolder = null
            };

            var subFolder = new DocumentFolder
            {
                FolderName = "Projects",
                ParentFolder = rootFolder
            };

            var deepFolder = new DocumentFolder
            {
                FolderName = "2024",
                ParentFolder = subFolder
            };

            // Act
            var result = deepFolder.GetFullPath();

            // Assert
            Assert.Equal("Documents/Projects/2024", result);
        }

        [Fact]
        public void GetFullPath_WithVeryDeepNesting_ShouldHandleCorrectly()
        {
            // Arrange - Create a 5-level deep folder structure
            var level1 = new DocumentFolder { FolderName = "Level1", ParentFolder = null };
            var level2 = new DocumentFolder { FolderName = "Level2", ParentFolder = level1 };
            var level3 = new DocumentFolder { FolderName = "Level3", ParentFolder = level2 };
            var level4 = new DocumentFolder { FolderName = "Level4", ParentFolder = level3 };
            var level5 = new DocumentFolder { FolderName = "Level5", ParentFolder = level4 };

            // Act
            var result = level5.GetFullPath();

            // Assert
            Assert.Equal("Level1/Level2/Level3/Level4/Level5", result);
        }

        [Fact]
        public void Delete_ShouldSoftDeleteWithMetadata()
        {
            // Arrange
            var folder = new DocumentFolder();
            var userId = Guid.NewGuid();
            var beforeDelete = DateTime.UtcNow;

            // Act
            folder.Delete(userId);

            // Assert
            Assert.True(folder.IsDeleted);
            Assert.NotNull(folder.DeletedAt);
            Assert.True(folder.DeletedAt >= beforeDelete);
            Assert.Equal(userId, folder.DeletedBy);
        }

        [Fact]
        public void Delete_WithChildFolders_ShouldCascadeDelete()
        {
            // Arrange
            var parentFolder = new DocumentFolder();
            var childFolder1 = new DocumentFolder { IsDeleted = false };
            var childFolder2 = new DocumentFolder { IsDeleted = false };
            var alreadyDeletedChild = new DocumentFolder { IsDeleted = true };

            parentFolder.ChildFolders.Add(childFolder1);
            parentFolder.ChildFolders.Add(childFolder2);
            parentFolder.ChildFolders.Add(alreadyDeletedChild);

            var userId = Guid.NewGuid();

            // Act
            parentFolder.Delete(userId);

            // Assert
            Assert.True(parentFolder.IsDeleted);
            Assert.True(childFolder1.IsDeleted);
            Assert.True(childFolder2.IsDeleted);
            Assert.True(alreadyDeletedChild.IsDeleted); // Should remain deleted

            // Check deletion metadata
            Assert.Equal(userId, childFolder1.DeletedBy);
            Assert.Equal(userId, childFolder2.DeletedBy);
            Assert.NotNull(childFolder1.DeletedAt);
            Assert.NotNull(childFolder2.DeletedAt);
        }

        [Fact]
        public void Delete_WithDocuments_ShouldCascadeDeleteDocuments()
        {
            // Arrange
            var folder = new DocumentFolder();
            var document1 = new WorkspaceDocument { IsDeleted = false };
            var document2 = new WorkspaceDocument { IsDeleted = false };
            var alreadyDeletedDoc = new WorkspaceDocument { IsDeleted = true };

            folder.Documents.Add(document1);
            folder.Documents.Add(document2);
            folder.Documents.Add(alreadyDeletedDoc);

            var userId = Guid.NewGuid();

            // Act
            folder.Delete(userId);

            // Assert
            Assert.True(folder.IsDeleted);
            Assert.True(document1.IsDeleted);
            Assert.True(document2.IsDeleted);
            Assert.True(alreadyDeletedDoc.IsDeleted); // Should remain deleted

            // Check deletion metadata
            Assert.Equal(userId, document1.DeletedBy);
            Assert.Equal(userId, document2.DeletedBy);
            Assert.NotNull(document1.DeletedAt);
            Assert.NotNull(document2.DeletedAt);
        }

        [Fact]
        public void Delete_WithMixedChildrenAndDocuments_ShouldCascadeDeleteAll()
        {
            // Arrange
            var parentFolder = new DocumentFolder();
            var childFolder = new DocumentFolder { IsDeleted = false };
            var document = new WorkspaceDocument { IsDeleted = false };

            parentFolder.ChildFolders.Add(childFolder);
            parentFolder.Documents.Add(document);

            var userId = Guid.NewGuid();

            // Act
            parentFolder.Delete(userId);

            // Assert
            Assert.True(parentFolder.IsDeleted);
            Assert.True(childFolder.IsDeleted);
            Assert.True(document.IsDeleted);
            Assert.Equal(userId, parentFolder.DeletedBy);
            Assert.Equal(userId, childFolder.DeletedBy);
            Assert.Equal(userId, document.DeletedBy);
        }

        [Fact]
        public void Restore_ShouldClearDeletionMetadata()
        {
            // Arrange
            var folder = new DocumentFolder();
            var userId = Guid.NewGuid();
            folder.Delete(userId);

            // Act
            folder.Restore();

            // Assert
            Assert.False(folder.IsDeleted);
            Assert.Null(folder.DeletedAt);
            Assert.Null(folder.DeletedBy);
        }

        [Fact]
        public void CanBeAccessedBy_WithDeletedFolder_ShouldReturnFalse()
        {
            // Arrange
            var folder = new DocumentFolder();
            var userId = Guid.NewGuid();
            var workspace = CreateMockWorkspace(userId, hasAccess: true);
            folder.Workspace = workspace;
            folder.Delete(Guid.NewGuid());

            // Act
            var result = folder.CanBeAccessedBy(userId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanBeAccessedBy_WithWorkspaceAccess_ShouldReturnTrue()
        {
            // Arrange
            var folder = new DocumentFolder();
            var userId = Guid.NewGuid();
            var workspace = CreateMockWorkspace(userId, hasAccess: true);
            folder.Workspace = workspace;

            // Act
            var result = folder.CanBeAccessedBy(userId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanBeAccessedBy_WithoutWorkspaceAccess_ShouldReturnFalse()
        {
            // Arrange
            var folder = new DocumentFolder();
            var userId = Guid.NewGuid();
            var workspace = CreateMockWorkspace(userId, hasAccess: false);
            folder.Workspace = workspace;

            // Act
            var result = folder.CanBeAccessedBy(userId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanBeEditedBy_WithDeletedFolder_ShouldReturnFalse()
        {
            // Arrange
            var folder = new DocumentFolder();
            var userId = Guid.NewGuid();
            folder.CreatedBy = userId;
            folder.Delete(userId);

            // Act
            var result = folder.CanBeEditedBy(userId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanBeEditedBy_AsCreator_ShouldReturnTrue()
        {
            // Arrange
            var folder = new DocumentFolder();
            var userId = Guid.NewGuid();
            folder.CreatedBy = userId;

            // Act
            var result = folder.CanBeEditedBy(userId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanBeEditedBy_WithWorkspaceAccess_ShouldReturnTrue()
        {
            // Arrange
            var folder = new DocumentFolder();
            var userId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            folder.CreatedBy = creatorId;

            var workspace = CreateMockWorkspace(userId, hasAccess: true);
            folder.Workspace = workspace;

            // Act
            var result = folder.CanBeEditedBy(userId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanBeEditedBy_WithoutAccess_ShouldReturnFalse()
        {
            // Arrange
            var folder = new DocumentFolder();
            var userId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            folder.CreatedBy = creatorId;

            var workspace = CreateMockWorkspace(userId, hasAccess: false);
            folder.Workspace = workspace;

            // Act
            var result = folder.CanBeEditedBy(userId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanBeDeletedBy_WithDeletedFolder_ShouldReturnFalse()
        {
            // Arrange
            var folder = new DocumentFolder();
            var userId = Guid.NewGuid();
            folder.CreatedBy = userId;
            folder.Delete(userId);

            // Act
            var result = folder.CanBeDeletedBy(userId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanBeDeletedBy_AsCreator_ShouldReturnTrue()
        {
            // Arrange
            var folder = new DocumentFolder();
            var userId = Guid.NewGuid();
            folder.CreatedBy = userId;

            // Act
            var result = folder.CanBeDeletedBy(userId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanBeDeletedBy_AsNonCreator_ShouldReturnFalse()
        {
            // Arrange
            var folder = new DocumentFolder();
            var creatorId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            folder.CreatedBy = creatorId;

            // Act
            var result = folder.CanBeDeletedBy(otherUserId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Properties_ShouldAcceptValidValues()
        {
            // Arrange
            var folder = new DocumentFolder();
            var workspaceId = Guid.NewGuid();
            var createdBy = Guid.NewGuid();
            var parentFolderId = Guid.NewGuid();
            var description = "This folder contains project documentation";

            // Act
            folder.WorkspaceId = workspaceId;
            folder.FolderName = "Project Documents";
            folder.ParentFolderId = parentFolderId;
            folder.CreatedBy = createdBy;
            folder.Description = description;
            folder.SortOrder = 5;

            // Assert
            Assert.Equal(workspaceId, folder.WorkspaceId);
            Assert.Equal("Project Documents", folder.FolderName);
            Assert.Equal(parentFolderId, folder.ParentFolderId);
            Assert.Equal(createdBy, folder.CreatedBy);
            Assert.Equal(description, folder.Description);
            Assert.Equal(5, folder.SortOrder);
        }

        [Fact]
        public void OptionalProperties_ShouldAcceptNullValues()
        {
            // Arrange
            var folder = new DocumentFolder();

            // Act & Assert
            folder.ParentFolderId = null;
            folder.DeletedAt = null;
            folder.DeletedBy = null;
            folder.Description = null;

            Assert.Null(folder.ParentFolderId);
            Assert.Null(folder.DeletedAt);
            Assert.Null(folder.DeletedBy);
            Assert.Null(folder.Description);
        }

        [Fact]
        public void FolderHierarchy_ShouldMaintainConsistency()
        {
            // Arrange
            var parentFolder = new DocumentFolder
            {
                Id = Guid.NewGuid(),
                FolderName = "Parent",
                WorkspaceId = Guid.NewGuid()
            };

            var childFolder = new DocumentFolder
            {
                Id = Guid.NewGuid(),
                FolderName = "Child",
                WorkspaceId = parentFolder.WorkspaceId,
                ParentFolderId = parentFolder.Id,
                ParentFolder = parentFolder
            };

            // Act
            parentFolder.ChildFolders.Add(childFolder);

            // Assert
            Assert.Single(parentFolder.ChildFolders);
            Assert.Contains(childFolder, parentFolder.ChildFolders);
            Assert.Equal(parentFolder.Id, childFolder.ParentFolderId);
            Assert.Equal(parentFolder, childFolder.ParentFolder);
            Assert.Equal(parentFolder.WorkspaceId, childFolder.WorkspaceId);
        }

        private static ProjectWorkspace CreateMockWorkspace(Guid userId, bool hasAccess)
        {
            var workspace = new ProjectWorkspace
            {
                Id = Guid.NewGuid(),
                ClientId = hasAccess ? userId : Guid.NewGuid(),
                ProviderId = Guid.NewGuid(),
                Status = WorkspaceStatus.Active
            };

            return workspace;
        }
    }
}