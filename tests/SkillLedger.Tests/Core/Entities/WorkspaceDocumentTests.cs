using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Tests.Infrastructure;
using Xunit;

namespace SkillLedger.Tests.Core.Entities
{
    /// <summary>
    /// Unit tests for WorkspaceDocument entity following TDD principles
    /// Tests focus on critical business logic, security, and data integrity
    /// </summary>
    [UnitTest]
    [DocumentTest]
    public class WorkspaceDocumentTests
    {
        [Fact]
        public void Constructor_ShouldInitializeWithDefaults()
        {
            // Act
            var document = new WorkspaceDocument();

            // Assert
            Assert.NotEqual(Guid.Empty, document.Id);
            Assert.Equal(1, document.VersionNumber);
            Assert.False(document.IsDeleted);
            Assert.False(document.SecurityScanPassed);
            Assert.True(document.CreatedAt <= DateTime.UtcNow);
            Assert.True(document.CreatedAt > DateTime.UtcNow.AddMinutes(-1));
            Assert.NotNull(document.AccessHistory);
            Assert.NotNull(document.Shares);
            Assert.NotNull(document.PreviousVersions);
        }

        [Fact]
        public void RecordAccess_ShouldCreateAccessRecordAndUpdateLastAccessed()
        {
            // Arrange
            var document = new WorkspaceDocument();
            var userId = Guid.NewGuid();
            var beforeAccess = DateTime.UtcNow;

            // Act
            document.RecordAccess(userId);

            // Assert
            Assert.NotNull(document.LastAccessedAt);
            Assert.True(document.LastAccessedAt >= beforeAccess);
            Assert.Single(document.AccessHistory);

            var accessRecord = document.AccessHistory.First();
            Assert.Equal(document.Id, accessRecord.DocumentId);
            Assert.Equal(userId, accessRecord.UserId);
            Assert.True(accessRecord.AccessedAt <= DateTime.UtcNow);
        }

        [Fact]
        public void RecordAccess_MultipleAccesses_ShouldMaintainHistory()
        {
            // Arrange
            var document = new WorkspaceDocument();
            var user1 = Guid.NewGuid();
            var user2 = Guid.NewGuid();

            // Act
            document.RecordAccess(user1);
            Thread.Sleep(1); // Ensure different timestamps
            document.RecordAccess(user2);
            document.RecordAccess(user1); // Same user again

            // Assert
            Assert.Equal(3, document.AccessHistory.Count);
            Assert.Equal(2, document.AccessHistory.Count(a => a.UserId == user1));
            Assert.Equal(1, document.AccessHistory.Count(a => a.UserId == user2));
        }

        [Fact]
        public void Delete_ShouldSoftDeleteWithMetadata()
        {
            // Arrange
            var document = new WorkspaceDocument();
            var userId = Guid.NewGuid();
            var beforeDelete = DateTime.UtcNow;

            // Act
            document.Delete(userId);

            // Assert
            Assert.True(document.IsDeleted);
            Assert.NotNull(document.DeletedAt);
            Assert.True(document.DeletedAt >= beforeDelete);
            Assert.Equal(userId, document.DeletedBy);
        }

        [Fact]
        public void Delete_WhenAlreadyDeleted_ShouldUpdateMetadata()
        {
            // Arrange
            var document = new WorkspaceDocument();
            var firstDeleter = Guid.NewGuid();
            var secondDeleter = Guid.NewGuid();

            document.Delete(firstDeleter);
            var firstDeletedAt = document.DeletedAt;
            Thread.Sleep(1);

            // Act
            document.Delete(secondDeleter);

            // Assert
            Assert.True(document.IsDeleted);
            Assert.True(document.DeletedAt > firstDeletedAt);
            Assert.Equal(secondDeleter, document.DeletedBy);
        }

        [Fact]
        public void Restore_ShouldClearDeletionMetadata()
        {
            // Arrange
            var document = new WorkspaceDocument();
            var userId = Guid.NewGuid();
            document.Delete(userId);

            // Act
            document.Restore();

            // Assert
            Assert.False(document.IsDeleted);
            Assert.Null(document.DeletedAt);
            Assert.Null(document.DeletedBy);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CanBeAccessedBy_WithDeletedDocument_ShouldReturnFalse(bool hasWorkspaceAccess)
        {
            // Arrange
            var document = new WorkspaceDocument();
            var userId = Guid.NewGuid();
            var workspace = CreateMockWorkspace(userId, hasWorkspaceAccess);
            document.Workspace = workspace;
            document.Delete(Guid.NewGuid()); // Delete the document

            // Act
            var result = document.CanBeAccessedBy(userId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanBeAccessedBy_WithWorkspaceAccess_ShouldReturnTrue()
        {
            // Arrange
            var document = new WorkspaceDocument();
            var userId = Guid.NewGuid();
            var workspace = CreateMockWorkspace(userId, hasAccess: true);
            document.Workspace = workspace;

            // Act
            var result = document.CanBeAccessedBy(userId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanBeAccessedBy_WithValidDocumentShare_ShouldReturnTrue()
        {
            // Arrange
            var document = new WorkspaceDocument();
            var userId = Guid.NewGuid();
            var workspace = CreateMockWorkspace(userId, hasAccess: false);
            document.Workspace = workspace;

            // Add valid document share
            var share = CreateMockDocumentShare(userId, isActiveAndValid: true);
            document.Shares.Add(share);

            // Act
            var result = document.CanBeAccessedBy(userId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanBeAccessedBy_WithInvalidDocumentShare_ShouldReturnFalse()
        {
            // Arrange
            var document = new WorkspaceDocument();
            var userId = Guid.NewGuid();
            var workspace = CreateMockWorkspace(userId, hasAccess: false);
            document.Workspace = workspace;

            // Add invalid document share (expired/revoked)
            var share = CreateMockDocumentShare(userId, isActiveAndValid: false);
            document.Shares.Add(share);

            // Act
            var result = document.CanBeAccessedBy(userId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanBeAccessedBy_WithNoAccess_ShouldReturnFalse()
        {
            // Arrange
            var document = new WorkspaceDocument();
            var userId = Guid.NewGuid();
            var workspace = CreateMockWorkspace(userId, hasAccess: false);
            document.Workspace = workspace;

            // Act
            var result = document.CanBeAccessedBy(userId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanBeEditedBy_WithDeletedDocument_ShouldReturnFalse()
        {
            // Arrange
            var document = new WorkspaceDocument();
            var userId = Guid.NewGuid();
            document.UploadedBy = userId;
            document.Delete(Guid.NewGuid());

            // Act
            var result = document.CanBeEditedBy(userId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanBeEditedBy_AsUploader_ShouldReturnTrue()
        {
            // Arrange
            var document = new WorkspaceDocument();
            var userId = Guid.NewGuid();
            document.UploadedBy = userId;

            // Act
            var result = document.CanBeEditedBy(userId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanBeEditedBy_WithWorkspaceAccess_ShouldReturnTrue()
        {
            // Arrange
            var document = new WorkspaceDocument();
            var userId = Guid.NewGuid();
            var uploaderId = Guid.NewGuid();
            document.UploadedBy = uploaderId;

            var workspace = CreateMockWorkspace(userId, hasAccess: true);
            document.Workspace = workspace;

            // Act
            var result = document.CanBeEditedBy(userId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanBeEditedBy_WithoutAccess_ShouldReturnFalse()
        {
            // Arrange
            var document = new WorkspaceDocument();
            var userId = Guid.NewGuid();
            var uploaderId = Guid.NewGuid();
            document.UploadedBy = uploaderId;

            var workspace = CreateMockWorkspace(userId, hasAccess: false);
            document.Workspace = workspace;

            // Act
            var result = document.CanBeEditedBy(userId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanBeDeletedBy_WithDeletedDocument_ShouldReturnFalse()
        {
            // Arrange
            var document = new WorkspaceDocument();
            var userId = Guid.NewGuid();
            document.UploadedBy = userId;
            document.Delete(userId);

            // Act
            var result = document.CanBeDeletedBy(userId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanBeDeletedBy_AsUploader_ShouldReturnTrue()
        {
            // Arrange
            var document = new WorkspaceDocument();
            var userId = Guid.NewGuid();
            document.UploadedBy = userId;

            // Act
            var result = document.CanBeDeletedBy(userId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanBeDeletedBy_AsNonUploader_ShouldReturnFalse()
        {
            // Arrange
            var document = new WorkspaceDocument();
            var uploaderId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            document.UploadedBy = uploaderId;

            // Act
            var result = document.CanBeDeletedBy(otherUserId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Properties_ShouldAcceptValidValues()
        {
            // Arrange
            var document = new WorkspaceDocument();
            var workspaceId = Guid.NewGuid();
            var uploaderId = Guid.NewGuid();
            var folderId = Guid.NewGuid();

            // Act
            document.WorkspaceId = workspaceId;
            document.FileName = "test-document.pdf";
            document.FilePath = "/storage/path/test-document.pdf";
            document.FileSize = 1024 * 1024; // 1MB
            document.MimeType = "application/pdf";
            document.UploadedBy = uploaderId;
            document.FolderId = folderId;
            document.VersionNumber = 2;
            document.SecurityScanPassed = true;

            // Assert
            Assert.Equal(workspaceId, document.WorkspaceId);
            Assert.Equal("test-document.pdf", document.FileName);
            Assert.Equal("/storage/path/test-document.pdf", document.FilePath);
            Assert.Equal(1024 * 1024, document.FileSize);
            Assert.Equal("application/pdf", document.MimeType);
            Assert.Equal(uploaderId, document.UploadedBy);
            Assert.Equal(folderId, document.FolderId);
            Assert.Equal(2, document.VersionNumber);
            Assert.True(document.SecurityScanPassed);
        }

        [Fact]
        public void OptionalProperties_ShouldAcceptNullValues()
        {
            // Arrange
            var document = new WorkspaceDocument();

            // Act & Assert
            document.FolderId = null;
            document.LastAccessedAt = null;
            document.DeletedAt = null;
            document.DeletedBy = null;
            document.SecurityScanResult = null;
            document.ParentDocumentId = null;

            Assert.Null(document.FolderId);
            Assert.Null(document.LastAccessedAt);
            Assert.Null(document.DeletedAt);
            Assert.Null(document.DeletedBy);
            Assert.Null(document.SecurityScanResult);
            Assert.Null(document.ParentDocumentId);
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

            // Mock the IsAccessibleBy method behavior
            // Note: In actual implementation, we would use a proper mocking framework
            // For this unit test, we simulate the behavior
            return workspace;
        }

        private static DocumentShare CreateMockDocumentShare(Guid userId, bool isActiveAndValid)
        {
            var share = new DocumentShare
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Permission = SharePermission.View,
                IsActive = isActiveAndValid,
                CreatedAt = DateTime.UtcNow
            };

            if (!isActiveAndValid)
            {
                share.ExpiresAt = DateTime.UtcNow.AddDays(-1); // Expired
            }

            return share;
        }
    }
}